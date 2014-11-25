using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Newtonsoft.Json.Linq;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs
{
    public class BufferedRetryDispatch : IObservableDispatch
    {
        public Guid ObservableId { get; private set; }
        private readonly Action<ObservableNotification> _transportAction;
        private readonly IScheduler _scheduler;
        private readonly CompositeDisposable _subscription = new CompositeDisposable();
        private readonly ConcurrentDictionary<long, MessageBuffer> _messageBuffer = new ConcurrentDictionary<long, MessageBuffer>();
        private long _nextCounter;

        public BufferedRetryDispatch(Guid observableId, Action<ObservableNotification> transportAction, IDisposable subscription) : this(observableId,transportAction,subscription,null)
        {
            
        }

        public BufferedRetryDispatch(Guid observableId, Action<ObservableNotification> transportAction, IDisposable subscription, IScheduler scheduler)
        {
            _scheduler = scheduler ?? Scheduler.Default;
            ObservableId = observableId;
            _transportAction = transportAction;
            _subscription.Add(subscription);
            // starts a timer that goes through buffer every second and resends any message that is older then 5 seconds (wasn't removed - ack not received)
            var resendTimer = Observable.Interval(TimeSpan.FromSeconds(1), _scheduler)
                .Do(_ =>
                {
//                    int bufferLength = 0;
                    foreach (var message in _messageBuffer)
                    {
                        if (message.Value.LastSentAttempt < _scheduler.Now.DateTime.AddSeconds(-5))
                        {
                            var error = message.Value.Payload as Error;
                            if (error != null)
                            {
                                SendToTransport(message.Key, ObservableComponent.Error,  error);
                            }
                            else
                            {
                                var end = message.Value.Payload as SequenceEnd;
                                if (end != null)
                                {
                                    SendToTransport(message.Key, ObservableComponent.Complete, null);
                                }
                                else
                                {
                                    SendToTransport(message.Key, ObservableComponent.Next, message.Value.Payload);
                                }
                            }

                            message.Value.LastSentAttempt = _scheduler.Now.DateTime;
                        }
//                        bufferLength++;
                    }
                })
                .Subscribe();
            _subscription.Add(resendTimer);
        }


        public CompositeDisposable Subscription
        {
            get { return _subscription; }
        }

        public long NextCounter
        {
            get { return _nextCounter; }
        }
        
        // doesn't cause a lock to be acquired
        public long BufferLength { get { return _messageBuffer.Skip(0).Count(); } }

        public void OnNext(object message)
        {
//            if (_isComplete) return -1; // should never happen as we receive next after complete/error, but just in case
            var msgSequenceNo = Interlocked.Increment(ref _nextCounter);
            var msgBuffer = new MessageBuffer(message) { LastSentAttempt = DateTime.Now };
            _messageBuffer.TryAdd(msgSequenceNo, msgBuffer);
            SendToTransport(msgSequenceNo, ObservableComponent.Next,  message);
//            return msgSequenceNo;
        }

        public void SendToTransport(long sequenceNo, ObservableComponent component, object payload)
        {
            var observableNotification = new ObservableNotification(this.ObservableId, sequenceNo, component, payload != null ? JToken.FromObject(payload) : null);
            _transportAction(observableNotification);
        }

        public void OnError(Exception exception)
        {
            var sequenceTerminator = new Error( exception);
            AddSequenceTerminatorToBuffer(sequenceTerminator);
            SendToTransport(this.NextCounter + 1, ObservableComponent.Error, sequenceTerminator);
        }

        public void OnCompleted()
        {
            var sequenceTerminator = new SequenceEnd();
            AddSequenceTerminatorToBuffer(sequenceTerminator);
            SendToTransport(this.NextCounter + 1, ObservableComponent.Complete, null);
        }

        private void AddSequenceTerminatorToBuffer(SequenceEnd sequenceTerminator)
        {
            var msgBuffer = new MessageBuffer(sequenceTerminator) { LastSentAttempt = DateTime.Now };
            _messageBuffer.TryAdd(NextCounter + 1, msgBuffer);
            _isComplete = true;
        }

        private bool _isComplete = false;

        public void Ack(int messageId)
        {
            MessageBuffer msgBuffer;
            _messageBuffer.TryRemove(messageId, out msgBuffer);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }
    }
}
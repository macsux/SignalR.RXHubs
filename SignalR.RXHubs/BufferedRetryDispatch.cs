using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using SignalR.RXHubs.Core;

namespace SignalR.RXHubs
{
    public class BufferedRetryDispatch : IObservableDispatch
    {
        private readonly Action<long,object> _nextTransport;
        private readonly Action<long,Error> _errorTransport;
        private readonly Action<long,SequenceEnd> _completeTransport;
        private readonly CompositeDisposable _subscription = new CompositeDisposable();
        private readonly ConcurrentDictionary<long, MessageBuffer> _messageBuffer = new ConcurrentDictionary<long, MessageBuffer>();
        private long _nextCounter;

        public BufferedRetryDispatch(Action<long, object> nextTransport, Action<long,Error> errorTransport, Action<long,SequenceEnd> completeTransport, IDisposable subscription)
        {
            _nextTransport = nextTransport;
            _errorTransport = errorTransport;
            _completeTransport = completeTransport;
            _subscription.Add(subscription);
            // starts a timer that goes through buffer every second and resends any message that is older then 5 seconds (wasn't removed - ack not received)
            var resendTimer = Observable.Timer(TimeSpan.FromSeconds(1))
                .Do(_ =>
                {
//                    int bufferLength = 0;
                    foreach (var message in _messageBuffer)
                    {
                        if (message.Value.LastSentAttempt < DateTime.Now.AddSeconds(5))
                        {
                            if (message.Value.Payload is Error)
                            {
                                errorTransport(message.Key, (Error) message.Value.Payload);
                            }
                            else if (message.Value.Payload is SequenceEnd)
                            {
                                completeTransport(message.Key, (SequenceEnd) message.Value.Payload);
                            }
                            else
                            {
                                nextTransport(message.Key, message.Value.Payload);
                            }
                            message.Value.LastSentAttempt = DateTime.Now;
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
            _nextTransport(msgSequenceNo, message);
//            return msgSequenceNo;
        }

        public void OnError(Exception exception)
        {
            var sequenceTerminator = new Error( exception);
            AddSequenceTerminatorToBuffer(sequenceTerminator);
            _errorTransport(sequenceTerminator);
        }

        public void OnCompleted()
        {
            var sequenceTerminator = new SequenceEnd();
            AddSequenceTerminatorToBuffer(sequenceTerminator);
            _completeTransport(sequenceTerminator);
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
    }
}
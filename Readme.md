How it works internally

1. Detect all hubs that inherit from ObservableHub
2. Find interface that inherits from IVirtualHub. This is our contract that's shared between client and server
3. Generate a new TYPE that is based on this interface, but for properties that return IObservable change signature to void and introduce extra parameter at position 0 of type guid Guid.
Ex. Observable<string> MyMethod(string hello)  --> void MyMethod(Guid observableId, string hello)
4. Create an instance of this class via dynamic proxy, handled by interceptor
5. Register these new "real hubs" with IoC / SignalR hubs resolver

Guid is provided by the client as identifier for observable. Messages are sent over special channel (msg name "O"), carrying subscription GUID, sequence number, and payload (terminator if end of sequence). 
Server buffers all msgs until ack replies are received, after which time they are discarded. If after X seconds the ACK is not received, msg is sent again. 
Server maintains a list of all open observables mapped to client connection ID. If client id disconnects, all observables are unsubscribed.
If client is done with observable, it unsubscribes via special "Unsubscribe" message to server with GUID.

Client maintains it's own buffer that ensures that msgs are output in proper sequence. If msgs are received out of order they are buffered until the "missing" message is received.

Limitations:
Buffering is very primitive, could potentially become overfilled
Currently only has implementation for autofac, no internal IoC provider (architecture allows plugging in another IoC however)
No caching for reflection of calls inside interceptors. *may* become an issue if very heavy subscribe/unsubscribe pattern

How it works internally

1. Detect all hubs that inherit from ObservableHub
2. Find interface that inherits from IVirtualHub. This is our contract that's shared between client and server
3. Generate a new TYPE that is based on this interface, but for properties that return IObservable change signature to void and introduce extra parameter at position 0 of type guid Guid.
Ex. Observable<string> MyMethod(string hello)  --> void MyMethod(Guid observableId, string hello)
4. Create an instance of this class via dynamic proxy, handled by interceptor
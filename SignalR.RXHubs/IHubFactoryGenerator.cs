namespace SignalR.RXHubs
{
    public interface IHubFactoryGenerator
    {
        HubFactory GetRealHubFactory(HubFactory virtualHubFactory);
    }
}
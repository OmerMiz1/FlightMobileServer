namespace FlightMobileServer.ServerModels
{
    public interface ITcpClient
    {
        void Connect(string ip, int port);
        void Write(string command);
        string Read();
        void Disconnect();
    }
}
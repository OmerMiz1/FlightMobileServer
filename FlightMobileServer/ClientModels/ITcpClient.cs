using FlightMobileWeb.Models;

namespace FlightMobileWeb.ClientModels
{
    public interface ITcpClient
    {
        public void Start();
        public void Stop();
        public void Write(Command cmd);
        public string Read();
    }
}
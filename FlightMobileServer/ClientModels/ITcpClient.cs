using System.Net.Sockets;
using FlightMobileServer.Models;

namespace FlightMobileServer.ClientModels
{
    public interface ITcpClient {
        public void Start();
        public void Stop();
        public void Write(Command cmd);
        public string Read();
    }
}
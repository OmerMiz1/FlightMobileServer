using FlightMobileServer.Models;
using System.Threading.Tasks;

namespace FlightMobileServer.ClientModels
{
    public interface IAsyncTcpClient : ITcpClient
    {
        public void ProcessCommands();
        public Task<Result> Execute(Command cmd);
    }
}
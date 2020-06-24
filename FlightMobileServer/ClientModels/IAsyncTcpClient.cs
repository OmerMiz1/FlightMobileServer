using FlightMobileWeb.Models;
using System.Threading.Tasks;

namespace FlightMobileWeb.ClientModels
{
    public interface IAsyncTcpClient : ITcpClient
    {
        public void ProcessCommands();
        public Task<Result> Execute(Command cmd);
    }
}
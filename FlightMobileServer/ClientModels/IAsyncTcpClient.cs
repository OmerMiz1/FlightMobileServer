using System.Threading.Tasks;
using FlightMobileServer.Models;

namespace FlightMobileServer.ClientModels {
    public interface IAsyncTcpClient : ITcpClient {
        public void ProcessCommands();
        public Task<Result> Execute(Command cmd);
    }
}
using System.Threading.Tasks;
using FlightMobileServer.Models;
//a/dsf

namespace FlightMobileServer.ClientModels {
    public enum Result { Ok, NotOk }
    public class AsyncCommand {
        public AsyncCommand(Command command, TaskCompletionSource<Result> completion) {
            Command = command;
            Completion = completion;
        }

        public Command Command { get; private set; }
        public Task<Result> Task => Completion.Task;
        public TaskCompletionSource<Result> Completion { get; private set; }

        public AsyncCommand (Command input) { 
            Command = input;
            Completion = new TaskCompletionSource<Result> (
                TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}

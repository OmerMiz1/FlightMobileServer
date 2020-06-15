using System.Threading.Tasks;
using FlightMobileServer.Models;

namespace FlightMobileServer.ServerModels {
    public enum Result { Ok, NotOk }
    public class AsyncCommand {
        public AsyncCommand() { }
        public AsyncCommand(Command command, TaskCompletionSource<Result> completion) {
            Command = command;
            Completion = completion;
        }

        public Command Command { get; private set; }
        public Task<Result> Task { get => Completion.Task; }
        public TaskCompletionSource<Result> Completion { get; private set; }

        public AsyncCommand (Command input) { 
            Command = input;

            // Watch out! Run Continuations Async is important!
            Completion = new TaskCompletionSource<Result> (
            TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}

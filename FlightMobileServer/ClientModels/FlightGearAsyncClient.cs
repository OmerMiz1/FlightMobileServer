using FlightMobileServer.Models;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FlightMobileServer.ClientModels
{
    public class FlightGearAsyncClient : IAsyncTcpClient
    {
        /* Variables Paths */
        private const string ElevatorPath = @"/controls/flight/elevator";
        private const string RudderPath = @"/controls/flight/rudder";
        private const string AileronPath = @"/controls/flight/aileron";
        private const string ThrottlePath = @"/controls/engines/current-engine/throttle";

        /* Command Templates (simulator queries templates) */
        private const string SetCommandTemplate = "set {0} {1}\r\n";
        private const string GetCommandTemplate = "get {0}\r\n";

        /* Error Templates */
        private const string ConnectionError = "Client is not connected";
        private const string NetworkStreamError = "Error: Cant get NetworkStream from TcpClient";

        /* Simulator Communication & Tasks Queue*/
        private readonly BlockingCollection<AsyncCommand> _queue;
        private readonly SimulatorConfig _simulatorConfig;
        private readonly TcpClient _client;
        private const int DefaultTimeout = 10000;
        private const int ReadBufferSize = 4 * 1024;
        private bool _running;

        /* Enum used for validation method */
        private enum VariableName
        {
            Aileron = 0,
            Rudder,
            Elevator,
            Throttle
        };

        /* CTor\DTor */
        public FlightGearAsyncClient(SimulatorConfig simulatorConfig)
        {
            _simulatorConfig = simulatorConfig;
            _queue = new BlockingCollection<AsyncCommand>();
            _client = new TcpClient();
            Start();
        }
        ~FlightGearAsyncClient()
        {
            if (_client.Connected) Disconnect();
        }

        /* ITcpClient Methods */
        public void Start()
        {
            _running = true;
            Task.Factory.StartNew(ProcessCommands);
        }
        public void Stop()
        {
            _running = false;
            Disconnect();
        }
        public void Write(Command cmd)
        {
            var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError);
            stream.WriteTimeout = DefaultTimeout;

            /* Prepare request string */
            var writeBuffer =
                                /* Set Requests */
                                string.Format(SetCommandTemplate, AileronPath, cmd.Aileron)
                              + string.Format(SetCommandTemplate, RudderPath, cmd.Rudder)
                              + string.Format(SetCommandTemplate, ElevatorPath, cmd.Elevator)
                              + string.Format(SetCommandTemplate, ThrottlePath, cmd.Throttle)
                              /* Get Requests */
                              + string.Format(GetCommandTemplate, AileronPath)
                              + string.Format(GetCommandTemplate, RudderPath)
                              + string.Format(GetCommandTemplate, ElevatorPath)
                              + string.Format(GetCommandTemplate, ThrottlePath);

            /* Send request */
            var writeBufferBytes = Encoding.ASCII.GetBytes(writeBuffer);
            stream.Write(writeBufferBytes, 0, writeBufferBytes.Length);
        }
        public string Read()
        {
            /* Set stream and buffer */
            var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError);
            stream.ReadTimeout = DefaultTimeout;
            var readBufferBytes = new byte[ReadBufferSize];

            /* Read data */
            var bytesRead = stream.Read(readBufferBytes, 0, ReadBufferSize);
            return Encoding.ASCII.GetString(readBufferBytes, 0, bytesRead);
        }

        /* IAsyncTcpClient Methods */
        public void ProcessCommands()
        {
            /* Parse port from config throw exception if fail */
            if (!int.TryParse(_simulatorConfig.TelnetPort, out var port))
                throw new Exception("Error parsing port");

            Connect(_simulatorConfig.Ip, port);

            /* Loop works as long as there are commands to process - o.w. blocks until receives */
            foreach (var cmd in _queue.GetConsumingEnumerable())
            {
                string readBuffer;
                try
                {
                    Write(cmd.Command);
                    readBuffer = Read();
                }
                catch (IOException ioe)
                {
                    cmd.Completion.SetException(ioe);
                    continue;
                }
                catch (Exception e)
                {
                    cmd.Completion.SetException(e);
                    continue;
                }

                var res = ValidateResults(cmd.Command, readBuffer);
                cmd.Completion.SetResult(res);
            }
        }
        public Task<Result> Execute(Command cmd)
        {
            var asyncCommand = new AsyncCommand(cmd);
            _queue.Add(asyncCommand);
            return asyncCommand.Task;
        }
        private static Result ValidateResults(Command cmd, string readBuffer)
        {
            /* Find all decimal (double) matches in returned response string */
            const string decimalRx = @"-?\d+(\.\d+)?";
            var matches = Regex.Matches(readBuffer, decimalRx);

            /* Iterates each match and compares to the actual value that was sent
             and check if anything went wrong */
            var curVar = VariableName.Aileron;
            foreach (var match in matches)
            {
                var receivedVal = double.Parse(match.ToString());


                var sentVal = curVar switch
                {
                    VariableName.Aileron => cmd.Aileron,
                    VariableName.Rudder => cmd.Rudder,
                    VariableName.Elevator => cmd.Elevator,
                    VariableName.Throttle => cmd.Throttle,
                    _ => 0
                };

                if (!sentVal.Equals(receivedVal)) return Result.NotOk;
                ++curVar;
            } // End of foreach loop

            return Result.Ok;
        }

        private void Connect(string ip, int port)
        {
            var attempt = 0; // Debug remove
            IAsyncResult result = null;

            /* Keeps trying to connect to a tcp client until succeeds */
            /**NOTE: once it is connected if connection was lost or simulator 
               was restarted you have to restart this mediator as well! */
            while (_running && !_client.Connected)
            {
                Debug.WriteLine($"TCP Client: Connect attempt #{attempt++}..."); // Debug remove
                result = _client.BeginConnect(ip, port, null, null);
                result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            }

            if (result == null)
                throw new Exception("Error connecting to telnet server");

            /* Connected successfully*/
            _client.EndConnect(result);
            Debug.WriteLine("TCP Client: Connected successfully to server..."); // Debug remove

            /*Set stream and sent first message to set raw data communication with simulator*/
            var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError);
            var initBuf = Encoding.ASCII.GetBytes("data\n");
            stream.Write(initBuf);
        }
        private void Disconnect()
        {
            _client.Close();
            /** Clear the queue (more info, look at mythz's answer):
             https://stackoverflow.com/questions/8001133/how-to-empty-a-blockingcollection
            */
            while (_queue.TryTake(out _)) { }

            Debug.WriteLine("TCP Client: Disconnected successfully to server...");
        }
    }
}
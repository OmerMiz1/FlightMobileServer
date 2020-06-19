using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FlightMobileServer.Models;

namespace FlightMobileServer.ClientModels {
    public class FlightGearAsyncClient : IAsyncTcpClient{
        /* Variables */
        private const string ElevatorPath = @"/controls/flight/elevator";
        private const string RudderPath = @"/controls/flight/rudder";
        private const string AileronPath = @"/controls/flight/aileron";
        private const string ThrottlePath = @"/controls/engines/current-engine/throttle";
        //private const string ThrottlePath = @"/controls/engines/engine/throttle"; // used this one before debugging with the simulator

        /* Command Templates */
        private const string SetCommandTemplate = "set {0} {1}\r\n";
        private const string GetCommandTemplate = "get {0}\r\n";
        
        /* Error Templates */
        private const string ConnectionError = "Client is not connected";
        private const string NetworkStreamError = "Error: Cant get NetworkStream from TcpClient";

        /* Simulator communication */
        private readonly BlockingCollection<AsyncCommand> _queue;
        private readonly TcpClient _client;
        private readonly SimulatorConfig _simulatorConfig;
        private const int DefaultTimeout = 10000;
        private bool _running;



        /* CTor\DTor */
        public FlightGearAsyncClient(SimulatorConfig simulatorConfig)
        {
            _simulatorConfig = simulatorConfig;
            _queue = new BlockingCollection<AsyncCommand>();
            _client = new TcpClient();
            Start();
        }
        ~FlightGearAsyncClient() {
            if(_client.Connected) Disconnect();
        }

        /* ITcpClient Methods */
        public void Start() {
            _running = true;
            Task.Factory.StartNew(ProcessCommands);
        }
        public void Stop() {
            _running = false;
            Disconnect();
        }
        public void Write(Command cmd) {
            var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError);
            stream.WriteTimeout = DefaultTimeout;

            var writeBuffer =
                                // Set requests
                                string.Format(SetCommandTemplate, AileronPath, cmd.Aileron) 
                              + string.Format(SetCommandTemplate, RudderPath, cmd.Rudder)
                              + string.Format(SetCommandTemplate, ElevatorPath, cmd.Elevator)
                              + string.Format(SetCommandTemplate, ThrottlePath, cmd.Throttle)
                                // Get requests
                              + string.Format(GetCommandTemplate, AileronPath) 
                              + string.Format(GetCommandTemplate, RudderPath)
                              + string.Format(GetCommandTemplate, ElevatorPath)
                              + string.Format(GetCommandTemplate, ThrottlePath);
            var writeBufferBytes = Encoding.ASCII.GetBytes(writeBuffer);
            stream.Write(writeBufferBytes, 0, writeBufferBytes.Length);
        }
        public string Read() {
            const int readBufferSize = 4 * 1024;
            var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError); ;
            stream.ReadTimeout = DefaultTimeout;

            var readBufferBytes = new byte[readBufferSize];
            var bytesRead = stream.Read(readBufferBytes, 0, readBufferSize);
            return Encoding.ASCII.GetString(readBufferBytes, 0, bytesRead);
        }

        /* IAsyncTcpClient Methods */
        public void ProcessCommands() {

            Connect(_simulatorConfig.Ip,int.Parse(_simulatorConfig.TelnetPort)); // debug not handling parse exceptions
            foreach (var cmd in _queue.GetConsumingEnumerable()) {
                var readBuffer = string.Empty;
                try {
                    Write(cmd.Command);
                    readBuffer = Read();
                }
                catch (IOException ioe) {
                    cmd.Completion.SetException(ioe);
                }
                catch (Exception e) {
                    cmd.Completion.SetException(e);
                    continue;
                }

                var res = ValidateResults(cmd.Command, readBuffer);
                cmd.Completion.SetResult(res);
            }
        }
        public Task<Result> Execute(Command cmd) {
            var asyncCommand = new AsyncCommand(cmd);
            _queue.Add(asyncCommand);
            return asyncCommand.Task;
        }
        private static Result ValidateResults(Command cmd, string readBuffer) {
            const string decimalRx = @"\d+(\.\d+)?";
            var matches = Regex.Matches(readBuffer, decimalRx);
            var matchEnum = matches.GetEnumerator();
            if (!matchEnum.MoveNext() || !matchEnum.Current.ToString().Equals(cmd.Aileron.ToString())) return Result.NotOk;
            if (!matchEnum.MoveNext() || !matchEnum.Current.ToString().Equals(cmd.Rudder.ToString())) return Result.NotOk;
            if (!matchEnum.MoveNext() || !matchEnum.Current.ToString().Equals(cmd.Elevator.ToString())) return Result.NotOk;
            if (!matchEnum.MoveNext() || !matchEnum.Current.ToString().Equals(cmd.Throttle.ToString())) return Result.NotOk;
            return Result.Ok;
        }

        private void Connect(string ip, int port) {
            var attempt = 0;
            IAsyncResult result = null;

            while (_running && !_client.Connected) {
                Debug.WriteLine($"TCP Client: Connect attempt #{attempt++}...");
                result = _client.BeginConnect(ip, port, null, null);
                result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            }

            if (result == null)
                throw new Exception("Error connecting to telnet server");

            _client.EndConnect(result);
            Debug.WriteLine("TCP Client: Connected successfully to server...");

            var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError);

            var initBuf = Encoding.ASCII.GetBytes("data\n");
            stream.Write(initBuf);
        }
        private void Disconnect()
        {
            _client.Close();
            // _setRequests.Clear(); todo clear\wait all _queue
            Debug.WriteLine("TCP Client: Disconnected successfully to server...");
        }
    }
}
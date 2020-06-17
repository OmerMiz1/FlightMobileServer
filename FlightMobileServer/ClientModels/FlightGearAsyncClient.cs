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
        private const string ThrottlePath = @"/controls/engines/engine/throttle";

        /* Command Templates */
        private const string SetCommandTemplate = "set {0} {1} \r\n";
        private const string GetCommandTemplate = "get {0} \r\n";
        
        /* Error Templates */
        private const string ConnectionError = "Client is not connected";
        private const string NetworkStreamError = "Error: Cant get NetworkStream from TcpClient";

        /* Simulator communication */
        private readonly BlockingCollection<AsyncCommand> _queue;
        private readonly TcpClient _client;
        public string Ip { get; set; }//OLD
        public int Port { get; set; }//OLD
        private const int DefaultTimeout = 10000;

        /* CTor\DTor */
        public FlightGearAsyncClient()
        {
            _queue = new BlockingCollection<AsyncCommand>();
            _client = new TcpClient();
        }
        ~FlightGearAsyncClient() {
            if(Connected) Disconnect();
        }

        /* ITcpClient Methods */
        public void Start() {
            Task.Factory.StartNew(ProcessCommands);
        }
        public void Stop() {
            Disconnect();
        }
        public void Write(Command cmd) {
            using var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError);
            stream.WriteTimeout = DefaultTimeout;

            var writeBuffer = string.Format(SetCommandTemplate, AileronPath, cmd.Aileron) // Set requests
                              + string.Format(SetCommandTemplate, RudderPath, cmd.Rudder)
                              + string.Format(SetCommandTemplate, ElevatorPath, cmd.Elevator)
                              + string.Format(SetCommandTemplate, ThrottlePath, cmd.Throttle)
                              + string.Format(GetCommandTemplate, AileronPath) // Get requests
                              + string.Format(GetCommandTemplate, RudderPath)
                              + string.Format(GetCommandTemplate, ElevatorPath)
                              + string.Format(GetCommandTemplate, ThrottlePath);
            var writeBufferBytes = Encoding.ASCII.GetBytes(writeBuffer);
            stream.Write(writeBufferBytes, 0, writeBufferBytes.Length);
        }
        public string Read() {
            const int readBufferSize = 4 * 1024;
            using var stream = _client.GetStream();
            if (stream == null) throw new Exception(NetworkStreamError); ;
            stream.ReadTimeout = DefaultTimeout;

            var readBufferBytes = new byte[readBufferSize];
            var bytesRead = stream.Read(readBufferBytes, 0, readBufferSize);
            return Encoding.ASCII.GetString(readBufferBytes, 0, bytesRead);
        }

        /* IAsyncTcpClient Methods */
        public void ProcessCommands() {
            
            Connect(Ip,Port);
            foreach (var cmd in _queue.GetConsumingEnumerable()) {
                var readBuffer = string.Empty;
                try {
                    Write(cmd.Command);
                    readBuffer = Read();
                }
                catch (IOException ioe) {
                    cmd.Completion.SetException(ioe);
                    cmd.Completion.SetResult(Result.NotOk);
                }
                catch (Exception e) {
                    cmd.Completion.SetException(e);
                    cmd.Completion.SetResult(Result.NotOk);
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
            if (!matchEnum.MoveNext() || !matchEnum.Current.Equals(cmd.Aileron)) return Result.NotOk;
            if (!matchEnum.MoveNext() || !matchEnum.Current.Equals(cmd.Rudder)) return Result.NotOk;
            if (!matchEnum.MoveNext() || !matchEnum.Current.Equals(cmd.Elevator)) return Result.NotOk;
            if (!matchEnum.MoveNext() || !matchEnum.Current.Equals(cmd.Throttle)) return Result.NotOk;
            return Result.Ok;
        }

        private void Connect(string ip, int port)
        {
            _client.Connect(ip, port);
            Debug.WriteLine("TCP Client: Connected successfully to server...");

            using var stream = _client.GetStream();
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
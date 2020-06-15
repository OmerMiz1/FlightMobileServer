using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace FlightMobileServer.ServerModels
{
    public class SimulatorModel : ITcpClient, INotifyPropertyChanged {
        
        /* Server related fields */
        private NetworkStream _stream;
        private Boolean _running = false;
        public string Ip { get; set; }
        public int Port { get; set; }
        public Boolean Connected {
            get => _tcpClient.Connected;
        }

        private readonly BlockingCollection<AsyncCommand> _queue;
        private readonly TcpClient _tcpClient;

        private static Mutex mtx = new Mutex(); // debug remove all uses?

        /* Variables related fields */
        private DictionaryIndexer _variables; // todo replace with hardcoded properties
        private Queue<string> _setRequests = new Queue<string>(); // todo replace all uses with blocking queue
        private VariableNamesManager _varNamesMgr = new VariableNamesManager(); // debug remove

        public event PropertyChangedEventHandler PropertyChanged; //debug remove all uses?
        public event PropertyChangedEventHandler ConnectionChanged; //debug remove all uses?

        public SimulatorModel() {
            InitVariables();
            _tcpClient = new TcpClient();
            _queue = new BlockingCollection<AsyncCommand>();
        }

        // todo validate params
        public void Connect(string ip, int port) { 
            try
            { /* Connect to server */
                _tcpClient.Connect(ip, port);
                NotifyConnectionChanged("Connected");
                Debug.WriteLine("TCP Client: Connected successfully to server...");
            }
            catch (SocketException se)
            { /* Usually this error points that server is not on yet */
                NotifyConnectionChanged("Error: Connection Failed, try to turn on the server -> Click on 'Connect'");
                return;
            }
            catch (Exception e)
            { /* Unexpected error */
                NotifyConnectionChanged("Error: Connection Failed, unexpected error occured");
                return;
            }

            /* Check connection & Start communicating. NOTE:
               Named the thread for easier debugging */
            if (!_tcpClient.Connected) return;
            var t = new Thread(Start) {Name = "SimulatorModel.Start Thread"};
            t.Start();
        }

        public void Connect() {
            Connect(Ip, Port);
        }

        public void Disconnect() {
            Stop();
            _tcpClient.Close();
            _setRequests.Clear();
            NotifyConnectionChanged("Disconnected");
            Debug.WriteLine("TCP Client: Disconnected successfully to server...");
        }

        public string Read() {
            /* Double check reading is possible */
            if (_stream.CanRead) {
                byte[] readBuffer = new byte[4096];
                int bytesRead = 0;
                StringBuilder strBuilder = new StringBuilder();

                /** Read all data sent from simulator
                 * NOTE: Probably there's no need in the string builder
                 * because the messaged received from sim are short.
                 * Still i think its a good idea just in-case.
                 **/
                try {
                    bytesRead = _stream.Read(readBuffer, 0, readBuffer.Length);
                    strBuilder.AppendFormat("{0}", Encoding.ASCII.GetString(readBuffer, 0, bytesRead));
                }
                catch (Exception e) { /* Error occured - might be server shut down unexpectedly */
                    NotifyConnectionChanged("Error: Connection to server lost\\broken");
                    Stop();
                    /* TODO Unknown exception thrown, might be because server was shut down mid way*/
                }
                return strBuilder.ToString();
            }

            /*TODO Determine if its an Error or Warning !!! */
            NotifyPropertyChanged("Warning: Network stream unable to read from server");
            return null;
        }
        public void Write(string msg) {
            /* Double check writing is possible */
            if (_stream.CanWrite) {
                try {
                    byte[] writeBuffer = Encoding.ASCII.GetBytes(msg + "\r\n");
                    _stream.Write(writeBuffer, 0, writeBuffer.Length);
                }
                catch (Exception e) { /* Error occured - might be server shut down unexpectedly */
                    NotifyConnectionChanged("Error: Connection to server lost\\broken");
                    Stop();
                }
            }
            else { /*TODO Determine if its an Error or Warning !!! */
                NotifyConnectionChanged("Warning: Network stream unable to write to server");
            }
        }

        public void Start() {
            _running = true;

            /* Create stream & set timeout to 10 seconds */
            _stream = _tcpClient.GetStream();
            _stream.ReadTimeout = 10;

            /* Start getting data from simulator (continuously) */
            Thread getValuesThread = new Thread(ReadValuesFromSim);
            getValuesThread.Start();

            /* Start responding to set requests from user input (GUI actually) */
            Thread setValuesThread = new Thread(WriteValuesToSim);
            setValuesThread.Start();

            getValuesThread.Join();
            setValuesThread.Join();

            Disconnect();
        }

        public void Stop() {
            _running = false;
        }

        public void NotifyPropertyChanged(string propName) {
            if (_varNamesMgr.toName(propName) != "Variable Not Found") {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_varNamesMgr.toName(propName)));
            }
            else {
                /*TODO WHAT SHOULD WE PRINT IF HAPPENS?*/
                Debug.WriteLine("Some Weird BUG", Thread.CurrentThread.Name);
            }
        }

        public void NotifyConnectionChanged(string connected) {
            ConnectionChanged?.Invoke(this, new PropertyChangedEventArgs(connected));
        }

        /* Personal Implementations */
        private void ReadValuesFromSim() {
            StringBuilder strBuilder = new StringBuilder();
            List<string> paths = new List<string>();
            List<string>.Enumerator pathEnum;
            string requestMsg, valuesFromSim;

            /** Build get requests message (once). More info at:
             *  https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2.system-collections-idictionary-getenumerator?view=netframework-4.8
             */
            var entry = _variables.GetEnumerator();
            while (entry.MoveNext()) {
                /*TODO: Possible problem, possible fix =  use AppendLine*/
                strBuilder.Append("get " + entry.Key + "\r\n");
                paths.Add((string) entry.Key);
            }

            requestMsg = strBuilder.ToString();

            /* Request values from simulator every 100ms */
            while (_running) {
                /* TODO maybe need to show notification to user? */
                if (!_tcpClient.Connected) {
                    Debug.WriteLine("Error #1 SimulatorModel.Start()...");
                }

                /* Send request for updates & read response */
                mtx.WaitOne();
                Write(requestMsg);
                valuesFromSim = Read();
                mtx.ReleaseMutex();

                /* Enumerate each variable manually (iterator)*/
                pathEnum = paths.GetEnumerator();
                pathEnum.MoveNext();

                /* Split received values and update each one */
                List<string> newValsArray = valuesFromSim.Split("\n").ToList();
                foreach (string newVal in newValsArray) {
                    if (newVal == "ERR") {
                        /*TODO notify user about error for 2 ~ 5 seconds and make it disappear. */
                    }
                    else if (newVal == "") {
                        // DO NOTHING
                    }
                    else if (_variables.ContainsKey(pathEnum.Current)) {
                        _variables[pathEnum.Current] = newVal;
                        NotifyPropertyChanged(pathEnum.Current);
                    }
                    else {
                        /*TODO Make sure we notify user (with GUI text box/smthing..) about an error!!!! */
                        Console.WriteLine("ERR");
                    }

                    if (!pathEnum.MoveNext()) {
                        break;
                    }
                }

                pathEnum.Dispose();
                Thread.Sleep(100);
            }
        }

        /**
         *TODO Its not supposed to be like that.
         *TODO Each property update should be triggered from VM and updated as event.
         **/
        private void WriteValuesToSim() {
            while (_running) {
                if (_setRequests.Count != 0) {
                    /* Send requested value change and read respond afterwards */
                    string request = _setRequests.Dequeue();
                    mtx.WaitOne();
                    Write("set " + request);
                    string response = Read();
                    mtx.ReleaseMutex();

                    /* Check if request was valid. */
                    if (response != request) {
                        /*TODO debug*/
                        Debug.WriteLine("response= " + response);
                        Debug.WriteLine("request= " + request);
                    }
                }
            }
        }

        private void InitVariables() {
            /* Initializes NAMES of variables that model is GETTING FROM simulator */
            _variables = new DictionaryIndexer {
                ["/instrumentation/heading-indicator/indicated-heading-deg"] = "NO_VALUE_YET",
                ["/instrumentation/gps/indicated-vertical-speed"] = "NO_VALUE_YET",
                ["/instrumentation/gps/indicated-ground-speed-kt"] = "NO_VALUE_YET",
                ["/instrumentation/airspeed-indicator/indicated-speed-kt"] = "NO_VALUE_YET",
                ["/instrumentation/gps/indicated-altitude-ft"] = "NO_VALUE_YET",
                ["/instrumentation/attitude-indicator/internal-roll-deg"] = "NO_VALUE_YET",
                ["/instrumentation/attitude-indicator/internal-pitch-deg"] = "NO_VALUE_YET",
                ["/instrumentation/altimeter/indicated-altitude-ft"] = "NO_VALUE_YET",
                ["/position/longitude-deg"] = "NO_VALUE_YET",
                ["/position/latitude-deg"] = "NO_VALUE_YET",
            };
        }

        public string GetVariable(string varName) {
            return _variables[_varNamesMgr.toPath(varName)];
        }

        public void SetVariable(string varName, string varValue) {
            string varPath = _varNamesMgr.toPath(varName);
            _setRequests.Enqueue(varPath + " " + varValue);
            
        }
    }
}
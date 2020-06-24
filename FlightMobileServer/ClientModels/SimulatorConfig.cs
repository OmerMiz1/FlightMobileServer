using Microsoft.Extensions.Configuration;

namespace FlightMobileServer.ClientModels
{

    /* Data Object */
    public class SimulatorConfig
    {
        public SimulatorConfig(IConfiguration config)
        {
            Ip = config["Ip"];
            TelnetPort = config["TelnetPort"];
            HttpPort = config["HttpPort"];
        }

        public string Ip { get; set; }
        public string TelnetPort { get; set; }
        public string HttpPort { get; set; }
    }
}
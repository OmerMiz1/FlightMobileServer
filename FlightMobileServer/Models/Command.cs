
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FlightMobileServer.Models {
    public class Command {
        [JsonProperty("aileron")]
        // [Range(-1,1)]
        public double Aileron { get; set; }
        [JsonProperty("rudder")]
        // [Range(-1, 1)]
        public double Rudder { get; set; }
        [JsonProperty("elevator")]
        // [Range(-1,1)]
        public double Elevator{ get; set; }
        [JsonProperty("throttle")]
        // [Range(0,1)]
        public double Throttle { get; set; }


        /*private string aileronOutOfRangeMsg = "aileron must be between -1 to 1."; //debug
        private string rudderOutOfRangeMsg = "rudder must be between -1 to 1.";//debug
        private string elevatorOutOfRangeMsg = "elevator must be between -1 to 1.";//debug
        private string throttleOutOfRangeMsg = "throttle must be between 0 to 1.";//debug*/
    }
}
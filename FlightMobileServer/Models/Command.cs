
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FlightMobileServer.Models {
    public class Command {

        [JsonProperty("aileron")]
        [Range(-1, 1, ErrorMessage = AileronOutOfRangeMsg)]
        public double Aileron { get; set; }

        [JsonProperty("rudder")]
        [Range(-1, 1, ErrorMessage = RudderOutOfRangeMsg)]
        public double Rudder { get; set; }

        [JsonProperty("elevator")]
        [Range(-1, 1, ErrorMessage = ElevatorOutOfRangeMsg)]
        public double Elevator{ get; set; }

        [JsonProperty("throttle")]
        [Range(0, 1, ErrorMessage = ThrottleOutOfRangeMsg)]
        public double Throttle { get; set; }

        /* Error Messages */
        private const string AileronOutOfRangeMsg = "aileron must be between -1 to 1.";
        private const string RudderOutOfRangeMsg = "rudder must be between -1 to 1.";
        private const string ElevatorOutOfRangeMsg = "elevator must be between -1 to 1.";
        private const string ThrottleOutOfRangeMsg = "throttle must be between 0 to 1.";
    }
}
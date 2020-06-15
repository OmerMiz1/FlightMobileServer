
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace FlightMobileServer.Models {
    public class Command {

        [JsonProperty("aileron")]
        [Range(-1, 1, ErrorMessage = _aileronOutOfRangeMsg)]
        public double Aileron { get; set; }

        [JsonProperty("rudder")]
        [Range(-1, 1, ErrorMessage = _rudderOutOfRangeMsg)]
        public double Rudder { get; set; }

        [JsonProperty("elevator")]
        [Range(-1, 1, ErrorMessage = _elevatorOutOfRangeMsg)]
        public double Elevator{ get; set; }

        [JsonProperty("throttle")]
        [Range(0, 1, ErrorMessage = _throttleOutOfRangeMsg)]
        public double Throttle { get; set; }

        /* Error Messages */
        private const string _aileronOutOfRangeMsg = "aileron must be between -1 to 1.";
        private const string _rudderOutOfRangeMsg = "rudder must be between -1 to 1.";
        private const string _elevatorOutOfRangeMsg = "elevator must be between -1 to 1.";
        private const string _throttleOutOfRangeMsg = "throttle must be between 0 to 1.";
    }
}
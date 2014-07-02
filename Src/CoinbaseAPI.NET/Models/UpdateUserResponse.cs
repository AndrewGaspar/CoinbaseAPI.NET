using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class UpdateUserResponse : RequestResponse
    {
        [JsonProperty("user")]
        public UserEntity User { get; set; }
    }
}

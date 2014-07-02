using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class AddUserEntity : ShortUserEntity
    {
        [JsonProperty("receive_address")]
        public string ReceiveAddress { get; set; }
    }

    public class CreateUserResponse : RequestResponse
    {
        [JsonProperty("user")]
        public AddUserEntity User { get; set; }
    }
}

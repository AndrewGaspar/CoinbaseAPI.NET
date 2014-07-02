using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class UpdateUserRequest
    {
        public class Details
        {
            [JsonProperty("name"), Optional]
            public string Name { get; set; }

            [JsonProperty("email"), Optional]
            public string Email { get; set; }

            [JsonProperty("pin"), Optional]
            public string Pin { get; set; }

            [JsonProperty("native_currency"), Optional]
            public string NativeCurrency { get; set; }

            [JsonProperty("time_zone"), Optional]
            public string TimeZone { get; set; }
        }

        [JsonProperty("user"), Required]
        public Details User { get; set; }
    }
}

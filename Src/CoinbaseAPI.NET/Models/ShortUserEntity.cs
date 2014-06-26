using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class ShortUserEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}

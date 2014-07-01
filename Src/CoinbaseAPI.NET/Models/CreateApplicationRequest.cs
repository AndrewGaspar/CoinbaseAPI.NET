using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class CreateApplicationRequest
    {
        public class Details
        {
            [JsonProperty("name"), Required]
            public string Name { get; set; }

            [JsonProperty("redirect_uri"), Required]
            public string RedirectUri { get; set; }
        }

        [JsonProperty("application"), Required]
        public Details Application { get; set; }
    }
}

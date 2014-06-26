using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class CreateApplicationDetails
    {
        [JsonProperty("name"), Required]
        public string Name { get; set; }

        [JsonProperty("redirect_uri"), Required]
        public string RedirectUri { get; set; }
    }

    public class CreateApplicationRequest
    {
        [JsonProperty("application"), Required]
        public CreateApplicationDetails Application { get; set; }
    }
}

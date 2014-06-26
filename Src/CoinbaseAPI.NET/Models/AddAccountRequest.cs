using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class AddAccountDetails
    {
        [JsonProperty("name"), Required]
        public string Name { get; set; }
    }

    public class AddAccountRequest
    {
        [JsonProperty("account"), Required]
        public AddAccountDetails Account { get; set; }
    }
}

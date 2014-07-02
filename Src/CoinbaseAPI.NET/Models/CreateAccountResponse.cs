using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class CreateAccountResponse : RequestResponse
    {
        [JsonProperty("account")]
        public AccountResponse Account { get; set; }
    }
}

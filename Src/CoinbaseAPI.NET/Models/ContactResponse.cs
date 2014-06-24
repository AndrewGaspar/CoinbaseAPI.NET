using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    public class ContactResponse
    {
        [JsonProperty("contact")]
        public ContactResponseEntity Contact { get; set; }
    }

    public class ContactResponseEntity 
    {
        [JsonProperty("email")]
        public string Email { get; set; }
    }
}

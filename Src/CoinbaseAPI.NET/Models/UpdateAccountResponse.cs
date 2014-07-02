using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class UpdateAccountResponse : RequestResponse
    {
        [JsonProperty("account")]
        public AccountResponse Account { get; set; }
    }
}

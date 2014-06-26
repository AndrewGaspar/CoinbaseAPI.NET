using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class ModifyAccountResponse : RequestResponse
    {
        [JsonProperty("account")]
        public AccountResponse Account { get; set; }
    }
}

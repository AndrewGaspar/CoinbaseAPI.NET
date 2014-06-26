using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class AddUserDetails
    {
        [JsonProperty("email"), Required]
        public string Email { get; set; }

        [JsonProperty("password"), Required]
        public string Password { get; set; }

        [JsonProperty("referrer_id"), Optional]
        public string ReferrerId { get; set; }
    }

    public class AddUserRequest
    {
        [JsonProperty("user"), Required]
        public AddUserDetails User { get; set; }

        [JsonProperty("client_id"), Optional]
        public string ClientId { get; set; }

        [JsonProperty("scopes"), Optional]
        public string Scopes { get; set; }
    }
}

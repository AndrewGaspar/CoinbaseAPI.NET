using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class AddUserRequest
    {
        public class Details
        {
            [JsonProperty("email"), Required]
            public string Email { get; set; }

            [JsonProperty("password"), Required]
            public string Password { get; set; }

            [JsonProperty("referrer_id"), Optional]
            public string ReferrerId { get; set; }
        }

        [JsonProperty("user"), Required]
        public Details User { get; set; }

        [JsonProperty("client_id"), Optional]
        public string ClientId { get; set; }

        [JsonProperty("scopes"), Optional]
        public string Scopes { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class UpdateAccountDetails
    {
        [JsonProperty("name"), Required]
        public string Name { get; set; }
    }

    public class UpdateAccountRequest
    {
        [JsonProperty("account"), Required]
        public UpdateAccountDetails Account { get; set; }
    }
}

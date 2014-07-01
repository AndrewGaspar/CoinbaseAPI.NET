using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class UpdateAccountRequest
    {
        public class Details
        {
            [JsonProperty("name"), Required]
            public string Name { get; set; }
        }

        [JsonProperty("account"), Required]
        public Details Account { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class AccountsResponse : PaginatedResponse
    {
        [JsonProperty("accounts")]
        public IList<AccountResponse> Accounts { get; set; }
    }
}

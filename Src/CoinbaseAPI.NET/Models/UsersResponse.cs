using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class UsersResponse
    {
        [JsonProperty("users")]
        public IList<UserResponse> Users { get; set; }
    }
}

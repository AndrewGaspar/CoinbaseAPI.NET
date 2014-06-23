using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class UserResponseWrapper
    {
        [JsonProperty("user")]
        public UserResponse User { get; set; }
    }

    public class UsersResponse
    {
        [JsonProperty("users")]
        public IList<UserResponseWrapper> Users { get; set; }
    }
}

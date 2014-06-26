using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class UsersResponse
    {
        [JsonProperty("users")]
        public IList<UserResponse> Users { get; set; }
    }
}

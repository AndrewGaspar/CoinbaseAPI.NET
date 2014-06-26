using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class AddUserEntity : ShortUserEntity
    {
        [JsonProperty("receive_address")]
        public string ReceiveAddress { get; set; }
    }

    public class AddUserResponse : RequestResponse
    {
        [JsonProperty("user")]
        public AddUserEntity User { get; set; }
    }
}

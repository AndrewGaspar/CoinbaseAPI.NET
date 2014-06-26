using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public interface IRequestResponse
    {
        bool Success { get; set; }
        IList<string> Errors { get; set; }
    }

    public class RequestResponse : IRequestResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("errors")]
        public IList<string> Errors { get; set; }
    }
}

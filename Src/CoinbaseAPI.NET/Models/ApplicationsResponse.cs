using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class ApplicationsResponse : PaginatedResponse
    {
        [JsonProperty("applications")]
        public IList<ApplicationResponse> Applications { get; set; }
    }
}

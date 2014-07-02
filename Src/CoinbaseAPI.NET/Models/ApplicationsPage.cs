using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class ApplicationsPage : RecordsPage
    {
        [JsonProperty("applications")]
        public IList<ApplicationResponse> Applications { get; set; }
    }
}

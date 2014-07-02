using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class TransfersPage : RecordsPage
    {
        [JsonProperty("transfers")]
        public IList<TransferResponse> Transfers { get; set; }
    }
}

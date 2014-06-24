using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class TransfersResponse : PaginatedResponse
    {
        [JsonProperty("transfers")]
        public IList<TransferResponse> Transfers { get; set; }
    }
}

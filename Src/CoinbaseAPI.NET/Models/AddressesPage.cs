using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class AddressesPage : RecordsPage
    {
        [JsonProperty("addresses")]
        public IList<AddressResponse> Addresses { get; set; }
    }
}

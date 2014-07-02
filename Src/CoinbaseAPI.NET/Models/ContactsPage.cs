using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class ContactsPage : RecordsPage
    {
        [JsonProperty("contacts")]
        public IList<ContactResponse> Contacts { get; set; }
    }
}

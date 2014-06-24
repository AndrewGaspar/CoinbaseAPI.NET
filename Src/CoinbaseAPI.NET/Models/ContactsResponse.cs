using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    public class ContactsResponse : PaginatedResponse
    {
        [JsonProperty("contacts")]
        public IList<ContactResponse> Contacts { get; set; }
    }
}

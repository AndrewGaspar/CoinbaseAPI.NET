using Newtonsoft.Json;
using System;

namespace Bitlet.Coinbase.Models
{
    public class ApplicationResponseWrapper
    {
        [JsonProperty("application")]
        public ApplicationResponse Application { get; set; }
    }

    public class ApplicationResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("redirect_uri")]
        public string RedirectUri { get; set; }

        [JsonProperty("num_users")]
        public int NumUsers { get; set; }
    }
}

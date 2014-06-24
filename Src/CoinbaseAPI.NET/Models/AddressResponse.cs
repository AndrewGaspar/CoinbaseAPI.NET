using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Bitlet.Coinbase.Models
{
    public class AddressResponse
    {
        [JsonProperty("address")]
        public AddressEntity Address { get; set; }
    }

    public class AddressEntity
    {
        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("callback_url")]
        public string CallbackUrl { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("created_at"), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime CreatedAt { get; set; }
    }
}

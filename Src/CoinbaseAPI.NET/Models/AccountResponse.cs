using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    /// <summary>
    /// https://coinbase.com/api/doc/1.0/accounts.html
    /// </summary>
    public class AccountResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("balance"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> Balance { get; set; }

        [JsonProperty("native_balance")]
        public NativeCurrencyResponse NativeBalance { get; set; }

        [JsonProperty("created_at"), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("primary")]
        public bool Primary { get; set; }

        [JsonProperty("active")]
        public bool Active { get; set; }
    }
}

using Newtonsoft.Json;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    public class UserResponse
    {
        [JsonProperty("user")]
        public UserEntity User { get; set; }
    }

    public class ShortUserEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }

    public class UserEntity : ShortUserEntity
    {
        [JsonProperty("time_zone")]
        public string TimeZone { get; set; }

        [JsonProperty("native_currency")]
        public string NativeCurrency { get; set; }

        [JsonProperty("balance"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> Balance { get; set; }

        [JsonProperty("buy_level")]
        public int BuyLevel { get; set; }

        [JsonProperty("sell_level")]
        public int SellLevel { get; set; }

        [JsonProperty("buy_limit"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> BuyLimit { get; set; }

        [JsonProperty("sell_limit"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> SellLimit { get; set; }
    }
}

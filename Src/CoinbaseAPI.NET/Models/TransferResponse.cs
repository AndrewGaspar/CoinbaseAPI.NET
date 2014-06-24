using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    public enum TransferType
    {
        //[EnumMember(Value = "Buy")]
        Buy,

        //[EnumMember(Value = "Sell")]
        Sell
    }

    public enum TransferStatus
    {
        Pending,
        Completed,
        Canceled,
        Reversed
    }

    public class TransferResponse
    {
        [JsonProperty("transfer")]
        public TransferEntity Transfer { get; set; }
    }

    public class FeeEntity
    {
        [JsonProperty("cents")]
        public int Cents { get; set; }

        [JsonProperty("currency_iso")]
        public string CurrencyIso { get; set; }
    }

    public class FeesEntity
    {
        [JsonProperty("coinbase")]
        public FeeEntity Coinbase { get; set; }

        [JsonProperty("bank")]
        public FeeEntity Bank { get; set; }
    }

    public class TransferEntity
    {
        [JsonProperty("type")]
        public TransferType Type { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("created_at"), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("fees")]
        public FeesEntity Fees { get; set; }

        [JsonProperty("payout_date"), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime PayoutDate { get; set; }

        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("status")]
        public TransferStatus Status { get; set; }

        [JsonProperty("btc"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> BTC { get; set; }

        [JsonProperty("subtotal")]
        public NativeCurrencyResponse Subtotal { get; set; }

        [JsonProperty("total")]
        public NativeCurrencyResponse Total { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}

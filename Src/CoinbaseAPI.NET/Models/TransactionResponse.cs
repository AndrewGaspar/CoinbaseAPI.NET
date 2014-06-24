using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TransactionStatus
    {
        [EnumMember(Value = "pending")]
        Pending,

        [EnumMember(Value = "complete")]
        Complete
    }

    public class TransactionResponse
    {
        [JsonProperty("transaction")]
        public TransactionEntity Transaction { get; set; }
    }

    public class TransactionEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("created_at"), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime CreatedAt { get; set; }
        
        [JsonProperty("hsh")]
        public string Hsh { get; set; }

        [JsonProperty("amount"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> Amount { get; set; }

        [JsonProperty("request")]
        public bool Request { get; set; }

        [JsonProperty("status")]
        public TransactionStatus Status { get; set; }

        [JsonProperty("sender")]
        public ShortUserEntity Sender { get; set; }

        [JsonProperty("recipient")]
        public ShortUserEntity Recipient { get; set; }

        [JsonProperty("recipient_address")]
        public string RecipientAddress { get; set; }
    }
}

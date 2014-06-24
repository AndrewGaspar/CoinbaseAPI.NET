using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    public enum TransactionStatus
    {
        Pending,
        Complete
    }

    public class TransactionResponse
    {
        [JsonProperty("transaction")]
        public TransactionResponseEntity Transaction { get; set; }
    }

    public class TransactionStatusEnumConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var enumString = (string)reader.Value;

            switch(enumString) {
                case "pending": return TransactionStatus.Pending;
                case "complete": return TransactionStatus.Complete;
                default: return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            TransactionStatus status = (TransactionStatus)value;
            switch (status)
            {
                case TransactionStatus.Pending:
                    writer.WriteValue("pending");
                    break;
                case TransactionStatus.Complete:
                    writer.WriteValue("complete");
                    break;
            }
        }
    }

    public class TransactionResponseEntity
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

        [JsonProperty("status"), JsonConverter(typeof(TransactionStatusEnumConverter))]
        public TransactionStatus Status { get; set; }

        [JsonProperty("sender")]
        public ShortUserResponse Sender { get; set; }

        [JsonProperty("recipient")]
        public ShortUserResponse Recipient { get; set; }

        [JsonProperty("recipient_address")]
        public string RecipientAddress { get; set; }
    }
}

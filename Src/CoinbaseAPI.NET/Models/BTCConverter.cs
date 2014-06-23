using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.Contracts;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    public class BTCConverter : JsonConverter
    {

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FixedPrecisionUnit<Bitcoin.BTC>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            var currency = (string)obj["currency"];

            Contract.Assert(currency == "BTC", "Currency from Coinbase must be BTC.");

            var balance = (decimal)obj["amount"];

            return new FixedPrecisionUnit<Bitcoin.BTC>(balance);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var btc = (FixedPrecisionUnit<Bitcoin.BTC>)value;

            var obj = new JObject();
            obj["currency"] = "BTC";
            obj["amount"] = btc.Value;

            obj.WriteTo(writer);
        }

        
    }
}

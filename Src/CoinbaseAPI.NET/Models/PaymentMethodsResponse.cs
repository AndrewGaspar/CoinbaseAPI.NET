using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Models
{
    public class PaymentMethodResponse
    {
        [JsonProperty("payment_method")]
        public PaymentMethodEntity PaymentMethod { get; set; }
    }

    public class PaymentMethodEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("can_buy")]
        public bool CanBuy { get; set; }

        [JsonProperty("can_sell")]
        public bool CanSell { get; set; }
    }

    public class PaymentMethodsResponse
    {
        [JsonProperty("payment_methods")]
        public IList<PaymentMethodResponse> PaymentMethods { get; set; }

        [JsonProperty("default_buy")]
        public string DefaultBuy { get; set; }

        [JsonProperty("default_sell")]
        public string DefaultSell { get; set; }
    }
}

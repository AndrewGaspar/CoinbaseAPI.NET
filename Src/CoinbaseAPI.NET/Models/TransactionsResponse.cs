using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    public interface ITransactionsResponse
    {
        ShortUserEntity CurrentUser { get; set; }

        FixedPrecisionUnit<Bitcoin.BTC> Balance { get; set; }

        NativeCurrencyResponse NativeBalance { get; set; }

        IList<TransactionResponse> Transactions { get; set; }
    }

    public class TransactionsResponse : PaginatedResponse, ITransactionsResponse
    {
        [JsonProperty("current_user")]
        public ShortUserEntity CurrentUser { get; set; }

        [JsonProperty("balance"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> Balance { get; set; }

        [JsonProperty("native_balance")]
        public NativeCurrencyResponse NativeBalance { get; set; }

        [JsonProperty("transactions")]
        public IList<TransactionResponse> Transactions { get; set; }
    }
}

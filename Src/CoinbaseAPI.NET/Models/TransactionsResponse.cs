using Newtonsoft.Json;
using System.Collections.Generic;

namespace Bitlet.Coinbase.Models
{
    using Primitives;

    public class TransactionResponse
    {
        [JsonProperty("transaction")]
        public TransactionResponseItem Transaction { get; set; }
    }

    public interface ITransactionsResponse
    {
        ShortUserResponse CurrentUser { get; set; }

        FixedPrecisionUnit<Bitcoin.BTC> Balance { get; set; }

        NativeBalanceResponse NativeBalance { get; set; }

        IList<TransactionResponse> Transactions { get; set; }
    }

    public class TransactionsResponse : PaginatedResponse, ITransactionsResponse
    {
        [JsonProperty("current_user")]
        public ShortUserResponse CurrentUser { get; set; }

        [JsonProperty("balance"), JsonConverter(typeof(BTCConverter))]
        public FixedPrecisionUnit<Bitcoin.BTC> Balance { get; set; }

        [JsonProperty("native_balance")]
        public NativeBalanceResponse NativeBalance { get; set; }

        [JsonProperty("transactions")]
        public IList<TransactionResponse> Transactions { get; set; }
    }
}

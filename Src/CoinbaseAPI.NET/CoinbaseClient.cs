using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;

    using Primitives;
    using Utilities;

    internal static class HttpValueCollectionExtensionsForCoinbase
    {
        public static void SetAccountId(this HttpValueCollection coll, string accountId)
        {
            if (accountId != null)
            {
                coll.AddOrUpdate("account_id", accountId);
            }
        }

        public static void SetQuery(this HttpValueCollection coll, string query)
        {
            if (query != null)
            {
                coll.AddOrUpdate("query", query);
            }
        }

        public static void SetPage(this HttpValueCollection coll, int? page)
        {
            if (page.HasValue)
            {
                coll.AddOrUpdate("page", page.Value.ToString());
            }
        }

        private static void SetLimit(this HttpValueCollection coll, int? limit)
        {
            if (limit.HasValue)
            {
                coll.AddOrUpdate("limit", limit.Value.ToString());
            }
        }
    }

    public class CoinbaseMessageHandler : DelegatingHandler
    {
        private readonly ICoinbaseTokenProvider tokenProvider;

        public CoinbaseMessageHandler(ICoinbaseTokenProvider provider)
            : base(new HttpClientHandler())
        {
            tokenProvider = provider;
        }

        private async Task<string> AppendAccessTokenAsync(string query)
        {
            var collection = HttpUtility.ParseQueryString(query);
            var accessToken = await tokenProvider.GetAccessTokenAsync();
            if (collection.ContainsKey("access_token"))
            {
                collection["access_token"] = accessToken;
            }
            else
            {
                collection.Add("access_token", accessToken);
            }
            return collection.ToString();
        }

        private async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            var uriBuilder = new UriBuilder(request.RequestUri)
            {
                Query = await AppendAccessTokenAsync(request.RequestUri.Query)
            };

            request.RequestUri = uriBuilder.Uri;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            bool tokensHaveBeenRefreshed = false;

            if (DateTime.Now > (await tokenProvider.GetExpirationDateAsync()).AddMinutes(-1))
            {
                // pre-empts the possibility of an "Unauthorized" response if we know the tokens
                // have already expired, or are close to expiring

                // Subtracts a minute from the expriation date to somewhat correct for networking latency

                await tokenProvider.RefreshTokensAsync();
                tokensHaveBeenRefreshed = true;
            }

            await AuthenticateRequestAsync(request);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized || tokensHaveBeenRefreshed)
            {
                // if the request was not unauthorized, return the response
                // or if the tokens have already been refreshed once, return the response
                return response;
            }

            // otherwise, new tokens may be needed, and our time tracking may be off, so refresh them

            await tokenProvider.RefreshTokensAsync(); // refreshes the token provider's tokens

            await AuthenticateRequestAsync(request); // reauths request with new access token

            return await base.SendAsync(request, cancellationToken); // sends the request
        }
    }

    public sealed class CoinbaseClient
    {
        private readonly ICoinbaseTokenProvider tokenProvider;

        public CoinbaseClient(ICoinbaseTokenProvider provider)
        {
            tokenProvider = provider;
        }

        internal HttpClient BuildClient()
        {
            return new HttpClient(new CoinbaseMessageHandler(tokenProvider))
            {
                BaseAddress = new Uri("https://coinbase.com/api/v1/")
            };
        }

        #region Helpers

        internal Task<T> GetResponseFromClientAsync<T>(HttpClient client, string endpoint, params JsonConverter[] converters)
        {
            return GetResponseFromClientAsync<T>(client, endpoint, null, converters);
        }

        internal async Task<T> GetResponseFromClientAsync<T>(HttpClient client, string endpoint, HttpValueCollection parameters = null, params JsonConverter[] converters)
        {
            var requestUri = endpoint;

            if (parameters != null)
            {
                requestUri += "?" + parameters.ToString();
            }

            var response = await client.GetAsync(requestUri).ConfigureAwait(false);

            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(responseContent, converters);
        }

        internal Task<T> GetResponseAsync<T>(string endpoint, params JsonConverter[] converters)
        {
            return GetResponseAsync<T>(endpoint, null, converters);
        }

        internal async Task<T> GetResponseAsync<T>(string endpoint, HttpValueCollection parameters = null, params JsonConverter[] converters)
        {
            using (var client = BuildClient())
            {
                return await GetResponseFromClientAsync<T>(client, endpoint, parameters, converters).ConfigureAwait(false);
            }
        }

        private CoinbaseClientPage<T> BeginPaging<T>(string endpoint, int? limit = null, HttpValueCollection parameters = null, params JsonConverter[] converters)
            where T : PaginatedResponse
        {
            return CoinbaseClientPage<T>.Begin(this, endpoint, limit, parameters, converters);
        }

        private Task<CoinbaseClientPage<T>> GetPageAsync<T>(string endpoint, int page = 1, int? limit = null, HttpValueCollection parameters = null, params JsonConverter[] converters) 
            where T : PaginatedResponse
        {
            var beginningPage = BeginPaging<T>(endpoint, limit, parameters, converters);
            return beginningPage.GetPageAsync(page);
        }

        private Task<IReadOnlyList<T>> GetAllPaginatedResponses<T>(string endpoint, int? limit = null, HttpValueCollection parameters = null, params JsonConverter[] converters) 
            where T : PaginatedResponse
        {
            var beginPage = CoinbaseClientPage<T>.Begin(this, endpoint, limit, parameters, converters);
            return beginPage.GetRemainingResponsesAsync();
        }

        #endregion

        #region User

        public async Task<UserResponse> GetUserAsync()
        {
            var usersResponse = await GetResponseAsync<UsersResponse>("users").ConfigureAwait(false);

            Contract.Assert(usersResponse.Users.Count == 1, "There should be exactly one user in users API request to coinbase.");

            return usersResponse.Users[0];
        }

        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetBalanceAsync()
        {
            return GetResponseAsync<FixedPrecisionUnit<Bitcoin.BTC>>("account/balance", new BTCConverter());
        }

        #endregion

        #region Accounts
        private HttpValueCollection GetAccountParameters(string accountId)
        {
            var parameters = new HttpValueCollection();
            parameters.SetAccountId(accountId);
            return parameters;
        }

        public Task<CoinbaseClientPage<AccountsResponse>> GetAccountsPageAsync(int page = 1)
        {
            return GetPageAsync<AccountsResponse>("accounts", page);
        }

        public async Task<IReadOnlyList<AccountResponse>> GetAllAccountsAsync(int? limit = null)
        {
            var responses = await GetAllPaginatedResponses<AccountsResponse>("accounts", limit).ConfigureAwait(false);
            return responses.SelectMany(response => response.Accounts).ToList();
        }

        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetAccountBalanceAsync(string id)
        {
            return GetResponseAsync<FixedPrecisionUnit<Bitcoin.BTC>>(String.Format("accounts/{0}/balance", id), new BTCConverter());
        }

        #endregion

        #region Transactions

        public Task<CoinbaseClientPage<TransactionsResponse>> GetTransactionsPageAsync(string accountId = null, int page = 1, int? limit = null)
        {
            return GetPageAsync<TransactionsResponse>("transactions", page, limit, GetAccountParameters(accountId));
        }

        public async Task<ITransactionsResponse> GetAllTransactionsAsync(string accountId = null, int? limit = null)
        {
            // https://coinbase.com/api/doc/1.0/transactions/index.html

            var responses = await GetAllPaginatedResponses<TransactionsResponse>("transactions", limit, GetAccountParameters(accountId)).ConfigureAwait(false);

            var first = responses.First();

            return new TransactionsResponse()
            {
                Balance = first.Balance,
                CurrentUser = first.CurrentUser,
                NativeBalance = first.NativeBalance,
                Transactions = responses.SelectMany(response => response.Transactions).ToList()
            };
        }

        public Task<TransactionResponse> GetTransactionAsync(string transactionId, string accountId = null)
        {
            // https://coinbase.com/api/doc/1.0/transactions/show.html

            return GetResponseAsync<TransactionResponse>(String.Format("transactions/{0}", transactionId), GetAccountParameters(accountId));
        }

        #endregion

        #region Transfers

        public Task<CoinbaseClientPage<TransfersResponse>> GetTransfersPageAsync(string accountId = null, int page = 1, int? limit = null)
        {
            return GetPageAsync<TransfersResponse>("transfers", page, limit, GetAccountParameters(accountId));
        }

        public async Task<IReadOnlyList<TransferResponse>> GetAllTransfersAsync(string accountId = null, int? limit = null)
        {
            // https://coinbase.com/api/doc/1.0/transactions/index.html

            var responses = await GetAllPaginatedResponses<TransfersResponse>("transfers", limit, GetAccountParameters(accountId)).ConfigureAwait(false);

            return responses.SelectMany(res => res.Transfers).ToList();
        }

        #endregion

        #region Addresses

        private HttpValueCollection GetAddressParameters(string accountId, string query)
        {
            var parameters = new HttpValueCollection();
            parameters.SetAccountId(accountId);
            parameters.SetQuery(query);
            return parameters;
        }

        public Task<CoinbaseClientPage<AddressesResponse>> GetAddressesPageAsync(string accountId = null, string query = null, int page = 1, int? limit = null)
        {
            return GetPageAsync<AddressesResponse>("addresses", page, limit, GetAddressParameters(accountId, query));
        }

        public async Task<IReadOnlyList<AddressResponse>> GetAllAddressesAsync(string accountId = null, string query = null, int? limit = null)
        {
            var responses = await GetAllPaginatedResponses<AddressesResponse>("addresses", limit, GetAddressParameters(accountId, query)).ConfigureAwait(false);

            return responses.SelectMany(res => res.Addresses).ToList();
        }

        #endregion

        #region Contacts
        private HttpValueCollection GetContactParameters(string query)
        {
            var parameters = new HttpValueCollection();
            parameters.SetQuery(query);
            return parameters;
        }

        public Task<CoinbaseClientPage<ContactsResponse>> GetContactsPageAsync(string query = null, int page = 1, int? limit = null)
        {
            return GetPageAsync<ContactsResponse>("contacts", page, limit, GetContactParameters(query));
        }

        public async Task<IReadOnlyList<ContactResponse>> GetAllContactsAsync(string query = null, int? limit = null)
        {
            var responses = await GetAllPaginatedResponses<ContactsResponse>("contacts", limit, GetContactParameters(query)).ConfigureAwait(false);

            return responses.SelectMany(res => res.Contacts).ToList();
        }
        #endregion

        #region Payment Methods
        public Task<PaymentMethodsResponse> GetPaymentMethodsAsync()
        {
            return GetResponseAsync<PaymentMethodsResponse>("payment_methods");
        }
        #endregion
    }
}

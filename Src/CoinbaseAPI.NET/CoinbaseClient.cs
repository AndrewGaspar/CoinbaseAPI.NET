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

        public async Task<UserResponseEntity> GetUserAsync()
        {
            var usersResponse = await GetResponseAsync<UsersResponse>("users").ConfigureAwait(false);

            Contract.Assert(usersResponse.Users.Count == 1, "There should be exactly one user in users API request to coinbase.");

            return usersResponse.Users[0].User;
        }

        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetBalanceAsync()
        {
            return GetResponseAsync<FixedPrecisionUnit<Bitcoin.BTC>>("account/balance", new BTCConverter());
        }

        #region Accounts

        public Task<CoinbaseClientPage<AccountsResponse>> GetAccountsPageAsync(int page)
        {
            return GetPageAsync<AccountsResponse>("accounts", page);
        }

        public async Task<IList<AccountResponse>> GetAccountsAsync()
        {
            var responses = await GetAllPaginatedResponses<AccountsResponse>("accounts").ConfigureAwait(false);
            return responses.SelectMany(response => response.Accounts).ToList();
        }

        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetAccountBalanceAsync(string id)
        {
            return GetResponseAsync<FixedPrecisionUnit<Bitcoin.BTC>>(String.Format("accounts/{0}/balance", id), new BTCConverter());
        }

        #endregion

        #region Transactions

        public Task<CoinbaseClientPage<TransactionsResponse>> GetTransactionsPageAsync(string accountId = null, int page = 1)
        {
            var parameters = new HttpValueCollection();;
            if (accountId != null)
            {
                parameters.Add("account_id", accountId);
            }

            return GetPageAsync<TransactionsResponse>("transactions", 1, null, parameters);
        }

        public async Task<ITransactionsResponse> GetAllTransactionsAsync(string accountId = null)
        {
            // https://coinbase.com/api/doc/1.0/transactions/index.html

            var collection = new HttpValueCollection();
            if (accountId != null)
            {
                collection.Add("account_id", accountId);
            }

            var responses = await GetAllPaginatedResponses<TransactionsResponse>("transactions", null, collection).ConfigureAwait(false);

            var first = responses[0];

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

            var parameters = new HttpValueCollection();
            if (accountId != null)
            {
                parameters.Add("account_id", accountId);
            }

            return GetResponseAsync<TransactionResponse>(String.Format("transactions/{0}", transactionId), parameters);
        }

        #endregion
    }
}

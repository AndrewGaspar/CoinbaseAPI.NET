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

    public sealed class CoinbaseClient : IDisposable
    {
        private readonly HttpClient client;

        public CoinbaseClient(ICoinbaseTokenProvider provider)
        {
            client = new HttpClient(new CoinbaseMessageHandler(provider))
            {
                BaseAddress = new Uri("https://coinbase.com/api/v1/")
            };
        }

        #region Helpers

        private Task<T> GetResponse<T>(string endpoint, params JsonConverter[] converters)
        {
            return GetResponse<T>(endpoint, null, converters);
        }

        private async Task<T> GetResponse<T>(string endpoint, HttpValueCollection parameters = null, params JsonConverter[] converters)
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

        private Task<T> GetPaginatedResponse<T>(string endpoint, int page, HttpValueCollection parameters = null, params JsonConverter[] converters) 
            where T : PaginatedResponse
        {
            var newParameters = parameters != null ? new HttpValueCollection(parameters) : new HttpValueCollection();
            newParameters.AddOrUpdate("page", page.ToString());

            return GetResponse<T>(endpoint, newParameters, converters);
        }

        private async Task<IList<T>> GetAllPaginatedResponses<T>(string endpoint, HttpValueCollection parameters = null, params JsonConverter[] converters) 
            where T : PaginatedResponse
        {
            var firstPage = await GetPaginatedResponse<T>(endpoint, 1, parameters, converters).ConfigureAwait(false);

            var restPages = await Task.WhenAll(from page in Enumerable.Range(2, firstPage.NumPages - 1)
                                               select GetPaginatedResponse<T>(endpoint, page, parameters, converters)).ConfigureAwait(false);

            return firstPage.Yield().Concat(restPages).ToList();
        }

        #endregion

        public async Task<UserResponse> GetUserAsync()
        {
            var usersResponse = await GetResponse<UsersResponse>("users").ConfigureAwait(false);

            Contract.Assert(usersResponse.Users.Count == 1, "There should be exactly one user in users API request to coinbase.");

            return usersResponse.Users[0].User;
        }

        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetBalanceAsync()
        {
            return GetResponse<FixedPrecisionUnit<Bitcoin.BTC>>("account/balance", new BTCConverter());
        }

        #region Accounts

        public Task<AccountsResponse> GetAccountsPageAsync(int page)
        {
            return GetPaginatedResponse<AccountsResponse>("accounts", page);
        }

        public async Task<IList<AccountResponse>> GetAccountsAsync()
        {
            var responses = await GetAllPaginatedResponses<AccountsResponse>("accounts").ConfigureAwait(false);
            return responses.SelectMany(response => response.Accounts).ToList();
        }

        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetAccountBalanceAsync(string id)
        {
            return GetResponse<FixedPrecisionUnit<Bitcoin.BTC>>(String.Format("accounts/{0}/balance", id), new BTCConverter());
        }

        #endregion

        #region Transactions

        public async Task<ITransactionsResponse> GetTransactionsAsync(string accountId = null)
        {
            // https://coinbase.com/api/doc/1.0/transactions/index.html

            var collection = new HttpValueCollection();
            if (accountId != null)
            {
                collection.Add("account_id", accountId);
            }

            var responses = await GetAllPaginatedResponses<TransactionsResponse>("transactions", collection).ConfigureAwait(false);

            var first = responses[0];

            return new TransactionsResponse()
            {
                Balance = first.Balance,
                CurrentUser = first.CurrentUser,
                NativeBalance = first.NativeBalance,
                Transactions = responses.SelectMany(response => response.Transactions).ToList()
            };
        }

        public async Task<TransactionResponseItem> GetTransactionAsync(string transactionId, string accountId = null)
        {
            // https://coinbase.com/api/doc/1.0/transactions/show.html

            var parameters = new HttpValueCollection();
            if (accountId != null)
            {
                parameters.Add("account_id", accountId);
            }

            var response = await GetResponse<TransactionResponse>(String.Format("transactions/{0}", transactionId), parameters).ConfigureAwait(false);

            return response.Transaction;
        }

        #endregion

        public void Dispose()
        {
            client.Dispose();
        }

        ~CoinbaseClient()
        {
            Dispose();
        }
    }
}

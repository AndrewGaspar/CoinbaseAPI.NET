using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;
    using Primitives;
    using Utilities;

    /// <summary>
    /// This static class contains extensions for setting Http URL Parameters.
    /// </summary>
    internal static class HttpValueCollectionExtensionsForCoinbase
    {
        /// <summary>
        /// Sets the access_token parameter to a string value if the access token is non-null.
        /// </summary>
        /// <param name="coll">The collection the parameter is set on.</param>
        /// <param name="accountId">The access token value.</param>
        public static void SetAccessToken(this HttpValueCollection coll, string accessToken)
        {
            if (accessToken != null)
            {
                coll.AddOrUpdate("access_token", accessToken);
            }
        }

        /// <summary>
        /// Sets the account_id parameter to a string value if the account id is non-null.
        /// </summary>
        /// <param name="coll">The collection the parameter is set on.</param>
        /// <param name="accountId">The account id value.</param>
        public static void SetAccountId(this HttpValueCollection coll, string accountId)
        {
            if (accountId != null)
            {
                coll.AddOrUpdate("account_id", accountId);
            }
        }

        /// <summary>
        /// Sets the query parameter to a string value if the query is non-null.
        /// </summary>
        /// <param name="coll">The collection the parameter is set on.</param>
        /// <param name="accountId">The query value.</param>
        public static void SetQuery(this HttpValueCollection coll, string query)
        {
            if (query != null)
            {
                coll.AddOrUpdate("query", query);
            }
        }

        /// <summary>
        /// Sets the page parameters to an integer value if the page is non-null.
        /// </summary>
        /// <param name="coll">The collection the parameter is set on.</param>
        /// <param name="accountId">The page value.</param>
        public static void SetPage(this HttpValueCollection coll, int? page)
        {
            if (page.HasValue)
            {
                coll.AddOrUpdate("page", page.Value.ToString());
            }
        }

        /// <summary>
        /// Sets the limit parameters to an integer value if the page is non-null.
        /// </summary>
        /// <param name="coll">The collection the parameter is set on.</param>
        /// <param name="accountId">The limit value.</param>
        public static void SetLimit(this HttpValueCollection coll, int? limit)
        {
            if (limit.HasValue)
            {
                coll.AddOrUpdate("limit", limit.Value.ToString());
            }
        }
    }

    /// <summary>
    /// This is a message handler applied to the CoinbaseClient's HttpClient. 
    /// It ensures that the OAuth tokens are refreshed if a request is returns a 401/Unauthorized code.
    /// </summary>
    class CoinbaseMessageHandler : DelegatingHandler
    {
        private readonly ICoinbaseTokenProvider tokenProvider;

        /// <summary>
        /// Constructs a message handler with a CoinbaseTokenProvider.
        /// </summary>
        /// <param name="provider">A token provider.</param>
        internal CoinbaseMessageHandler(ICoinbaseTokenProvider provider)
            : base(new HttpClientHandler())
        {
            tokenProvider = provider;
        }

        /// <summary>
        /// Creates a new Uri with an access token query parameter.
        /// </summary>
        /// <param name="original">The original Uri before setting the access token.</param>
        /// <returns>A task for a new Uri with the access token set.</returns>
        private Task<Uri> AppendAccessTokenAsync(Uri original)
        {
            return tokenProvider.GetAccessTokenAsync().ContinueWith(tokenTask =>
            {
                var accessToken = tokenTask.Result;

                var collection = HttpUtility.ParseQueryString(original.Query);
                collection.SetAccessToken(accessToken);

                return new UriBuilder(original)
                {
                    Query = collection.ToString()
                }.Uri;
            });
        }

        /// <summary>
        /// Adds the token provider's access token to the request.
        /// </summary>
        /// <param name="request">The request message to be sent.</param>
        /// <returns>A task that completes once the request Uri has been updated.</returns>
        private Task AddAccessTokenToRequestAsync(HttpRequestMessage request)
        {
            return AppendAccessTokenAsync(request.RequestUri).ContinueWith(uriTask => { request.RequestUri = uriTask.Result; });
        }

        /// <summary>
        /// Overrides the base implementation of SendAsync to resend a request with new access tokens when necessary.
        /// </summary>
        /// <param name="request">The message to be sent.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A response message.</returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            bool tokensHaveBeenRefreshed = false;

            if (DateTime.Now > (await tokenProvider.GetExpirationDateAsync()).AddMinutes(-1))
            {
                // pre-empts the possibility of an "Unauthorized" response if we know the tokens
                // have already expired, or are close to expiring

                // Subtracts a minute from the expiration date to somewhat correct for latency between 
                // Coinbase issuing new tokens to finally setting date

                await tokenProvider.RefreshTokensAsync();
                tokensHaveBeenRefreshed = true;
            }

            await AddAccessTokenToRequestAsync(request);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized || tokensHaveBeenRefreshed)
            {
                // if the request was not unauthorized, return the response
                // or if the tokens have already been refreshed once, there's no point trying again, so return the response
                return response;
            }

            // otherwise, new tokens may be needed, and our time tracking may be off, so refresh them and try once more

            await tokenProvider.RefreshTokensAsync(); // refreshes the token provider's tokens

            await AddAccessTokenToRequestAsync(request); // reauths request with new access token

            return await base.SendAsync(request, cancellationToken); // sends the request
        }
    }

    /// <summary>
    /// An exception thrown if the specified resource could not be found. Thrown on 404s.
    /// </summary>
    public class CoinbaseResourceNotFoundException : Exception
    {
        /// <summary>
        /// The Endpoint that no resource could be found at.
        /// </summary>
        public string Endpoint { get; private set; }

        /// <summary>
        /// Accepts the endpoint that had no resource.
        /// </summary>
        /// <param name="endpoint">The endpoint that caused the problem.</param>
        internal CoinbaseResourceNotFoundException(string endpoint)
            : base("Could not find resource at this endpoint: " + endpoint)
        {
            Endpoint = endpoint;
        }
    }

    /// <summary>
    /// CoinbaseClient communicates with the Coinbase REST API via HttpClient. Each operation in the RESTful API is strongly typed so that consumers of
    /// this class can be assured they're providing properly formatted resources for the API.
    /// 
    /// This class implements IDisposable so the user can release resources as soon as they are done with them, if they so choose.
    /// GC will eventually clean up resources, however.
    /// </summary>
    public sealed class CoinbaseClient : DisposableObject
    {
        private HttpClient client;
        private HttpClient WebClient
        {
            get
            {
                if (Disposed)
                {
                    throw new ObjectDisposedException(this.GetType().FullName);
                }

                return client;
            }
            set
            {
                client = value;
            }
        }

        /// <summary>
        /// Instantiates a new CoinbaseClient using a tokenProvider to provide the user's access and refresh tokens.
        /// </summary>
        /// <param name="provider">A token provider that implements the ICoinbaseTokenProvider interface.</param>
        public CoinbaseClient(ICoinbaseTokenProvider provider)
        {
            WebClient = new HttpClient(new CoinbaseMessageHandler(provider))
            {
                BaseAddress = new Uri("https://coinbase.com/api/v1/")
            };

            WebClient.DefaultRequestHeaders.Accept.Clear();
            WebClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        #region Helpers
        /// <summary>
        /// A static helper to append the parameters of a request to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">An HTTP endpoint.</param>
        /// <param name="parameters">A collection of parameters to apply.</param>
        /// <returns>The parameters, if available, appended to the endpoint.</returns>
        private static string ConstructEndpoint(string endpoint, HttpValueCollection parameters)
        {
            var requestUri = endpoint;

            if (parameters != null)
            {
                requestUri += "?" + parameters.ToString();
            }

            return requestUri;
        }

        /// <summary>
        /// A static helper to read the provided message, check for any errors, and deserialize the result if available.
        /// 
        /// Hopefully someday this can be implemented using streaming data into Json.Net.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the response as.</typeparam>
        /// <param name="endpoint">The endpoint the message comes from - used only for error handling.</param>
        /// <param name="message">The message being read.</param>
        /// <param name="converters">Optional. Any necessary converters for the deserialization.</param>
        /// <returns>An asynchronous task for T.</returns>
        private async static Task<T> DeserializeResponseAsync<T>(string endpoint, HttpResponseMessage message, JsonConverter[] converters)
        {
            if (message.StatusCode == HttpStatusCode.NotFound)
            {
                throw new CoinbaseResourceNotFoundException(endpoint);
            }

            message.EnsureSuccessStatusCode();

            var responseContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);

            return JsonConvert.DeserializeObject<T>(responseContent, converters);
        }

        /// <summary>
        /// Checks a provided object to see if it satisfies the requirements set on it.
        /// 
        /// If it does and no exception is thrown, the object is serialized as JSON.
        /// </summary>
        /// <typeparam name="T">The type of object to serialize.</typeparam>
        /// <param name="request">The object being serialized.</param>
        /// <returns>A JSON representation of the given request.</returns>
        private static string SerializeRequest<T>(T request)
        {
            if (request == null)
            {
                return "";
            }

            RequirementsVerifier.EnsureSatisfactionOfRequirements(request);

            return JsonConvert.SerializeObject(request);
        }

        /// <summary>
        /// A private helper that sends a request to an endpoint with a specific verb, but with no specific resource attached,
        /// and then deserializes the response and returns it.
        /// </summary>
        /// <typeparam name="T">The type of the response to deserialize.</typeparam>
        /// <param name="method">The HttpMethod (GET, POST, etc.) to use for the request.</param>
        /// <param name="endpoint">The endpoint of the request.</param>
        /// <param name="parameters">The parameters to attach to the endpoint.</param>
        /// <param name="converters">The converters to apply at deserialization.</param>
        /// <returns>A task for the deserialized response.</returns>
        private async Task<T> SendAsync<T>(HttpMethod method, string endpoint, HttpValueCollection parameters, JsonConverter[] converters)
        {
            var requestUri = ConstructEndpoint(endpoint, parameters);

            var response = await WebClient.SendAsync(new HttpRequestMessage(method, requestUri)).ConfigureAwait(false);

            return await DeserializeResponseAsync<T>(endpoint, response, converters).ConfigureAwait(false);
        }

        /// <summary>
        /// A private helper that sends a request to an endpoint with a specific verb with a resource attached
        /// and then deserializes the response and returns it.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response to deserialize.</typeparam>
        /// <param name="method">The HttpMethod (GET, POST, etc.) to use for the request.</param>
        /// <param name="endpoint">The endpoint of the request.</param>
        /// <param name="resource">The resource to serialize.</param>
        /// <param name="parameters">The parameters to attach to the endpoint.</param>
        /// <param name="converters">The converters to apply at deserialization.</param>
        /// <returns></returns>
        private async Task<TResponse> SendRequestAsync<TRequest, TResponse>(HttpMethod method, string endpoint, TRequest resource, HttpValueCollection parameters, JsonConverter[] converters)
        {
            var requestUri = ConstructEndpoint(endpoint, parameters);

            var resourceText = SerializeRequest(resource);

            var content = new StringContent(resourceText);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await WebClient.SendAsync(new HttpRequestMessage(method, requestUri)
            {
                Content = content
            });

            return await DeserializeResponseAsync<TResponse>(endpoint, response, converters).ConfigureAwait(false);
        }

        #region Get
        /// <summary>
        /// Gets a resource at a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns>A task for the resource.</returns>
        internal Task<T> GetAsync<T>(string endpoint, params JsonConverter[] converters)
        {
            return GetAsync<T>(endpoint, null, converters);
        }

        /// <summary>
        /// Gets a resource at a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="parameters">The parameters to apply to the endpoint.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns>A task for the resource.</returns>
        internal Task<T> GetAsync<T>(string endpoint, HttpValueCollection parameters = null, params JsonConverter[] converters)
        {
            return SendAsync<T>(HttpMethod.Get, endpoint, parameters, converters);
        }
        #endregion

        #region Post
        /// <summary>
        /// Posts a resource to a specific endpoint and receives a resource back.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request being sent.</typeparam>
        /// <typeparam name="TResponse">The type of the response received.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="resource">The resource to send.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns></returns>
        internal Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest resource, params JsonConverter[] converters)
        {
            return PostAsync<TRequest, TResponse>(endpoint, resource, null, converters);
        }

        /// <summary>
        /// Posts a resource to a specific endpoint and receives a resource back.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request being sent.</typeparam>
        /// <typeparam name="TResponse">The type of the response received.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="resource">The resource to send.</param>
        /// <param name="parameters">The parameters to apply to the endpoint.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns></returns>
        internal Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest resource, HttpValueCollection parameters = null, params JsonConverter[] converters)
        {
            return SendRequestAsync<TRequest, TResponse>(HttpMethod.Post, endpoint, resource, parameters, converters);
        }

        /// <summary>
        /// Posts to a specific endpoint to make a change to that resource.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns></returns>
        internal Task<TResponse> PostAsync<TResponse>(string endpoint, params JsonConverter[] converters)
        {
            return PostAsync<TResponse>(endpoint, null, converters);
        }

        /// <summary>
        /// Posts to a specific endpoint to make a change to that resource.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="parameters">The parameters to apply to the endpoint.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns></returns>
        internal Task<TResponse> PostAsync<TResponse>(string endpoint, HttpValueCollection parameters = null, params JsonConverter[] converters)
        {
            return SendAsync<TResponse>(HttpMethod.Post, endpoint, parameters, converters);
        }
        #endregion

        #region Put
        /// <summary>
        /// Puts a resource at a specific endpoint and receives the updated resource in return.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request being sent.</typeparam>
        /// <typeparam name="TResponse">The type of the response received.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="resource">The resource to send.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns></returns>
        internal Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest resource, params JsonConverter[] converters)
        {
            return PutAsync<TRequest, TResponse>(endpoint, resource, null, converters);
        }

        /// <summary>
        /// Puts a resource at a specific endpoint and receives the updated resource in return.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request being sent.</typeparam>
        /// <typeparam name="TResponse">The type of the response received.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="resource">The resource to send.</param>
        /// <param name="parameters">The parameters to apply to the endpoint.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns></returns>
        internal Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest resource, HttpValueCollection parameters = null, params JsonConverter[] converters)
        {
            return SendRequestAsync<TRequest, TResponse>(HttpMethod.Put, endpoint, resource, parameters, converters);
        }
        #endregion

        #region Delete
        /// <summary>
        /// Gets a resource at a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns>A task for the resource.</returns>
        internal Task<TResponse> DeleteAsync<TResponse>(string endpoint, params JsonConverter[] converters)
        {
            return DeleteAsync<TResponse>(endpoint, null, converters);
        }

        /// <summary>
        /// Deletes a resource at a specific endpoint.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="endpoint">The location of the resource.</param>
        /// <param name="parameters">The parameters to apply to the endpoint.</param>
        /// <param name="converters">Converters to deserialize resource with.</param>
        /// <returns>A task for the resource.</returns>
        internal Task<TResponse> DeleteAsync<TResponse>(string endpoint, HttpValueCollection parameters = null, params JsonConverter[] converters)
        {
            return SendAsync<TResponse>(HttpMethod.Delete, endpoint, parameters, converters);
        }
        #endregion

        #region Pages
        /// <summary>
        /// This helper helps return an AsyncPageList with this CoinbaseClient passed in.
        /// </summary>
        /// <typeparam name="T">The type of each page of the list.</typeparam>
        /// <param name="endpoint">The location of the paged resource.</param>
        /// <param name="recordsPerPage">The number of results to be displayed on each page.</param>
        /// <param name="parameters">The parameters to apply to the endpoint.</param>
        /// <param name="converters">The converters used to convert each page from JSON.</param>
        /// <returns>An IAsyncReadOnlyList of the pages.</returns>
        private AsyncCoinbasePageList<T> GetAsyncPagesList<T>(string endpoint, int? recordsPerPage = null, HttpValueCollection parameters = null, params JsonConverter[] converters)
            where T : RecordsPage
        {
            return new AsyncCoinbasePageList<T>(this, endpoint, recordsPerPage, parameters, converters);
        }
        #endregion

        #endregion

        #region User
        /// <summary>
        /// Returns the user's profile asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/users/index.html
        /// </summary>
        /// <returns>A task for the user's identifying information.</returns>
        public async Task<UserResponse> GetUserAsync()
        {
            var usersResponse = await GetAsync<UsersResponse>("users").ConfigureAwait(false);

            Contract.Assert(usersResponse.Users.Count == 1, "There should be exactly one user in users API request to coinbase.");

            return usersResponse.Users[0];
        }

        /// <summary>
        /// Returns the balance of the user's primary account asynchronously.
        /// 
        /// This method is deprecated - it is preferred you use GetAccountBalanceAsync and specify the exact id of the account.
        /// 
        /// https://coinbase.com/api/doc/1.0/account/balance.html
        /// </summary>
        /// <returns>A task for the user's balance.</returns>
        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetBalanceAsync()
        {
            return GetAsync<FixedPrecisionUnit<Bitcoin.BTC>>("account/balance", new BTCConverter());
        }

        /// <summary>
        /// This method creates a new user. If the client id for this application is provided, then the user will 
        /// automatically be authenticated for that application.
        /// 
        /// https://coinbase.com/api/doc/1.0/users/create.html
        /// </summary>
        /// <param name="newUser">The details of the new user.</param>
        /// <returns>A task for a response indicating the results of adding the user.</returns>
        public Task<CreateUserResponse> CreateUserAsync(CreateUserRequest newUser)
        {
            return PostAsync<CreateUserRequest, CreateUserResponse>("users", newUser);
        }

        /// <summary>
        /// Updates the properties of the current user. This method will call GetUserAsync first.
        /// 
        /// If the user's id is already known, call the overload which accepts a user id to get the result faster.
        /// 
        /// https://coinbase.com/api/doc/1.0/users/update.html
        /// </summary>
        /// <param name="userChanges">The changes to the current user.</param>
        /// <returns>A task for the response to the requested changes.</returns>
        public async Task<UpdateUserResponse> UpdateUserAsync(UpdateUserRequest userChanges)
        {
            var user = await GetUserAsync();
            return await UpdateUserAsync(user.User.Id, userChanges);
        }

        /// <summary>
        /// Updates the properties of the current user. The id provided must match the current user's.
        /// 
        /// https://coinbase.com/api/doc/1.0/users/update.html
        /// </summary>
        /// <param name="userId">The id of the user you want to update.</param>
        /// <param name="userChanges">The changes to that user.</param>
        /// <returns>A task for the response to the requested changes.</returns>
        public Task<UpdateUserResponse> UpdateUserAsync(string userId, UpdateUserRequest userChanges)
        {
            return PutAsync<UpdateUserRequest, UpdateUserResponse>(String.Format("users/{0}", userId), userChanges);
        }
        #endregion

        #region Accounts

        class AccountParameters : HttpValueCollection
        {
            public string AccountId
            {
                get
                {
                    if (this.ContainsKey("account_id"))
                    {
                        return this["account_id"];
                    }

                    return null;
                }
                set
                {
                    if (value != null)
                    {
                        this.AddOrUpdate("account_id", value);
                    }
                }
            }
            public AccountParameters(string accountId)
            {
                AccountId = accountId;
            }
        }

        /// <summary>
        /// Gets the account pages as an enumerable list.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/index.html
        /// </summary>
        /// <param name="recordsPerPage">The number of records on each page. Default 25. Maximum 1000.</param>
        /// <returns>An enumerable list of the pages.</returns>
        public AsyncCoinbasePageList<AccountsPage> GetAccountPagesAsyncList(int? recordsPerPage = null)
        {
            return GetAsyncPagesList<AccountsPage>("accounts", recordsPerPage);
        }

        /// <summary>
        /// Gets all account pages asynchronously. Defaults to maximum results per page.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/index.html
        /// </summary>
        /// <param name="recordsPerPage">The number of records on each page. Maximum 1000.</param>
        /// <returns>A task for the list of account pages.</returns>
        public Task<IList<AccountsPage>> GetAccountPagesAsync(int? recordsPerPage = 1000)
        {
            return GetAccountPagesAsyncList(recordsPerPage).ToListAsync();
        }

        /// <summary>
        /// Gets the accounts as an enumerable list. This list is a wrapper of the pages list.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/index.html
        /// </summary>
        /// <param name="recordsPerPage">The number of records on each page request. Maximum 1000.</param>
        /// <returns>An enumerable list.</returns>
        public IAsyncReadOnlyList<AccountResponse> GetAccountsAsyncList(int? recordsPerPage = null)
        {
            return new AsyncCoinbaseRecordsList<AccountResponse, AccountsPage>(
                GetAccountPagesAsyncList(recordsPerPage), page => page.Accounts);
        }

        /// <summary>
        /// Gets all accounts asynchronously. Defaults to maximum results per page.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/index.html
        /// </summary>
        /// <param name="recordsPerPage">The number of records on each page. Maximum 1000.</param>
        /// <returns>A task for the list of accounts.</returns>
        public Task<IList<AccountResponse>> GetAccountsAsync(int? recordsPerPage = 1000)
        {
            return GetAccountsAsyncList(recordsPerPage).ToListAsync();
        }

        /// <summary>
        /// Gets the balance for an account asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/balance.html
        /// </summary>
        /// <param name="accountId">The id of the account to check the balance for.</param>
        /// <returns>A task for the balance of the account.</returns>
        public Task<FixedPrecisionUnit<Bitcoin.BTC>> GetAccountBalanceAsync(string accountId)
        {
            return GetAsync<FixedPrecisionUnit<Bitcoin.BTC>>(String.Format("accounts/{0}/balance", accountId), new BTCConverter());
        }

        /// <summary>
        /// Creates a new account with a default name asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/create.html
        /// </summary>
        /// <returns>A task for a new account.</returns>
        public Task<CreateAccountResponse> CreateAccountAsync()
        {
            return PostAsync<CreateAccountResponse>("accounts");
        }

        /// <summary>
        /// Creates a new account with a specified name asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/create.html
        /// </summary>
        /// <param name="request">The information the account should be created with.</param>
        /// <returns>A task for the response to the creation request.</returns>
        public Task<CreateAccountResponse> CreateAccountAsync(CreateAccountRequest request)
        {
            return PostAsync<CreateAccountRequest, CreateAccountResponse>("accounts", request);
        }

        /// <summary>
        /// Makes the account with the specified id the primary account.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/primary.html
        /// </summary>
        /// <param name="accountId">The id of the account that is being made the primary account.</param>
        /// <returns>A task for the response to this request.</returns>
        public Task<RequestResponse> SetPrimaryAccountAsync(string accountId)
        {
            return PostAsync<RequestResponse>(String.Format("accounts/{0}/primary", accountId));
        }

        /// <summary>
        /// Updates the specified account with new information asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/update.html
        /// </summary>
        /// <param name="id">The id of the account being changed.</param>
        /// <param name="request">The changes to be made.</param>
        /// <returns>A task for an updated account.</returns>
        public Task<UpdateAccountResponse> UpdateAccountAsync(string id, UpdateAccountRequest request)
        {
            return PutAsync<UpdateAccountRequest, UpdateAccountResponse>(String.Format("accounts/{0}", id), request);
        }

        /// <summary>
        /// Deletes the account at the specified id. This will fail if the account has a balance.
        /// 
        /// https://coinbase.com/api/doc/1.0/accounts/destroy.html
        /// </summary>
        /// <param name="id">The id for the account to delete.</param>
        /// <returns>A response indicating the success of the deletion.</returns>
        public Task<RequestResponse> DestroyAccountAsync(string id)
        {
            return DeleteAsync<RequestResponse>(String.Format("accounts/{0}", id));
        }
        #endregion

        #region Transactions
        /// <summary>
        /// Gets all the transactions associated with an account as an enumerable list of pages.
        /// 
        /// https://coinbase.com/api/doc/1.0/transactions/index.html
        /// </summary>
        /// <param name="accountId">The id of the account to retrieve the transactions for.</param>
        /// <returns>An enumerable of transaction pages.</returns>
        public AsyncCoinbasePageList<TransactionsPage> GetTransactionPagesAsyncList(string accountId = null)
        {
            return GetAsyncPagesList<TransactionsPage>("transactions", null, new AccountParameters(accountId));
        }

        /// <summary>
        /// Gets all the transactions associated with an account as a list of pages asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/transactions/index.html
        /// </summary>
        /// <param name="accountId">The id of the account to retrieve the transactions for.</param>
        /// <returns>A task for a list of transaction pages.</returns>
        public Task<IList<TransactionsPage>> GetTransactionPagesAsync(string accountId = null)
        {
            return GetTransactionPagesAsyncList(accountId).ToListAsync();
        }

        /// <summary>
        /// Gets all the transactions associated with an account as an enumerable list.
        /// 
        /// https://coinbase.com/api/doc/1.0/transactions/index.html
        /// </summary>
        /// <param name="accountId">The id of the account to retrieve transactions for.</param>
        /// <returns>An enumerable of transactions.</returns>
        public IAsyncReadOnlyList<TransactionResponse> GetTransactionsAsyncList(string accountId = null)
        {
            // https://coinbase.com/api/doc/1.0/transactions/index.html

            return new AsyncCoinbaseRecordsList<TransactionResponse, TransactionsPage>(GetTransactionPagesAsyncList(accountId),
                page => page.Transactions);
        }

        /// <summary>
        /// Gets all the transactions associated with an account as a list asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/transactions/index.html
        /// </summary>
        /// <param name="accountId">The id of the account to retrieve the transactions for.</param>
        /// <returns>A task for a list of transactions.</returns>
        public Task<IList<TransactionResponse>> GetTransactionsAsync(string accountId = null)
        {
            return GetTransactionsAsyncList(accountId).ToListAsync();
        }

        /// <summary>
        /// Gets a specific transaction asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/transactions/show.html
        /// </summary>
        /// <param name="transactionId">The transaction to retrieve.</param>
        /// <param name="accountId">The account the transaction is located in.</param>
        /// <returns>A task for the transaction.</returns>
        public Task<TransactionResponse> GetTransactionAsync(string transactionId, string accountId = null)
        {
            return GetAsync<TransactionResponse>(String.Format("transactions/{0}", transactionId), new AccountParameters(accountId));
        }
        #endregion

        #region Transfers
        /// <summary>
        /// Gets all transfer pages as an enumerable list.
        /// 
        /// https://coinbase.com/api/doc/1.0/transfers/index.html
        /// </summary>
        /// <param name="accountId">The id for the account to get transfers for.</param>
        /// <param name="recordsPerPage">The number of results on each page. Default 25. Maximum 1000.</param>
        /// <returns>An enumerable of transfer pages.</returns>
        public AsyncCoinbasePageList<TransfersPage> GetTransferPagesAsyncList(string accountId = null, int? recordsPerPage = 25)
        {
            return GetAsyncPagesList<TransfersPage>("transfers", recordsPerPage, new AccountParameters(accountId));
        }

        /// <summary>
        /// Gets a list of all transfer pages asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/transfers/index.html
        /// </summary>
        /// <param name="accountId">The id for the account to get transfers for.</param>
        /// <param name="recordsPerPage">The number of results on each page. Defaults to maximum of 1000.</param>
        /// <returns>A task for a list of transfer pages.</returns>
        public Task<IList<TransfersPage>> GetTransferPagesAsync(string accountId = null, int? recordsPerPage = 1000)
        {
            return GetTransferPagesAsyncList(accountId, recordsPerPage).ToListAsync();
        }

        /// <summary>
        /// Gets all transfers as an enumerable list.
        /// 
        /// https://coinbase.com/api/doc/1.0/transfers/index.html
        /// </summary>
        /// <param name="accountId">The id for the account to get transfers for.</param>
        /// <param name="recordsPerPage">The number of results on each page. Default 25. Maximum 1000.</param>
        /// <returns>An enumerable of transfers.</returns>
        public IAsyncReadOnlyList<TransferResponse> GetTransfersAsyncList(string accountId = null, int? recordsPerPage = 25)
        {
            // https://coinbase.com/api/doc/1.0/transfers/index.html

            return new AsyncCoinbaseRecordsList<TransferResponse, TransfersPage>(GetTransferPagesAsyncList(accountId, recordsPerPage), 
                page => page.Transfers);
        }

        /// <summary>
        /// Gets all transfers as a list asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/transfers/index.html
        /// </summary>
        /// <param name="accountId">The id for the account to get transfers for.</param>
        /// <param name="recordsPerPage">The number of results on each page. Defaults to the maximum of 1000.</param>
        /// <returns>A task for a list of transfers.</returns>
        public Task<IList<TransferResponse>> GetTransfersAsync(string accountId = null, int? recordsPerPage = 1000)
        {
            return GetTransfersAsyncList(accountId, recordsPerPage).ToListAsync();
        }
        #endregion

        #region Addresses

        class AddressParameters : AccountParameters
        {
            public string Query
            {
                get
                {
                    if (ContainsKey("query"))
                    {
                        return this["query"];
                    }

                    return null;
                }
                set
                {
                    if (value != null)
                    {
                        AddOrUpdate("query", value);
                    }
                }
            }

            public AddressParameters(string accountId, string query)
                : base(accountId)
            {
                Query = query;
            }
        }

        /// <summary>
        /// Gets addresses associated with an account as an enumerable list of pages.
        /// 
        /// https://coinbase.com/api/doc/1.0/addresses/index.html
        /// </summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="query">A query for the addresses - matches the address and labels on the address.</param>
        /// <param name="recordsPerPage">The number of addresses on each page. Default 25. Maximum 1000.</param>
        /// <returns>An asynchronously enumerable list of pages of addresses.</returns>
        public AsyncCoinbasePageList<AddressesPage> GetAddressPagesAsyncList(string accountId = null, string query = null, int? recordsPerPage = 25)
        {
            return GetAsyncPagesList<AddressesPage>("addresses", recordsPerPage, new AddressParameters(accountId, query));
        }

        /// <summary>
        /// Gets addresses associated with an account as a list of pages asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/addresses/index.html
        /// </summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="query">A query for the addresses - matches the address and labels on the address.</param>
        /// <param name="recordsPerPage">The number of addresses on each page. Default is maximum of 1000.</param>
        /// <returns>A task for a list of address pages.</returns>
        public Task<IList<AddressesPage>> GetAddressPagesAsync(string accountId = null, string query = null, int? recordsPerPage = 1000)
        {
            return GetAddressPagesAsyncList(accountId, query, recordsPerPage).ToListAsync();
        }

        /// <summary>
        /// Get addresses associated with an account as an enumerable list of addresses.
        /// 
        /// https://coinbase.com/api/doc/1.0/addresses/index.html
        /// </summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="query">A query for the addresses - matches the address and labels on the address.</param>
        /// <param name="recordsPerPage">The number of addresses on each page. Default 25. Maximum 1000.</param>
        /// <returns>An asynchronously enumerable list of addresses.</returns>
        public IAsyncReadOnlyList<AddressResponse> GetAddressesAsyncList(string accountId = null, string query = null, int? recordsPerPage = 25)
        {
            return new AsyncCoinbaseRecordsList<AddressResponse, AddressesPage>(GetAddressPagesAsyncList(accountId, query, recordsPerPage),
                page => page.Addresses);
        }

        /// <summary>
        /// Gets all addresses associated the account as a list asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/addresses/index.html
        /// </summary>
        /// <param name="accountId">The account id.</param>
        /// <param name="query">A query for the addresses - matches the address and labels on the address.</param>
        /// <param name="recordsPerPage">The number of addresses on each page. Default is maximum of 1000.</param>
        /// <returns>A task for a list of addresses.</returns>
        public Task<IList<AddressResponse>> GetAddressesAsync(string accountId = null, string query = null, int? recordsPerPage = 1000)
        {
            return GetAddressesAsyncList(accountId, query, recordsPerPage).ToListAsync();
        }
        #endregion

        #region Oauth Applications
        /// <summary>
        /// Gets information about all of the OAuth applications that the current user owns as an enumerable list of pages.
        /// 
        /// https://coinbase.com/api/doc/1.0/applications/index.html
        /// </summary>
        /// <returns>An asynchronously enumerable list of application pages.</returns>
        public AsyncCoinbasePageList<ApplicationsPage> GetApplicationPagesAsyncList()
        {
            return GetAsyncPagesList<ApplicationsPage>("oauth/applications");
        }

        /// <summary>
        /// Gets information about all of the OAuth applications that the current user owns as a list of pages asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/applications/index.html
        /// </summary>
        /// <returns>A task for a list of application pages.</returns>
        public Task<IList<ApplicationsPage>> GetApplicationPagesAsync()
        {
            return GetApplicationPagesAsyncList().ToListAsync();
        }

        /// <summary>
        /// Gets information about all of the OAuth applications that the current user owns as an enumerable list of applciations.
        /// 
        /// https://coinbase.com/api/doc/1.0/applications/index.html
        /// </summary>
        /// <returns>An asynchronously enumerable list of applications.</returns>
        public IAsyncReadOnlyList<ApplicationResponse> GetApplicationsAsyncList()
        {
            return new AsyncCoinbaseRecordsList<ApplicationResponse, ApplicationsPage>(GetApplicationPagesAsyncList(), page => page.Applications);
        }

        /// <summary>
        /// Gets information about all of the OAuth applications that the current user owns as a list asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/applications/index.html
        /// </summary>
        /// <returns>A task for a list of applications.</returns>
        public Task<IList<ApplicationResponse>> GetApplicationsAsync()
        {
            return GetApplicationsAsyncList().ToListAsync();
        }

        /// <summary>
        /// Returns a specific application by id.
        /// 
        /// There is a wrapper around the response that is not present in the GetApplications requests due to inconsistencies in the Coinbase API.
        /// 
        /// https://coinbase.com/api/doc/1.0/applications/show.html
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Task<ApplicationResponseWrapper> GetApplicationAsync(string id)
        {
            return GetAsync<ApplicationResponseWrapper>(String.Format("oauth/applications/{0}", id));
        }

        /// <summary>
        /// Creates a new application asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/applications/create.html
        /// </summary>
        /// <param name="request">The details of the new application.</param>
        /// <returns>A task for the created application.</returns>
        public Task<CreateApplicationResponse> CreateApplication(CreateApplicationRequest request)
        {
            return PostAsync<CreateApplicationRequest, CreateApplicationResponse>("oauth/applications", request);
        }
        #endregion

        #region Contacts
        class ContactParameters : HttpValueCollection
        {
            public string Query
            {
                get
                {
                    if (ContainsKey("query"))
                    {
                        return this["query"];
                    }

                    return null;
                }
                set
                {
                    if (value != null)
                    {
                        AddOrUpdate("query", value);
                    }
                }
            }

            public ContactParameters(string query)
            {
                Query = query;
            }
        }

        /// <summary>
        /// Gets the contacts for the current user as an asychronously enumerable list of contact pages.
        /// 
        /// https://coinbase.com/api/doc/1.0/contacts/index.html
        /// </summary>
        /// <param name="query">A query for the contacts.</param>
        /// <param name="contactsPerPage">The number of contacts on each page. Default 25. Max 1000.</param>
        /// <returns>An asynchronously enumerable list of contact pages.</returns>
        public AsyncCoinbasePageList<ContactsPage> GetContactPagesAsyncList(string query = null, int? contactsPerPage = 25)
        {
            return GetAsyncPagesList<ContactsPage>("contacts", contactsPerPage, new ContactParameters(query));
        }

        /// <summary>
        /// Gets the contacts for the current user as a list of contact pages asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/contacts/index.html
        /// </summary>
        /// <param name="query">A query for the contacts.</param>
        /// <param name="contactsPerPage">The number of contacts on each page. Default 25. Max 1000.</param>
        /// <returns>A task for a list of contact pages.</returns>
        public Task<IList<ContactsPage>> GetContactPagesAsync(string query = null, int? contactsPerPage = 1000)
        {
            return GetContactPagesAsyncList(query, contactsPerPage).ToListAsync();
        }

        /// <summary>
        /// Gets the contacts for the current user as an asychronously enumerable list of contacts.
        /// 
        /// https://coinbase.com/api/doc/1.0/contacts/index.html
        /// </summary>
        /// <param name="query">A query for the contacts.</param>
        /// <param name="contactsPerPage">The number of contacts on each page. Default 25. Max 1000.</param>
        /// <returns>An asynchronously enumerable list of contacts.</returns>
        public IAsyncReadOnlyList<ContactResponse> GetContactsAsyncList(string query = null, int? contactsPerPage = 25)
        {
            return new AsyncCoinbaseRecordsList<ContactResponse, ContactsPage>(GetContactPagesAsyncList(query, contactsPerPage), page => page.Contacts);
        }

        /// <summary>
        /// Gets the contacts for the current user as a list of contacts asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/contacts/index.html
        /// </summary>
        /// <param name="query">A query for the contacts.</param>
        /// <param name="contactsPerPage">The number of contacts on each page. Default 25. Max 1000.</param>
        /// <returns>A task for a list of contacts.</returns>
        public Task<IList<ContactResponse>> GetContactsAsync(string query = null, int? contactsPerPage = 1000)
        {
            return GetContactsAsyncList(query, contactsPerPage).ToListAsync();
        }
        #endregion

        #region Payment Methods
        /// <summary>
        /// Gets the payment methods associated with the current user asynchronously.
        /// 
        /// https://coinbase.com/api/doc/1.0/payment_methods/index.html
        /// </summary>
        /// <returns>A task for the payment methods.</returns>
        public Task<PaymentMethodsResponse> GetPaymentMethodsAsync()
        {
            return GetAsync<PaymentMethodsResponse>("payment_methods");
        }
        #endregion

        protected override void DisposeManagedResources()
        {
            WebClient.Dispose();
        }
    }
}

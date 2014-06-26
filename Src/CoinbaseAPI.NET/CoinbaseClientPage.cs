using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;
    using Newtonsoft.Json;
    using System.Net.Http;
    using Utilities;

    public class CoinbaseOutOfPageRangeException : Exception
    {
        public int RequestedPage { get; private set; }
        public int NumPages { get; private set; }

        internal CoinbaseOutOfPageRangeException(int requestedPage, int totalPages)
            : base(String.Format("Requested page {0}, but there are only {1} pages. Re-request the initial page to check for new pages.", requestedPage, totalPages))
        {
            RequestedPage = requestedPage;
            NumPages = totalPages;
        }
    }

    public class CoinbaseClientPage<TPaginated> : DisposableObject where TPaginated : PaginatedResponse
    {
        internal static CoinbaseClientPage<TPaginated> Begin(CoinbaseClient client, string endpoint, int? limit, 
            HttpValueCollection parameters, JsonConverter[] converters)
        {
            return new CoinbaseClientPage<TPaginated>(client, endpoint, limit, parameters, null, converters)
            {
                IsBeginPage = true
            };
        }

        public static CoinbaseClientPage<TPaginated> EndPage { get; private set; }

        static CoinbaseClientPage()
        {
            EndPage = new CoinbaseClientPage<TPaginated>(null, null, null, null, null, null)
            {
                IsEndPage = true
            };
        }

        private CoinbaseClient client;
        private CoinbaseClient Client
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
        private HttpValueCollection parameters;
        private string endpoint;
        private int? limit;
        private JsonConverter[] converters;

        internal CoinbaseClientPage(CoinbaseClientPage<TPaginated> page, TPaginated newPage)
            : this(page.Client, page.endpoint, page.limit, page.parameters, newPage, page.converters)
        {

        }

        internal CoinbaseClientPage(CoinbaseClient client, string endpoint, int? limit, HttpValueCollection parameters, TPaginated pageResponse, JsonConverter[] converters)
        {
            this.Client = client;
            this.parameters = parameters;
            this.endpoint = endpoint;
            this.limit = limit;
            this.converters = converters;
            this.Response = pageResponse;

            // static GetBeginPage() overrides this
            this.IsBeginPage = false;

            // static GetEndPage() overrides this
            this.IsEndPage = false;
        }

        public int? CurrentPage
        {
            get
            {
                if (Response == null)
                {
                    return null;
                }

                return Response.CurrentPage;
            }
        }
        public int? NumPages
        {
            get
            {
                if (Response == null)
                {
                    return null;
                }

                return Response.NumPages;
            }
        }
        public int? TotalCount
        {
            get
            {
                if (Response == null)
                {
                    return null;
                }

                return Response.TotalCount;
            }
        }
        public bool IsEndPage { get; private set; }
        internal bool IsBeginPage { get; private set; }

        public TPaginated Response { get; private set; }


        public async Task<CoinbaseClientPage<TPaginated>> GetPageAsync(int page)
        {
            var response = await GetResponseAsync(page).ConfigureAwait(false);

            if (response.CurrentPage > response.NumPages)
            {
                return CoinbaseClientPage<TPaginated>.EndPage;
            }

            return new CoinbaseClientPage<TPaginated>(this, response);
        }

        private async Task<TPaginated> GetResponseAsync(int page)
        {
            if (NumPages.HasValue && page > NumPages)
            {
                throw new CoinbaseOutOfPageRangeException(page, NumPages.Value);
            }
            else
            {
                var newParameters = parameters != null ? new HttpValueCollection(parameters) : new HttpValueCollection();
                newParameters.AddOrUpdate("page", page.ToString());

                if (limit.HasValue)
                {
                    newParameters.AddOrUpdate("limit", limit.Value.ToString());
                }

                return await Client.GetAsync<TPaginated>(endpoint, newParameters, converters);
            }
        }

        private bool CannotContinue()
        {
            return IsEndPage || (!IsBeginPage && CurrentPage.Value >= NumPages.Value);
        }

        public Task<CoinbaseClientPage<TPaginated>> GetNextPageAsync()
        {
            if (CannotContinue())
            {
                return Task.FromResult(EndPage);
            }

            var page = IsBeginPage ? 1 : (CurrentPage.Value + 1);
            return GetPageAsync(page);
        }



        public async Task<IReadOnlyList<TPaginated>> GetRemainingResponsesAsync()
        {
            if (CannotContinue())
            {
                return new List<TPaginated>();
            }

            CoinbaseClientPage<TPaginated> firstPage = null;
            int startPage;
            int endPage;
            // if this page is the "beginning page", then all pages are the remaining pages
            // We must request the first page because otherwise we don't know how many pages there are
            // Then recursively call GetRemainingPagesAsync to get all of the pages.
            if (IsBeginPage)
            {
                firstPage = await GetPageAsync(1).ConfigureAwait(false);
                if (firstPage.IsEndPage)
                {
                    return new List<TPaginated>();
                }

                startPage = firstPage.CurrentPage.Value + 1;
                endPage = firstPage.NumPages.Value;
                    
            }
            else
            {
                startPage = CurrentPage.Value + 1;
                endPage = NumPages.Value;
            }

            var remainingPages = await Task.WhenAll(from pageNumber in Enumerable.Range(startPage, endPage - 1)
                                        select GetResponseAsync(pageNumber)).ConfigureAwait(false);

            if (firstPage != null)
            {
                return firstPage.Response.Yield().Concat(remainingPages).ToList();
            }
            else
            {
                return remainingPages.ToList();
            }
        }

        public async Task<IReadOnlyList<CoinbaseClientPage<TPaginated>>> GetRemainingPagesAsync()
        {
            var pages = from response in await GetRemainingResponsesAsync().ConfigureAwait(false)
                        select new CoinbaseClientPage<TPaginated>(this, response);

            return pages.ToList();
        }

        protected override void DisposeManagedResources()
        {
            ((IDisposable)Client).Dispose();
        }
    }
}

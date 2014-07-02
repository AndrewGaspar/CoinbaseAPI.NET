using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;
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

    public class MidIterationAccessException : Exception
    {
        public MidIterationAccessException()
            : base("Attempted to access element of AsyncEnumerator mid iteration")
        {

        }
    }

    public class AsyncCoinbasePageList<TPaginated> : IAsyncReadOnlyList<TPaginated>
        where TPaginated : RecordsPage
    {
        protected List<TPaginated> PageCache { get; private set; }

        protected string Endpoint { get; private set; }

        public int? ResultsPerPage { get; private set; }

        protected HttpValueCollection Parameters { get; private set; }

        protected CoinbaseClient CoinbaseClient { get; private set; }

        protected JsonConverter[] Converters { get; private set; }

        internal AsyncCoinbasePageList(CoinbaseClient client, string endpoint, int? resultsPerPage, HttpValueCollection parameters, JsonConverter[] converters)
        {
            CoinbaseClient = client;
            Endpoint = endpoint;
            ResultsPerPage = resultsPerPage;
            Parameters = parameters;
            Converters = converters;

            PageCache = new List<TPaginated>();
        }

        #region Private Methods

        private Task<int> countCache;
        private async Task<int> FindCountAsync()
        {
            var firstPage = PageCache.FirstOrDefault(p => p != null);

            var page = firstPage ?? await GetPageUncheckedAsync(0).ConfigureAwait(false);

            return page.NumPages;
        }

        private async Task<TPaginated> GetPageUncheckedAsync(int index)
        {
            lock (PageCache)
            {
                if (index < PageCache.Count && PageCache[index] != null)
                {
                    return PageCache[index];
                }
            }

            var newParameters = Parameters != null ? new HttpValueCollection(Parameters) : new HttpValueCollection();
            newParameters.SetPage(index + 1);
            newParameters.SetLimit(ResultsPerPage);

            var page = await CoinbaseClient.GetAsync<TPaginated>(Endpoint, newParameters, Converters).ConfigureAwait(false);

            if (page.CurrentPage <= page.NumPages)
            {
                Contract.Assert(index == (page.CurrentPage - 1));

                lock (PageCache)
                {
                    while (index >= PageCache.Count)
                    {
                        PageCache.Add(null);
                    }

                    PageCache[index] = page;
                }
            }

            return page;
        }
        #endregion

        #region Interface Methods
        public Task<int> GetCountAsync()
        {
            if (countCache == null)
            {
                countCache = FindCountAsync();
            }

            return countCache;
        }

        public async Task<TPaginated> GetItemAsync(int index)
        {
            var count = await GetCountAsync();

            if (index >= count)
            {
                throw new ArgumentOutOfRangeException("index", index, String.Format("The index {0} exceeded the count {1}.", index, count));
            }
            else
            {
                return await GetPageUncheckedAsync(index);
            }
        }

        public IAsyncEnumerator<TPaginated> GetEnumerator()
        {
            return new AsyncListEnumerator<TPaginated>(this);
        }
        #endregion

        #region Public Methods
        public async Task<int> GetTotalAsync()
        {
            if (PageCache.Any())
            {
                return PageCache[0].TotalCount;
            }
            else
            {
                return (await GetPageUncheckedAsync(0)).TotalCount;
            }
        }

        public async Task<IList<TPaginated>> ToListAsync()
        {
            var count = await GetCountAsync();

            return (await Task.WhenAll(from i in Enumerable.Range(0, count) select GetPageUncheckedAsync(i))).ToList();
        }
        #endregion
    }
}

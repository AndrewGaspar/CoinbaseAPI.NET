using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    using Models;
    using Utilities;

    public class AsyncCoinbaseRecordsList<TResponse, TPage> : IAsyncReadOnlyList<TResponse>
        where TPage : RecordsPage
    {
        protected AsyncCoinbasePageList<TPage> PageList { get; private set; }

        public delegate IList<TResponse> GetResultsFromPage(TPage page);

        private GetResultsFromPage resultsGetter;

        public AsyncCoinbaseRecordsList(AsyncCoinbasePageList<TPage> pageList, GetResultsFromPage function)
        {
            PageList = pageList;
            resultsGetter = function;
        }

        #region Private Methods
        private async Task<int> GetItemsPerPageAsync()
        {
            if (await GetCountAsync() > 0)
            {
                var firstPage = await PageList.GetItemAsync(0);

                var results = resultsGetter(firstPage);

                return results.Count;
            }

            return 0;
        }
        #endregion

        public Task<int> GetCountAsync()
        {
            return PageList.GetTotalAsync();
        }

        public async Task<TResponse> GetItemAsync(int index)
        {
            var count = await GetCountAsync();
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException("index", index,
                    String.Format("The index {0} was out of the range of the list of length {1}.", index, count));
            }

            var itemsPerPage = await GetItemsPerPageAsync();

            var page = await PageList.GetItemAsync(index / itemsPerPage);

            var results = resultsGetter(page);

            return results[index % itemsPerPage];
        }

        public IAsyncEnumerator<TResponse> GetEnumerator()
        {
            return new AsyncListEnumerator<TResponse>(this);
        }

        public async Task<IList<TResponse>> ToListAsync()
        {
            var pagesList = await PageList.ToListAsync();

            return pagesList.SelectMany(page => resultsGetter(page)).ToList();
        }
    }
}

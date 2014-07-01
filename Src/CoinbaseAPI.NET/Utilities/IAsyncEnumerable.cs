using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase.Utilities
{
    public static class AsyncEnumerableExtensions
    {
        public async static Task ForEachAsync<T>(this IAsyncEnumerable<T> enumerable, Func<T, Task> action)
        {
            var enumerator = enumerable.GetEnumerator();

            var hasValue = await enumerator.MoveNextAsync();

            while (hasValue)
            {
                await action(enumerator.Current);
                hasValue = await enumerator.MoveNextAsync();
            }
        }

        public static Task ForEachAsync<T>(this IAsyncEnumerable<T> enumerable, Action<T> action)
        {
            return enumerable.ForEachAsync(new Func<T, Task>(async item => action(item)));
        }

        public async static Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> enumerable)
        {
            var list = new List<T>();

            await enumerable.ForEachAsync(item => list.Add(item));

            return list;
        }
    }

    public interface IAsyncEnumerable<T>
    {
        IAsyncEnumerator<T> GetEnumerator();
    }
}

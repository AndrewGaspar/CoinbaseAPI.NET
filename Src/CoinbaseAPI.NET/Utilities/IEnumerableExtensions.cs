using System.Collections.Generic;

namespace Bitlet.Coinbase.Utilities
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            /// <summary>
            /// Wraps this object instance into an IEnumerable&lt;T&gt;
            /// consisting of a single item.
            /// 
            /// http://stackoverflow.com/questions/1577822/passing-a-single-item-as-ienumerablet
            /// </summary>
            /// <typeparam name="T"> Type of the wrapped object.</typeparam>
            /// <param name="item"> The object to wrap.</param>
            /// <returns>
            /// An IEnumerable&lt;T&gt; consisting of a single item.
            /// </returns>
            yield return item;
        }
    }
}

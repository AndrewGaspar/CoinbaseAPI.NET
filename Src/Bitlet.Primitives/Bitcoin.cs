using System.Diagnostics.Contracts;
namespace Bitlet.Primitives
{
    public static class Bitcoin
    {
        public interface IBitcoin : IUnit { }
        public class BTC : IBitcoin { public decimal Base { get { return 1e8M; } } }
        public class mBTC : IBitcoin { public decimal Base { get { return 1e5M; } } }

        public class μBTC : IBitcoin { public decimal Base { get { return 1e2M; } } }

        public class Satoshis : IBitcoin { public decimal Base { get { return 1M; } } }
        public static FixedPrecisionUnit<R> Convert<T, R>(FixedPrecisionUnit<T> input)
            where T : IBitcoin, new()
            where R : IBitcoin, new()
        {
            Contract.Requires(input != null);

            var mult = (new T()).Base;
            var div = (new R()).Base;

            var scale = mult / div;

            return new FixedPrecisionUnit<R>(input.Value * scale);
        }
    }
}

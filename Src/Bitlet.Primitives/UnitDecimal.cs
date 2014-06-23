using System;
using System.Diagnostics.Contracts;

namespace Bitlet.Primitives
{
    public class FixedPrecisionUnit<Unit> where Unit : IUnit, new()
    {
        public readonly decimal Value;
        public readonly Type Type = typeof(Unit);

        public FixedPrecisionUnit(decimal value)
        {
            var baseValue = (new Unit()).Base;

            Value = Decimal.Truncate(value * baseValue) / baseValue;
        }

        public static FixedPrecisionUnit<Unit> operator +(FixedPrecisionUnit<Unit> first, FixedPrecisionUnit<Unit> second)
        {
            Contract.Requires(first != null && second != null);

            return new FixedPrecisionUnit<Unit>(first.Value + second.Value);
        }

        public static FixedPrecisionUnit<Unit> operator -(FixedPrecisionUnit<Unit> first, FixedPrecisionUnit<Unit> second)
        {
            Contract.Requires(first != null && second != null);

            return new FixedPrecisionUnit<Unit>(first.Value - second.Value);
        }

        public static FixedPrecisionUnit<Unit> operator *(FixedPrecisionUnit<Unit> unit, decimal scalar)
        {
            Contract.Requires(unit != null);

            return new FixedPrecisionUnit<Unit>(unit.Value * scalar);
        }

        public static FixedPrecisionUnit<Unit> operator /(FixedPrecisionUnit<Unit> unit, decimal scalar)
        {
            Contract.Requires(unit != null);
            
            return new FixedPrecisionUnit<Unit>(unit.Value / scalar);
        }
    }
}

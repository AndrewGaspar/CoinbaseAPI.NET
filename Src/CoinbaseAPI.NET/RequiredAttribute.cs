using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    [AttributeUsage(AttributeTargets.Property, Inherited=true, AllowMultiple=false)]
    internal class RequiredAttribute : Attribute
    {
        public bool IsRequired { get; set; }

        public RequiredAttribute()
        {
            IsRequired = true;
        }

        public RequiredAttribute(bool isRequired)
        {
            IsRequired = isRequired;
        }
    }

    internal class OptionalAttribute : RequiredAttribute
    {
        public OptionalAttribute()
            : base(false)
        {

        }
    }
}

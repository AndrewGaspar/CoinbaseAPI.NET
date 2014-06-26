using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Bitlet.Coinbase
{
    public class RequiredUnmarkedException : Exception
    {
        public Type Type { get; set; }
        public IList<PropertyInfo> Properties { get; set; }

        public RequiredUnmarkedException(Type t, IList<PropertyInfo> properties)
            : base(String.Format("Type {0} has unmarked requirements on properties: {1}", t.FullName, String.Join(",", properties.Select(p => p.Name))))
        {
            Type = t;
            Properties = properties;
        }
    }

    public class NullRequiredPropertyException : Exception
    {
        public object Instance { get; set; }
        public Type Type { get; set; }
        public IList<PropertyInfo> Properties { get; set; }

        public NullRequiredPropertyException(object instance, Type t, IList<PropertyInfo> nullProperties)
            : base(String.Format("An instance of type {0} has null values for required properties: {1}",
                t.FullName, String.Join(",", nullProperties.Select(p => p.Name))))
        {
            Instance = instance;
            Type = t;
            Properties = nullProperties;
        }
    }

    internal class RequirementInfo
    {
        public IList<PropertyInfo> RequiredProperties { get; set; }

        public IList<PropertyInfo> ChildEntityProperties { get; set; }
    }

    internal static class RequirementsVerifier
    {
        private static Dictionary<Type, RequirementInfo> requirementsDictionary = new Dictionary<Type, RequirementInfo>();

        private static bool IsUserEntityProperty(PropertyInfo p)
        {
            var type = p.PropertyType;
            var typeInfo = type.GetTypeInfo();

            return !(typeInfo.IsPrimitive || type == typeof(DateTime) || type == typeof(decimal));
        }

        private static RequirementInfo GetRequirementInfo(Type type)
        {
            RequirementInfo info;
            if (!requirementsDictionary.TryGetValue(type, out info))
            {
                var properties = type.GetRuntimeProperties();

                var noRequiredAttributeProperties = properties.Where(property => property.GetCustomAttribute<RequiredAttribute>() == null).ToList();

                if (noRequiredAttributeProperties.Count > 0)
                {
                    throw new RequiredUnmarkedException(type, noRequiredAttributeProperties);
                }

                var requiredProperties = (from property in properties
                                          where property.GetCustomAttribute<RequiredAttribute>().IsRequired
                                          select property).ToList();

                var childEntities = (from property in properties
                                     where IsUserEntityProperty(property)
                                     select property).ToList();

                info = new RequirementInfo()
                {
                    RequiredProperties = requiredProperties,
                    ChildEntityProperties = childEntities
                };

                requirementsDictionary[type] = info;
            }

            return info;
        }

        public static void EnsureSatisfactionOfRequirements(object resource)
        {
            var type = resource.GetType();

            var info = GetRequirementInfo(type);

            var nullProperties = (from requiredProperty in info.RequiredProperties
                                  where requiredProperty.GetValue(resource) == null
                                  select requiredProperty).ToList();

            if (nullProperties.Count > 0)
            {
                throw new NullRequiredPropertyException(resource, type, nullProperties);
            }

            foreach (var child in info.ChildEntityProperties)
            {
                EnsureSatisfactionOfRequirements(child.GetValue(resource));
            }
        }
    }
}

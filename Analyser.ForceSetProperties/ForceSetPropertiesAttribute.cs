using System;

namespace Analyser.ForceSetProperties
{
    public class ForceSetPropertiesAttribute : Attribute
    {
        public ForceSetPropertiesAttribute()
        {
            
        }

        public Type[] Types { get; set; }
    }

    public class ForceSetPropertiesAttribute<T> : ForceSetPropertiesAttribute
    {
        public ForceSetPropertiesAttribute()
        {
            Types = new[] { typeof(T) };
        }
    }
}

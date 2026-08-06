using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Analyser.ForceSetProperties.Models
{
    public class RequiredProperty
    {
        public RequiredProperty(string name, IPropertySymbol symbol)
        {
            Name = name;
            Symbol = symbol;
            SetLocations = new List<SetLocation>();
        }

        public string Name { get; }

        public IPropertySymbol Symbol { get; }

        public List<SetLocation> SetLocations { get; }

        public bool IsSet => SetLocations.Count > 0;
    }
}

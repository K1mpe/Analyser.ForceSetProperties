using System.Collections.Generic;
using Analyser.ForceSetProperties.Services;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildMissingPropertiesMessageTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void ListsEveryMissingPropertyOnItsOwnBulletLine()
        {
            var result = _sut.BuildMissingPropertiesMessage(new List<string> { "CreatedAt", "UpdatedAt" });

            Assert.Equal("The following properties must be initialized:\n - CreatedAt\n - UpdatedAt", result);
        }
    }
}

using Analyser.ForceSetProperties.Models;
using Analyser.ForceSetProperties.Services;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class BuildLocationTextTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void DirectLocation_HasNoMethodAnnotation()
        {
            var location = new SetLocation(@"C:\Project\Factory.cs", 45);

            var result = _sut.BuildLocationText(location, @"C:\Project");

            Assert.Equal("Factory.cs line 45", result);
        }

        [Fact]
        public void TracedLocation_IncludesTheMethodName()
        {
            var location = new SetLocation(@"C:\Project\Factory.cs", 45, "Map");

            var result = _sut.BuildLocationText(location, @"C:\Project");

            Assert.Equal("Factory.cs line 45 (via Map)", result);
        }

        [Fact]
        public void FileInASubdirectory_RendersARelativeSubPath()
        {
            var location = new SetLocation(@"C:\Project\TestModels\DtoModel.cs", 10);

            var result = _sut.BuildLocationText(location, @"C:\Project");

            Assert.Equal(@"TestModels\DtoModel.cs line 10", result);
        }
    }
}

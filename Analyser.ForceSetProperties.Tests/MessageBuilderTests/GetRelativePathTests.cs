using Analyser.ForceSetProperties.Services;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.MessageBuilderTests
{
    public class GetRelativePathTests
    {
        private readonly MessageBuilder _sut = new();

        [Fact]
        public void FileInTheBaseDirectory_ReturnsJustTheFileName()
        {
            var result = _sut.GetRelativePath(@"C:\Project", @"C:\Project\Factory.cs");

            Assert.Equal("Factory.cs", result);
        }

        [Fact]
        public void FileInANestedSubdirectory_ReturnsTheSubPath()
        {
            var result = _sut.GetRelativePath(@"C:\Project", @"C:\Project\TestModels\DtoModel.cs");

            Assert.Equal(@"TestModels\DtoModel.cs", result);
        }

        [Fact]
        public void FileOutsideTheBaseDirectory_WalksUpWithParentSegments()
        {
            var result = _sut.GetRelativePath(@"C:\Project\Sub", @"C:\Project\Other.cs");

            Assert.Equal(@"..\Other.cs", result);
        }

        [Fact]
        public void MissingFullPath_ReturnsNullInsteadOfThrowing()
        {
            var result = _sut.GetRelativePath(@"C:\Project", "");

            Assert.Null(result);
        }

        [Fact]
        public void FullPathWithoutADirectory_ReturnsNullInsteadOfThrowing()
        {
            // Not a valid absolute URI, e.g. a SyntaxTree parsed without a real file path.
            var result = _sut.GetRelativePath(@"C:\Project", "Test.cs");

            Assert.Null(result);
        }

        [Fact]
        public void MissingBasePath_ReturnsNullInsteadOfThrowing()
        {
            var result = _sut.GetRelativePath("", @"C:\Project\Test.cs");

            Assert.Null(result);
        }
    }
}

using Analyser.ForceSetProperties.Services;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesDriverTests
{
    public class GetCommonDirectoryTests
    {
        private readonly ForceSetPropertiesDriver _sut = new();

        [Fact]
        public void AllFilesInTheSameDirectory_ReturnsThatDirectory()
        {
            var result = _sut.GetCommonDirectory(new[]
            {
                @"C:\Project\Factory.cs",
                @"C:\Project\DtoModel.cs",
            });

            Assert.Equal(@"C:\Project", result);
        }

        [Fact]
        public void FilesInDifferentSubdirectories_ReturnsTheSharedParent()
        {
            var result = _sut.GetCommonDirectory(new[]
            {
                @"C:\Project\TestModels\DtoModel.cs",
                @"C:\Project\obj\Debug\Example.AssemblyInfo.cs",
            });

            Assert.Equal(@"C:\Project", result);
        }

        [Fact]
        public void SingleFile_ReturnsItsOwnDirectory()
        {
            var result = _sut.GetCommonDirectory(new[]
            {
                @"C:\Project\TestModels\DtoModel.cs",
            });

            Assert.Equal(@"C:\Project\TestModels", result);
        }

        [Fact]
        public void NoFiles_ReturnsEmpty()
        {
            var result = _sut.GetCommonDirectory(System.Array.Empty<string>());

            Assert.Equal(string.Empty, result);
        }
    }
}

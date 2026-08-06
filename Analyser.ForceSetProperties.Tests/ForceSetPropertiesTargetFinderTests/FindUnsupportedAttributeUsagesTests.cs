using System.Linq;
using Analyser.ForceSetProperties.Services;
using Analyser.ForceSetProperties.Tests.TestHelpers;
using Xunit;

namespace Analyser.ForceSetProperties.Tests.ForceSetPropertiesTargetFinderTests
{
    public class FindUnsupportedAttributeUsagesTests
    {
        private readonly ForceSetPropertiesTargetFinder _sut = new();

        [Fact]
        public void AttributeOnAClass_IsReportedAsUnsupported()
        {
            const string source = @"
[ForceSetProperties]
public class DtoModel
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);

            var result = _sut.FindUnsupportedAttributeUsages(compilation).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void AttributeOnAField_IsReportedAsUnsupported()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public string Name;
}";
            var compilation = RoslynTestHelper.Compile(source);

            var result = _sut.FindUnsupportedAttributeUsages(compilation).ToList();

            Assert.Single(result);
        }

        [Fact]
        public void AttributeOnAConstructor_IsNotReported()
        {
            const string source = @"
public class DtoModel
{
    [ForceSetProperties]
    public DtoModel()
    {
    }
}";
            var compilation = RoslynTestHelper.Compile(source);

            var result = _sut.FindUnsupportedAttributeUsages(compilation);

            Assert.Empty(result);
        }

        [Fact]
        public void AttributeOnAMethod_IsNotReported()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public object Create()
    {
        return new object();
    }
}";
            var compilation = RoslynTestHelper.Compile(source);

            var result = _sut.FindUnsupportedAttributeUsages(compilation);

            Assert.Empty(result);
        }

        [Fact]
        public void AttributeOnAProperty_IsNotReported()
        {
            const string source = @"
public class Factory
{
    [ForceSetProperties]
    public object Current => new object();
}";
            var compilation = RoslynTestHelper.Compile(source);

            var result = _sut.FindUnsupportedAttributeUsages(compilation);

            Assert.Empty(result);
        }

        [Fact]
        public void UnrelatedAttribute_IsIgnored()
        {
            const string source = @"
[System.Obsolete]
public class DtoModel
{
    public string Name { get; set; }
}";
            var compilation = RoslynTestHelper.Compile(source);

            var result = _sut.FindUnsupportedAttributeUsages(compilation);

            Assert.Empty(result);
        }
    }
}

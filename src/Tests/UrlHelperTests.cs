using System.Threading.Tasks;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class UrlHelperTests : VerifyBase
{
    [Fact]
    public Task Company()
    {
        return Verify(UrlHelper.GetCompany("https://github.com/org/repository"));
    }

    [Fact]
    public Task Project()
    {
        return Verify(UrlHelper.GetProject("https://github.com/org/repository"));
    }

    public UrlHelperTests(ITestOutputHelper output) : base(output)
    {
    }
}
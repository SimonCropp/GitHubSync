using VerifyXunit;
using Xunit;

[UsesVerify]
public class UrlHelperTests
{
    [Fact]
    public Task Company()
    {
        return Verifier.Verify(UrlHelper.GetCompany("https://github.com/org/repository"));
    }

    [Fact]
    public Task Project()
    {
        return Verifier.Verify(UrlHelper.GetProject("https://github.com/org/repository"));
    }
}
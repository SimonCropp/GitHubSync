[UsesVerify]
public class UrlHelperTests
{
    [Fact]
    public Task Company() =>
        Verify(UrlHelper.GetCompany("https://github.com/org/repository"));

    [Fact]
    public Task Project() =>
        Verify(UrlHelper.GetProject("https://github.com/org/repository"));
}
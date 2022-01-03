[UsesVerify]
public class UrlHelperTests
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
}
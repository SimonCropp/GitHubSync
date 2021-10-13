using VerifyXunit;
using Xunit;

[UsesVerify]
public class ConfigTests
{
    [Fact]
    public Task Parsing()
    {
        var context = ContextLoader.Load(@".\ConfigImport.yaml");
        return Verifier.Verify(context);
    }
}
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

public class ConfigTests :
    VerifyBase
{
    [Fact]
    public Task Parsing()
    {
        var context = ContextLoader.Load(@".\ConfigImport.yaml");
        return Verify(context);
    }

    public ConfigTests(ITestOutputHelper output) :
        base(output)
    {
    }
}

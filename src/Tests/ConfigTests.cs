public class ConfigTests
{
    [Fact]
    public Task Parsing()
    {
        var context = ContextLoader.Load(ProjectFiles.ConfigImport_yaml);
        return Verify(context);
    }
}
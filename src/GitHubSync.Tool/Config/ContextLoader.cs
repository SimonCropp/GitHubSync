using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

static class ContextLoader
{
    public static Context Load(string fileName)
    {
        var configurationContent = File.ReadAllText(fileName);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<Context>(configurationContent);
    }
}
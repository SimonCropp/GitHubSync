using Octokit;
using System;
using System.IO;
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

        var context = deserializer.Deserialize<Context>(configurationContent);

        var githubToken = Environment.GetEnvironmentVariable("Octokit_OAuthToken");
        if (!string.IsNullOrWhiteSpace(githubToken))
        {
            context.Credentials = new Credentials(githubToken);
        }

        return context;
    }
}
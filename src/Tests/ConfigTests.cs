using Xunit;

public class ConfigTests
{
    [Fact]
    public void ConfigExample01()
    {
        var context = Config.ContextLoader.Load(@".\ConfigTests.example_01.yaml");

        Assert.Equal(3, context.Templates.Count);

        var template1 = context.Templates[0];
        Assert.Equal("geertvanhorrik", template1.Name);
        Assert.Equal("https://github.com/geertvanhorrik/repositorytemplate", template1.Url);
        Assert.Equal("master", template1.Branch);

        var template2 = context.Templates[1];
        Assert.Equal("catel", template2.Name);
        Assert.Equal("https://github.com/Catel/RepositoryTemplate.Components", template2.Url);
        Assert.Equal("master", template2.Branch);

        var template3 = context.Templates[2];
        Assert.Equal("wildgums-components-public", template3.Name);
        Assert.Equal("https://github.com/wildgums/RepositoryTemplate.Components.Public", template3.Url);
        Assert.Equal("master", template3.Branch);

        Assert.Equal(3, context.Repositories.Count);

        var repository1 = context.Repositories[0];
        Assert.Equal("CsvHelper", repository1.Name);
        Assert.Equal("https://github.com/JoshClose/CsvHelper", repository1.Url);
        Assert.Equal("master", repository1.Branch);
        Assert.False(repository1.AutoMerge);
        Assert.Single(repository1.Templates);
        Assert.Equal("geertvanhorrik", repository1.Templates[0]);

        var repository2 = context.Repositories[1];
        Assert.Equal("Catel", repository2.Name);
        Assert.Equal("https://github.com/catel/catel", repository2.Url);
        Assert.Equal("develop", repository2.Branch);
        Assert.True(repository2.AutoMerge);
        Assert.Equal(2, repository2.Templates.Count);
        Assert.Equal("geertvanhorrik", repository2.Templates[0]);
        Assert.Equal("catel", repository2.Templates[1]);

        var repository3 = context.Repositories[2];
        Assert.Equal("Orc.Controls", repository3.Name);
        Assert.Equal("https://github.com/wildgums/orc.controls", repository3.Url);
        Assert.Equal("develop", repository3.Branch);
        Assert.True(repository3.AutoMerge);
        Assert.Equal(2, repository3.Templates.Count);
        Assert.Equal("geertvanhorrik", repository3.Templates[0]);
        Assert.Equal("wildgums-components-public", repository3.Templates[1]);
    }
}

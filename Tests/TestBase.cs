using GitHubSync;
using Xunit.Abstractions;

public abstract class TestBase
{
    public ITestOutputHelper Output;

    public TestBase(ITestOutputHelper output)
    {
        Output = output;
    }
    public void WriteLog(LogEntry obj)
    {
        Output.WriteLine("{0:o}\t{1}", obj.At, obj.What);
    }
}
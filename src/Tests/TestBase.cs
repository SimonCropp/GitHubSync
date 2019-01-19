using Xunit.Abstractions;

public abstract class TestBase
{
    public ITestOutputHelper Output;

    public TestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    public void WriteLog(string message)
    {
        Output.WriteLine(message);
    }
}
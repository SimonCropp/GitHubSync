using ObjectApproval;
using Xunit.Abstractions;

public abstract class TestBase
{
    static TestBase()
    {
        SerializerBuilder.IgnoreMembersThatThrow(x=>x.Message=="Cannot escape out of a Tree.");
        SerializerBuilder.IgnoreMember<Parts>(x=>x.Sha);
    }

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
using VerifyXunit;
using Xunit;

[GlobalSetUp]
public static class GlobalSetup
{
    public static void Setup()
    {
        Global.ModifySerialization(settings =>
        {
            settings.IgnoreMembersThatThrow(x => x.Message == "Cannot escape out of a Tree.");
            settings.IgnoreMember<Parts>(x => x.Sha);
        });
    }
}
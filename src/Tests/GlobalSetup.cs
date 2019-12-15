using Verify;
using Xunit;

[GlobalSetUp]
public static class GlobalSetup
{
    public static void Setup()
    {
        SharedVerifySettings.ModifySerialization(settings =>
        {
            settings.IgnoreMembersThatThrow(x => x.Message == "Cannot escape out of a Tree.");
            settings.IgnoreMember<Parts>(x => x.Sha);
        });
    }
}
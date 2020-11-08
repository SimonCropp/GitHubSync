using System.Runtime.CompilerServices;
using VerifyTests;

public static class GlobalSetup
{
    [ModuleInitializer]
    public static void Setup()
    {
        VerifierSettings.ModifySerialization(settings =>
        {
            settings.IgnoreMembersThatThrow(x => x.Message == "Cannot escape out of a Tree.");
            settings.IgnoreMember<Parts>(x => x.Sha);
        });
    }
}
public static class GlobalSetup
{
    [ModuleInitializer]
    public static void Setup()
    {
        VerifierSettings.IgnoreMembersThatThrow(x => x.Message == "Cannot escape out of a Tree.");
        VerifierSettings.IgnoreMember<Parts>(x => x.Sha);
    }
}
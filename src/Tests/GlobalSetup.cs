public static class GlobalSetup
{
    [ModuleInitializer]
    public static void Setup()
    {
        VerifierSettings.IgnoreMembersThatThrow(_ => _.Message == "Cannot escape out of a Tree.");
        VerifierSettings.IgnoreMember<Parts>(_ => _.Sha);
    }
}
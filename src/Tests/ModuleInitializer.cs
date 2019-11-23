using VerifyXunit;

public static class ModuleInitializer
{
    public static void Initialize()
    {
        Global.IgnoreMembersThatThrow(x=>x.Message=="Cannot escape out of a Tree.");
        Global.IgnoreMember<Parts>(x=>x.Sha);
    }
}
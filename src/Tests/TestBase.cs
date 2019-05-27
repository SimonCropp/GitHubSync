using ObjectApproval;
public static class ModuleInitializer
{
    public static void Initialize()
    {
        SerializerBuilder.IgnoreMembersThatThrow(x=>x.Message=="Cannot escape out of a Tree.");
        SerializerBuilder.IgnoreMember<Parts>(x=>x.Sha);
    }
}
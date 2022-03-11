
#nullable enable

public class Mapper : MapperBase
{
    public Mapper Add(Parts from, Parts to)
    {
        AddOrRemove_Internal(from, to);
        return this;
    }

    public Mapper Add(Parts from, params Parts[] tos)
    {
        foreach (var to in tos)
        {
            AddOrRemove_Internal(from, to);
        }

        return this;
    }

    public Mapper Remove(Parts to)
    {
        AddOrRemove_Internal(Parts.Empty, to);
        return this;
    }
}
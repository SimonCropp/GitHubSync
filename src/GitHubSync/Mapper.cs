class Mapper : MapperBase
{
    public Mapper Add(Parts from, Parts to)
    {
        Add_Internal(from, to);
        return this;
    }

    public Mapper Add(Parts from, params Parts[] tos)
    {
        foreach (var to in tos)
        {
            Add_Internal(from, to);
        }

        return this;
    }
}
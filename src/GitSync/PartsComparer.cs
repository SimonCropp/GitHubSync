class PartsComparer :
    IEqualityComparer<Parts>
{
    public bool Equals(Parts? x, Parts? y)
    {
        if (x is null && y is null)
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.GetHashCode() == y.GetHashCode();
    }

    public int GetHashCode(Parts parts)
    {
        unchecked
        {
            var hashCode = parts.Owner.GetHashCode();
            hashCode = (hashCode * 397) ^ parts.Repository.GetHashCode();
            hashCode = (hashCode * 397) ^ (int) parts.Type;
            hashCode = (hashCode * 397) ^ parts.Branch.GetHashCode();

            if (parts.Path != null)
            {
                hashCode = (hashCode * 397) ^ parts.Path.GetHashCode();
            }

            return hashCode;
        }
    }
}
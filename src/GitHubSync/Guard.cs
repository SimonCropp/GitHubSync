static class Guard
{
    // ReSharper disable UnusedParameter.Global
    public static void AgainstNull(object value, string argumentName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void AgainstNullAndEmpty(string value, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(argumentName);
        }
    }

    public static void AgainstEmpty(string value, string argumentName)
    {
        if (value == null)
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cannot be only whitespace.", argumentName);
        }
    }
}
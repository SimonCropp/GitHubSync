static class Guard
{
    // ReSharper disable UnusedParameter.Global
    public static void AgainstNull(object value, [CallerArgumentExpression("value")] string paramName = "")
    {
        if (value == null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void AgainstNullAndEmpty(string value, [CallerArgumentExpression("value")] string paramName = "")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void AgainstEmpty(string value, [CallerArgumentExpression("value")] string paramName = "")
    {
        if (value == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cannot be only whitespace.", paramName);
        }
    }
}
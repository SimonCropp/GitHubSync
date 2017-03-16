using System;
using System.Threading.Tasks;
using NUnit.Framework;

public static class AssertEx
{
    public static async Task ThrowsAsync<TException>(Func<Task> func)
    {
        var expected = typeof(TException);
        Type actual = null;
        try
        {
            await func().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            actual = e.GetType();
        }
        Assert.AreEqual(expected, actual);
    }
}
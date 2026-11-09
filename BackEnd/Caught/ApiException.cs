namespace Caught;

public sealed class ApiException : Exception
{
    public int Status { get; }

    public ApiException(int status, string message) : base(message)
    {
        Status = status;
    }
}

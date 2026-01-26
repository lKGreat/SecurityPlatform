namespace Atlas.Core.Exceptions;

public sealed class BusinessException : Exception
{
    public BusinessException(string message, string code)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
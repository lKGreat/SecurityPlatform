namespace Atlas.Core.Models;

public static class ErrorCodes
{
    public const string Success = "SUCCESS";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string ServerError = "SERVER_ERROR";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string PasswordExpired = "PASSWORD_EXPIRED";
    public const string TokenExpired = "TOKEN_EXPIRED";
}

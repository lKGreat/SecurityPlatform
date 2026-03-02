namespace Atlas.Application.Models;

public sealed record RegisterRequest(
    string Username,
    string Password,
    string ConfirmPassword,
    string? CaptchaKey,
    string? CaptchaCode);

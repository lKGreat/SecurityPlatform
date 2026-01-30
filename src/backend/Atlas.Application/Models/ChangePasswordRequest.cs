namespace Atlas.Application.Models;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

namespace Atlas.WebApi.Models;

public sealed record ChangePasswordViewModel(string CurrentPassword, string NewPassword, string ConfirmPassword);

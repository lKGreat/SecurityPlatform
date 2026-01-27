using System.Text.RegularExpressions;
using Atlas.Application.Options;

namespace Atlas.Application.Security;

public static class PasswordPolicy
{
    public static bool IsCompliant(string password, PasswordPolicyOptions options, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "密码不能为空";
            return false;
        }

        if (password.Length < options.MinLength)
        {
            errorMessage = $"密码长度不能少于 {options.MinLength} 位";
            return false;
        }

        if (options.RequireUppercase && !Regex.IsMatch(password, "[A-Z]"))
        {
            errorMessage = "密码必须包含大写字母";
            return false;
        }

        if (options.RequireLowercase && !Regex.IsMatch(password, "[a-z]"))
        {
            errorMessage = "密码必须包含小写字母";
            return false;
        }

        if (options.RequireDigit && !Regex.IsMatch(password, "[0-9]"))
        {
            errorMessage = "密码必须包含数字";
            return false;
        }

        if (options.RequireNonAlphanumeric && !Regex.IsMatch(password, "[^a-zA-Z0-9]"))
        {
            errorMessage = "密码必须包含特殊字符";
            return false;
        }

        return true;
    }
}

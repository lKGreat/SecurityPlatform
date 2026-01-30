namespace Atlas.Core.Identity;

public interface ICurrentUserAccessor
{
    CurrentUserInfo? GetCurrentUser();

    CurrentUserInfo GetCurrentUserOrThrow();
}

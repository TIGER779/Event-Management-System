namespace EventHub.Enums
{
    public enum UserRole
    {
        Admin,
        User
    }

    public static class UserRoleExtensions
    {
        public static bool IsAdmin(this UserRole role)
        {
            return role == UserRole.Admin;
        }
    }
}

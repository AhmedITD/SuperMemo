namespace SuperMemo.Domain.Enums;

public enum UserRole
{
    Admin = 1,
    Customer = 2
}

public static class UserRoleExtensions
{
    public static string ToStringValue(this UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "Admin",
            UserRole.Customer => "Customer",
            _ => "Customer"
        };
    }
}

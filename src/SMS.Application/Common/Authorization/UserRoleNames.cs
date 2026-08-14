namespace SMS.Application;

public static class UserRoleNames
{
    public const string Admin = "Admin";
    public const string SupportAgent = "SupportAgent";
    public const string Customer = "Customer";
    public const string All = Admin + "," + SupportAgent + "," + Customer;
}

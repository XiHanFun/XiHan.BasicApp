namespace XiHan.BasicApp.Saas.Constants.Settings;

/// <summary>
/// SaaS 统一配置键规范。
/// </summary>
public static class SaasSettingKeys
{
    public const string Prefix = "BasicApp:Saas";

    public static class Auth
    {
        public const string LoginMethods = $"{Prefix}:Auth:LoginMethods";
        public const string ExposeDebugSecrets = $"{Prefix}:Auth:ExposeDebugSecrets";
        public const string PhoneCodeExpiresInSeconds = $"{Prefix}:Auth:PhoneCodeExpiresInSeconds";
        public const string EmailCodeExpiresInSeconds = $"{Prefix}:Auth:EmailCodeExpiresInSeconds";
        public const string TwoFactorCodeExpiresInSeconds = $"{Prefix}:Auth:TwoFactorCodeExpiresInSeconds";
    }

    public static class MultiTenancy
    {
        public const string Enabled = $"{Prefix}:MultiTenancy:Enabled";
    }

    public static class Caching
    {
        public const string AuthorizationAbsoluteExpirationMinutes = $"{Prefix}:Caching:Authorization:AbsoluteExpirationMinutes";
        public const string AuthorizationSlidingExpirationMinutes = $"{Prefix}:Caching:Authorization:SlidingExpirationMinutes";
        public const string FieldSecurityAbsoluteExpirationMinutes = $"{Prefix}:Caching:FieldSecurity:AbsoluteExpirationMinutes";
        public const string FieldSecuritySlidingExpirationMinutes = $"{Prefix}:Caching:FieldSecurity:SlidingExpirationMinutes";
        public const string LookupAbsoluteExpirationMinutes = $"{Prefix}:Caching:Lookup:AbsoluteExpirationMinutes";
        public const string LookupSlidingExpirationMinutes = $"{Prefix}:Caching:Lookup:SlidingExpirationMinutes";
        public const string MessageUnreadAbsoluteExpirationMinutes = $"{Prefix}:Caching:MessageUnread:AbsoluteExpirationMinutes";
        public const string MessageUnreadSlidingExpirationMinutes = $"{Prefix}:Caching:MessageUnread:SlidingExpirationMinutes";
    }

    public static class Seed
    {
        public const string EnableDemoData = $"{Prefix}:Seed:EnableDemoData";
        public const string BootstrapTenantCode = $"{Prefix}:Seed:BootstrapTenantCode";
        public const string BootstrapTenantName = $"{Prefix}:Seed:BootstrapTenantName";
        public const string BootstrapAdminUserName = $"{Prefix}:Seed:BootstrapAdminUserName";
    }
}

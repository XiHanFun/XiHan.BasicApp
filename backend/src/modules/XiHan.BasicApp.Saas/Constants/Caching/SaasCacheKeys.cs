namespace XiHan.BasicApp.Saas.Constants.Caching;

/// <summary>
/// SaaS 统一缓存键规范。
/// </summary>
public static class SaasCacheKeys
{
    public const string Prefix = "basicapp:saas";

    public static string TenantSegment(long? tenantId)
    {
        return tenantId.HasValue && tenantId.Value > 0 ? tenantId.Value.ToString() : "0";
    }

    public static string AuthorizationPermissionVersion(long? tenantId)
        => $"{Prefix}:authorization:permission:version:{TenantSegment(tenantId)}";

    public static string AuthorizationPermissionSet(long? tenantId, long userId, long version)
        => $"{Prefix}:permission-set:{TenantSegment(tenantId)}:{userId}:v{version}";

    public static string AuthorizationDataScopeVersion(long? tenantId)
        => $"{Prefix}:authorization:data-scope:version:{TenantSegment(tenantId)}";

    public static string AuthorizationDataScope(long? tenantId, long userId, long version)
        => $"{Prefix}:data-scope:{TenantSegment(tenantId)}:{userId}:v{version}";

    public static string LookupVersion(long? tenantId, string category)
        => $"{Prefix}:lookup:{category}:version:{TenantSegment(tenantId)}";

    public static string LookupItem(long? tenantId, string category, string key, long version)
        => $"{Prefix}:lookup:{category}:{TenantSegment(tenantId)}:{key}:v{version}";

    public static string MessageUnreadVersion(long? tenantId)
        => $"{Prefix}:message:unread:version:{TenantSegment(tenantId)}";

    public static string MessageUnread(long? tenantId, long userId, long version)
        => $"{Prefix}:message:unread:{TenantSegment(tenantId)}:{userId}:v{version}";

    public static string AuthPhoneLoginCode(long? tenantId, string phone)
        => $"{Prefix}:auth:phone-login:{TenantSegment(tenantId)}:{phone.Trim()}";

    public static string AuthEmailVerifyCode(long? tenantId, string email)
        => $"{Prefix}:auth:email-verify:{TenantSegment(tenantId)}:{email.Trim().ToLowerInvariant()}";

    public static string AuthPhoneVerifyCode(long? tenantId, string phone)
        => $"{Prefix}:auth:phone-verify:{TenantSegment(tenantId)}:{phone.Trim()}";

    public static string AuthChangeContact(long? tenantId, long userId, string purpose)
        => $"{Prefix}:auth:{purpose.Trim().ToLowerInvariant()}:{TenantSegment(tenantId)}:{userId}";

    public static string AuthTwoFactorCode(long? tenantId, string purpose, string target)
        => $"{Prefix}:auth:2fa:{purpose.Trim().ToLowerInvariant()}:{TenantSegment(tenantId)}:{target.Trim().ToLowerInvariant()}";

    public static string AuthRefreshToken(string tokenHash)
        => $"{Prefix}:auth:refresh:{tokenHash}";

    public static string AuthSessionTokenMap(string sessionId)
        => $"{Prefix}:auth:session:{sessionId.Trim()}";

    public static string AuthRecoveryCodes(long? tenantId, long userId, string hash)
        => $"{Prefix}:auth:recovery:{TenantSegment(tenantId)}:{userId}:{hash}";

    public static string SettingsValue(string providerName, string providerKey, string settingName)
        => $"{Prefix}:config:setting:{providerName}:{providerKey}:{settingName}";
}

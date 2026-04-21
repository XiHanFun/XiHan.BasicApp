namespace XiHan.BasicApp.Saas.Constants.Caching;

/// <summary>
/// 查询缓存键模板。
/// </summary>
public static class QueryCacheKeys
{
    public const string AuthPermissionCodes = $"{SaasCacheKeys.Prefix}:query:auth:permission-codes:{{tenantId}}:{{userId}}";
    public const string ConstraintRuleById = $"{SaasCacheKeys.Prefix}:query:constraint-rule:id:{{id}}";
    public const string DictById = $"{SaasCacheKeys.Prefix}:query:dict:id:{{id}}";
    public const string DictByCode = $"{SaasCacheKeys.Prefix}:query:dict:code:{{code}}";
    public const string DepartmentById = $"{SaasCacheKeys.Prefix}:query:department:id:{{id}}";
    public const string FileById = $"{SaasCacheKeys.Prefix}:query:file:id:{{id}}";
    public const string MenuById = $"{SaasCacheKeys.Prefix}:query:menu:id:{{id}}";
    public const string NotificationById = $"{SaasCacheKeys.Prefix}:query:notification:id:{{id}}";
    public const string OAuthAppById = $"{SaasCacheKeys.Prefix}:query:oauth-app:id:{{id}}";
    public const string PermissionById = $"{SaasCacheKeys.Prefix}:query:permission:id:{{id}}";
    public const string ProfileUserById = $"{SaasCacheKeys.Prefix}:query:profile:user:{{userId}}";
    public const string ReviewById = $"{SaasCacheKeys.Prefix}:query:review:id:{{id}}";
    public const string RoleById = $"{SaasCacheKeys.Prefix}:query:role:id:{{id}}";
    public const string TaskById = $"{SaasCacheKeys.Prefix}:query:task:id:{{id}}";
    public const string TenantById = $"{SaasCacheKeys.Prefix}:query:tenant:id:{{id}}";
    public const string UserById = $"{SaasCacheKeys.Prefix}:query:user:id:{{id}}";

    public static string ConstraintRuleByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:constraint-rule:id:{id}";
    public static string DictByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:dict:id:{id}";
    public static string DictByCodeValue(string code) => $"{SaasCacheKeys.Prefix}:query:dict:code:{code.Trim()}";
    public static string DepartmentByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:department:id:{id}";
    public static string FileByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:file:id:{id}";
    public static string MenuByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:menu:id:{id}";
    public static string NotificationByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:notification:id:{id}";
    public static string OAuthAppByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:oauth-app:id:{id}";
    public static string PermissionByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:permission:id:{id}";
    public static string ReviewByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:review:id:{id}";
    public static string RoleByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:role:id:{id}";
    public static string TaskByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:task:id:{id}";
    public static string TenantByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:tenant:id:{id}";
    public static string UserByIdValue(long id) => $"{SaasCacheKeys.Prefix}:query:user:id:{id}";
}

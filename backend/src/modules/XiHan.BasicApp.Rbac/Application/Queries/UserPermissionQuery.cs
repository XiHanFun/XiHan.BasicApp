namespace XiHan.BasicApp.Rbac.Application.Queries;

/// <summary>
/// 用户权限查询
/// </summary>
public class UserPermissionQuery
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

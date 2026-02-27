namespace XiHan.BasicApp.Rbac.Application.Queries;

/// <summary>
/// 用户菜单查询
/// </summary>
public class UserMenuQuery
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

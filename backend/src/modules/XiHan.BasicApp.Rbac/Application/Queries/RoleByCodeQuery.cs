namespace XiHan.BasicApp.Rbac.Application.Queries;

/// <summary>
/// 根据编码查询角色
/// </summary>
public class RoleByCodeQuery
{
    /// <summary>
    /// 角色编码
    /// </summary>
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

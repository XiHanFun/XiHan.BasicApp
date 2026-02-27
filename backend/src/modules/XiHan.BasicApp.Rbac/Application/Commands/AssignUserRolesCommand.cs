namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 分配用户角色命令
/// </summary>
public class AssignUserRolesCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    public IReadOnlyCollection<long> RoleIds { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

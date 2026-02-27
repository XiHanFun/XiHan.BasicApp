namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 分配角色菜单命令
/// </summary>
public class AssignRoleMenusCommand
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    public IReadOnlyCollection<long> MenuIds { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

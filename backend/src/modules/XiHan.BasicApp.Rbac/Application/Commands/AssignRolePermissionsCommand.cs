namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 分配角色权限命令
/// </summary>
public class AssignRolePermissionsCommand
{
    /// <summary>
    /// 角色ID
    /// </summary>
    public long RoleId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public IReadOnlyCollection<long> PermissionIds { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

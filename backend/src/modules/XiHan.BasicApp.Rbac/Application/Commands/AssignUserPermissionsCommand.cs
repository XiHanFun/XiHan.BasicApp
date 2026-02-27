namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 分配用户直授权限命令
/// </summary>
public class AssignUserPermissionsCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 权限ID
    /// </summary>
    public IReadOnlyCollection<long> PermissionIds { get; set; } = [];

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.InternalServices;

/// <summary>
/// 超级管理员保护规则。
/// </summary>
public interface ISuperAdminGuard
{
    /// <summary>
    /// 校验目标角色分配是否合法。
    /// </summary>
    Task EnsureRoleAssignmentAllowedAsync(
        SysUser user,
        IReadOnlyCollection<long> targetRoleIds,
        IReadOnlyCollection<SysRole> targetRoles,
        long? tenantId);

    /// <summary>
    /// 校验账号是否允许执行敏感操作。
    /// </summary>
    Task EnsureAccountMutableAsync(SysUser user, long? tenantId, string operationName);
}

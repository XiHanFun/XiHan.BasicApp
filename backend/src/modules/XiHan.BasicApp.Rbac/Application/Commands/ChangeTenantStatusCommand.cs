using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 修改租户状态命令
/// </summary>
public class ChangeTenantStatusCommand
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long TenantId { get; set; }

    /// <summary>
    /// 租户状态
    /// </summary>
    public TenantStatus TenantStatus { get; set; } = TenantStatus.Normal;
}

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 权限 DTO
/// </summary>
public class PermissionDto : BasicAppDto
{
    /// <summary>
    /// 资源ID
    /// </summary>
    public long ResourceId { get; set; }

    /// <summary>
    /// 操作ID
    /// </summary>
    public long OperationId { get; set; }

    /// <summary>
    /// 权限代码
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 权限名称
    /// </summary>
    public string PermissionName { get; set; } = string.Empty;

    /// <summary>
    /// 权限描述
    /// </summary>
    public string? PermissionDescription { get; set; }

    /// <summary>
    /// 是否需要审核
    /// </summary>
    public bool IsRequireAudit { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

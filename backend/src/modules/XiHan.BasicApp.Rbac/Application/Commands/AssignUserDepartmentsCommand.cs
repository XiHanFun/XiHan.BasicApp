namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 分配用户部门命令
/// </summary>
public class AssignUserDepartmentsCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public IReadOnlyCollection<long> DepartmentIds { get; set; } = [];

    /// <summary>
    /// 主部门ID
    /// </summary>
    public long? MainDepartmentId { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

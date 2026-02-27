namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 修改密码命令
/// </summary>
public class ChangePasswordCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 旧密码
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

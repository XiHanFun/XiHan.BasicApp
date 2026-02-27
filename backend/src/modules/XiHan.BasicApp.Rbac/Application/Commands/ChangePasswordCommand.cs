using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 修改密码命令
/// </summary>
public class ChangePasswordCommand
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [Range(1, long.MaxValue, ErrorMessage = "用户 ID 无效")]
    public long UserId { get; set; }

    /// <summary>
    /// 旧密码
    /// </summary>
    [Required(ErrorMessage = "原密码不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "原密码不能为空")]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required(ErrorMessage = "新密码不能为空")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "新密码不能为空")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

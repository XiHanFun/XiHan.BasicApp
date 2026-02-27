namespace XiHan.BasicApp.Rbac.Application.Commands;

/// <summary>
/// 用户登录命令
/// </summary>
public class UserLoginCommand
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 登录IP
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }
}

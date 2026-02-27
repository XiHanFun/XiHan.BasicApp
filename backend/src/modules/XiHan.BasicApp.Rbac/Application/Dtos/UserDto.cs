using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Domain.Enums;

namespace XiHan.BasicApp.Rbac.Application.Dtos;

/// <summary>
/// 用户 DTO
/// </summary>
public class UserDto : BasicAppDto
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 创建用户 DTO
/// </summary>
public class UserCreateDto : BasicAppCDto
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
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }
}

/// <summary>
/// 更新用户 DTO
/// </summary>
public class UserUpdateDto : BasicAppUDto
{
    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public UserGender Gender { get; set; } = UserGender.Unknown;

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 登录结果 DTO
/// </summary>
public class UserLoginResultDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 登录结果
    /// </summary>
    public LoginResult LoginResult { get; set; } = LoginResult.Failed;

    /// <summary>
    /// 消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 权限代码
    /// </summary>
    public IReadOnlyCollection<string> PermissionCodes { get; set; } = [];
}

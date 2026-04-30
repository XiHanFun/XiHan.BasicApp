#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginListItemDto
// Guid:4cac21b6-a7e1-42b6-9378-8cb9b26c0d84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 第三方登录绑定列表项 DTO
/// </summary>
public class ExternalLoginListItemDto : BasicAppDto
{
    /// <summary>
    /// 用户主键
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string? NickName { get; set; }

    /// <summary>
    /// 提供商名称
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 脱敏后的外部账号标识
    /// </summary>
    public string ExternalAccountMasked { get; set; } = string.Empty;

    /// <summary>
    /// 提供商显示名称
    /// </summary>
    public string? ProviderDisplayName { get; set; }

    /// <summary>
    /// 脱敏后的三方邮箱
    /// </summary>
    public string? ExternalEmailMasked { get; set; }

    /// <summary>
    /// 是否存在三方头像
    /// </summary>
    public bool HasAvatar { get; set; }

    /// <summary>
    /// 最后登录时间
    /// </summary>
    public DateTimeOffset? LastLoginTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

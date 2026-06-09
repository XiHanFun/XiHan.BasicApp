#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginPageQueryDto
// Guid:417d2ad3-2355-4d6a-8a0a-4ce877b9ae88
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 第三方登录绑定分页查询 DTO
/// </summary>
public sealed class ExternalLoginPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 用户主键
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 提供商名称
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// 最后登录开始时间
    /// </summary>
    public DateTimeOffset? LastLoginTimeStart { get; set; }

    /// <summary>
    /// 最后登录结束时间
    /// </summary>
    public DateTimeOffset? LastLoginTimeEnd { get; set; }
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogPageQueryDto
// Guid:9f50f8cf-058c-46c9-ae56-cbe832f1062e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 登录日志分页查询 DTO
/// </summary>
public sealed class LoginLogPageQueryDto : BasicAppPRDto
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
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 会话标识
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 登录结果
    /// </summary>
    public LoginResult? LoginResult { get; set; }

    /// <summary>
    /// 是否风险登录
    /// </summary>
    public bool? IsRiskLogin { get; set; }

    /// <summary>
    /// 登录开始时间
    /// </summary>
    public DateTimeOffset? LoginTimeStart { get; set; }

    /// <summary>
    /// 登录结束时间
    /// </summary>
    public DateTimeOffset? LoginTimeEnd { get; set; }
}

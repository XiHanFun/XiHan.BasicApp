#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginLogListItemDto
// Guid:1c32c1e5-c45e-47a8-80d3-6b09f11c42aa
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 登录日志列表项 DTO
/// </summary>
public class LoginLogListItemDto : BasicAppDto
{
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
    public LoginResult LoginResult { get; set; }

    /// <summary>
    /// 是否风险登录
    /// </summary>
    public bool IsRiskLogin { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    public DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 是否包含客户端上下文
    /// </summary>
    public bool HasClientContext { get; set; }

    /// <summary>
    /// 是否包含设备上下文
    /// </summary>
    public bool HasDeviceContext { get; set; }

    /// <summary>
    /// 是否包含结果说明
    /// </summary>
    public bool HasResultNote { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

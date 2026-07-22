// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// 登录 IP
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// 登录地址
    /// </summary>
    public string? LoginLocation { get; set; }

    /// <summary>
    /// 浏览器
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 设备
    /// </summary>
    public string? Device { get; set; }

    /// <summary>
    /// 设备标识
    /// </summary>
    public string? DeviceId { get; set; }

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
    /// 登录消息
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

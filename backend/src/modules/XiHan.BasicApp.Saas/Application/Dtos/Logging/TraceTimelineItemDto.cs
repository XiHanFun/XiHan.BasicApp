#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TraceTimelineItemDto
// Guid:7b2f5ca4-3d69-4e28-9071-0b8c4d6f3e92
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/09 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 链路追踪时间线条目 DTO（跨日志类型归一化）
/// </summary>
public sealed class TraceTimelineItemDto
{
    /// <summary>
    /// 日志类型
    /// </summary>
    public TraceLogType LogType { get; set; }

    /// <summary>
    /// 日志主键（用于按类型回查完整详情）
    /// </summary>
    public long BasicId { get; set; }

    /// <summary>
    /// 条目时间（各类日志的主时间：访问/请求/操作/登录/异常/审计/变更时间）
    /// </summary>
    public DateTimeOffset Time { get; set; }

    /// <summary>
    /// 标题（动作摘要，如 "GET /api/users"、异常类型、操作标题）
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 摘要（次要说明）
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// 归一化状态（success / warning / error / info / default），驱动时间线颜色
    /// </summary>
    public string Status { get; set; } = "default";

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
    /// IP 地址
    /// </summary>
    public string? Ip { get; set; }

    /// <summary>
    /// 地理位置
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 请求路径 / 资源路径
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// 响应状态码（无则为 null）
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 执行耗时（毫秒，无则为 null）
    /// </summary>
    public long? ExecutionTime { get; set; }
}

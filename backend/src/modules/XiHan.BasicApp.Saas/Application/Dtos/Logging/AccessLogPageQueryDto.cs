#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AccessLogPageQueryDto
// Guid:4de5254c-e05a-4cf2-91c4-9d05f9b6c191
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 访问日志分页查询 DTO
/// </summary>
public sealed class AccessLogPageQueryDto : BasicAppPRDto
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
    /// 资源路径
    /// </summary>
    public string? ResourcePath { get; set; }

    /// <summary>
    /// 资源类型
    /// </summary>
    public string? ResourceType { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 访问结果
    /// </summary>
    public AccessResult? AccessResult { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 最小执行耗时（毫秒）
    /// </summary>
    public long? MinExecutionTime { get; set; }

    /// <summary>
    /// 最大执行耗时（毫秒）
    /// </summary>
    public long? MaxExecutionTime { get; set; }

    /// <summary>
    /// 访问开始时间
    /// </summary>
    public DateTimeOffset? AccessTimeStart { get; set; }

    /// <summary>
    /// 访问结束时间
    /// </summary>
    public DateTimeOffset? AccessTimeEnd { get; set; }
}

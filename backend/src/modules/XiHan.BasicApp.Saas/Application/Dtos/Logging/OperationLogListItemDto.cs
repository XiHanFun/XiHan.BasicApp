// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作日志列表项 DTO
/// </summary>
public class OperationLogListItemDto : BasicAppDto
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
    /// 操作类型
    /// </summary>
    public OperationType OperationType { get; set; }

    /// <summary>
    /// 操作模块
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// 操作功能
    /// </summary>
    public string? Function { get; set; }

    /// <summary>
    /// 操作标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 请求 URL
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long ExecutionTime { get; set; }

    /// <summary>
    /// 操作 IP
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 操作地址
    /// </summary>
    public string? OperationLocation { get; set; }

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
    /// 操作执行结果（成功/失败/部分成功）
    /// </summary>
    public OperationExecuteResult Result { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTimeOffset OperationTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

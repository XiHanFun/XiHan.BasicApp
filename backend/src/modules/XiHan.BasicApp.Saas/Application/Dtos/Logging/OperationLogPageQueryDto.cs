// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 操作日志分页查询 DTO
/// </summary>
public sealed class OperationLogPageQueryDto : BasicAppPRDto
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
    /// 操作类型
    /// </summary>
    public OperationType? OperationType { get; set; }

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
    /// 请求方法
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// 操作 IP（模糊匹配）
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 操作执行结果（成功/失败/部分成功）
    /// </summary>
    public OperationExecuteResult? Result { get; set; }

    /// <summary>
    /// 最小执行耗时（毫秒）
    /// </summary>
    public long? MinExecutionTime { get; set; }

    /// <summary>
    /// 最大执行耗时（毫秒）
    /// </summary>
    public long? MaxExecutionTime { get; set; }

    /// <summary>
    /// 操作开始时间
    /// </summary>
    public DateTimeOffset? OperationTimeStart { get; set; }

    /// <summary>
    /// 操作结束时间
    /// </summary>
    public DateTimeOffset? OperationTimeEnd { get; set; }
}

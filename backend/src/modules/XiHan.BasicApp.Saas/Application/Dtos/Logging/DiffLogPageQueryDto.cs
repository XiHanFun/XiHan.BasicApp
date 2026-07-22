// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 差异日志分页查询 DTO
/// </summary>
public sealed class DiffLogPageQueryDto : BasicAppPRDto
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
    /// 请求标识
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 审计类型
    /// </summary>
    public string? AuditType { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public OperationType? OperationType { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 实体 ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// 实体名称
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// 表名称
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// 操作 IP（模糊匹配）
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool? IsSuccess { get; set; }

    /// <summary>
    /// 风险等级
    /// </summary>
    public AuditRiskLevel? RiskLevel { get; set; }

    /// <summary>
    /// 最小执行耗时（毫秒）
    /// </summary>
    public long? MinExecutionTime { get; set; }

    /// <summary>
    /// 最大执行耗时（毫秒）
    /// </summary>
    public long? MaxExecutionTime { get; set; }

    /// <summary>
    /// 审计开始时间
    /// </summary>
    public DateTimeOffset? AuditTimeStart { get; set; }

    /// <summary>
    /// 审计结束时间
    /// </summary>
    public DateTimeOffset? AuditTimeEnd { get; set; }
}

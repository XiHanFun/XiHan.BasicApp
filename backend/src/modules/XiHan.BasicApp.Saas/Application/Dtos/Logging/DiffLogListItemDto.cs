// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 差异日志列表项 DTO
/// </summary>
public class DiffLogListItemDto : BasicAppDto
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
    public string AuditType { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    public OperationType OperationType { get; set; }

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
    /// 主键字段
    /// </summary>
    public string? PrimaryKey { get; set; }

    /// <summary>
    /// 主键值
    /// </summary>
    public string? PrimaryKeyValue { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 变更摘要
    /// </summary>
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// 操作 IP
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// 风险等级
    /// </summary>
    public AuditRiskLevel RiskLevel { get; set; }

    /// <summary>
    /// 执行耗时（毫秒）
    /// </summary>
    public long ExecutionTime { get; set; }

    /// <summary>
    /// 审计时间
    /// </summary>
    public DateTimeOffset AuditTime { get; set; }

    /// <summary>
    /// 变更前数据
    /// </summary>
    public string? BeforeData { get; set; }

    /// <summary>
    /// 变更后数据
    /// </summary>
    public string? AfterData { get; set; }

    /// <summary>
    /// 变更字段
    /// </summary>
    public string? ChangedFields { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

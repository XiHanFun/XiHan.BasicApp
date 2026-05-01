#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditLogListItemDto
// Guid:d50f32b5-4735-42d4-98ee-edda4607f646
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 审计日志列表项 DTO
/// </summary>
public class AuditLogListItemDto : BasicAppDto
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
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

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
    /// 是否包含操作上下文
    /// </summary>
    public bool HasOperationContext { get; set; }

    /// <summary>
    /// 是否包含操作描述
    /// </summary>
    public bool HasAuditText { get; set; }

    /// <summary>
    /// 是否包含变更摘要
    /// </summary>
    public bool HasChangeSummary { get; set; }

    /// <summary>
    /// 是否包含变更前快照
    /// </summary>
    public bool HasBeforeSnapshot { get; set; }

    /// <summary>
    /// 是否包含变更后快照
    /// </summary>
    public bool HasAfterSnapshot { get; set; }

    /// <summary>
    /// 是否包含变更字段
    /// </summary>
    public bool HasFieldChanges { get; set; }

    /// <summary>
    /// 是否包含异常信息
    /// </summary>
    public bool HasException { get; set; }

    /// <summary>
    /// 是否包含扩展数据
    /// </summary>
    public bool HasExtension { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; }
}

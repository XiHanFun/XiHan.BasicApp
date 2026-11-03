// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 已处理的工作流事件（租户库实体）
/// </summary>
/// <remarks>
/// 设计稿 D7：投递至少一次，框架没有收件箱、去重由处理器按消息 ID 自理。通知处理器在同一个事务里先写这一行再发通知、写抄送记录，
/// 同一条消息再投递时看到这一行即跳过；两次投递并发时第二次撞唯一索引失败、由代理重试，重试时同样跳过
/// </remarks>
[SugarIndex("ux_{table}_tenant_id_message_id", nameof(TenantId), OrderByType.Asc, nameof(MessageId), OrderByType.Asc, true)]
public sealed class SysWorkflowMessageReceipt : TenantScopedRecord
{
    /// <summary>
    /// 消息 ID（信封的 ID）
    /// </summary>
    [StringLength(128)]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// 主题
    /// </summary>
    [StringLength(128)]
    public string Topic { get; set; } = string.Empty;
}

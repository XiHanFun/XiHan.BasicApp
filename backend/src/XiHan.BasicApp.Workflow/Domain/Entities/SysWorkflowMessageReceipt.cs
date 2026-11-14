// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 已处理的工作流事件（租户库实体）
/// </summary>
/// <remarks>
/// 设计稿 D7：工作流事件至少投递一次，框架没有收件箱，去重由处理器按消息 ID 自理，这张表就是处理过的消息的回执
/// 通知处理器第一次处理一条消息时在同一个事务里先写回执，再发站内通知、写抄送记录：回执与它们同进退，再投递时看到回执即跳过，不会重复通知或重复抄送
/// 两次投递并发时靠「侧 + 消息 ID」的唯一索引兜住：第二次撞索引失败、由代理重试，重试时看到回执同样跳过；回执只追加、没有软删除，用普通唯一索引即可
/// 平台一侧的回执租户为 <c>platform</c>；没有清理作业，行数随处理过的事件一直增长
/// </remarks>
[SugarTable(TableName = "sys_workflow_message_receipt", TableDescription = "已处理的工作流事件（租户库实体）")]
[SugarIndex("ux_sys_workflow_message_receipt_tenant_id_message_id", nameof(TenantId), OrderByType.Asc, nameof(MessageId), OrderByType.Asc, true)]
public sealed class SysWorkflowMessageReceipt : TenantScopedRecord
{
    /// <summary>
    /// 消息 ID（信封的 ID）
    /// </summary>
    [SugarColumn(ColumnName = "message_id", Length = 128, IsNullable = false, ColumnDescription = "消息 ID（信封的 ID）")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// 主题
    /// </summary>
    [SugarColumn(ColumnName = "topic", Length = 128, IsNullable = false, ColumnDescription = "主题")]
    public string Topic { get; set; } = string.Empty;
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Workflow.Domain.Entities;

/// <summary>
/// 审批记录动作
/// </summary>
public enum WorkflowTaskRecordAction
{
    /// <summary>
    /// 办理（同意/驳回/自定义结果）
    /// </summary>
    [Description("办理")]
    Completed = 1,

    /// <summary>
    /// 转办
    /// </summary>
    [Description("转办")]
    Transferred = 2,

    /// <summary>
    /// 加签
    /// </summary>
    [Description("加签")]
    AddedAssignees = 3,

    /// <summary>
    /// 被抄送
    /// </summary>
    [Description("抄送")]
    CarbonCopied = 4
}

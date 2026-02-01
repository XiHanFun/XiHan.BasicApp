#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTaskLog.pl
// Guid:2e28152c-d6e9-4396-addb-b479254bad61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 06:52:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统任务日志实体扩展
/// </summary>
public partial class SysTaskLog
{
    /// <summary>
    /// 任务信息
    /// </summary>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.ManyToOne, nameof(TaskId))]
    public virtual SysTask? Task { get; set; }
}

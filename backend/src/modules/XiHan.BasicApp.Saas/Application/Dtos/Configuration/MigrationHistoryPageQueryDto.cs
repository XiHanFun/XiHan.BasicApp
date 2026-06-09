#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MigrationHistoryPageQueryDto
// Guid:30533c14-064d-4154-add7-3b0f3996a00e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统迁移历史分页查询 DTO
/// </summary>
public sealed class MigrationHistoryPageQueryDto : BasicAppPRDto
{
    /// <summary>
    /// 关键字
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 版本
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// 脚本名称
    /// </summary>
    public string? ScriptName { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool? Success { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    public string? NodeName { get; set; }

    /// <summary>
    /// 执行时间起始
    /// </summary>
    public DateTimeOffset? ExecutedTimeStart { get; set; }

    /// <summary>
    /// 执行时间结束
    /// </summary>
    public DateTimeOffset? ExecutedTimeEnd { get; set; }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个升级脚本的执行记录。
/// </summary>
/// <remarks>
/// 每执行一个脚本记一行，成功与失败都记；同一脚本失败后修好重试会再记一行。
/// </remarks>
public sealed class SysMigrationHistory : AppEntity
{
    /// <summary>
    /// 脚本所在的版本目录。
    /// </summary>
    [StringLength(32)]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 脚本文件名。
    /// </summary>
    [StringLength(256)]
    public string ScriptName { get; set; } = string.Empty;

    /// <summary>
    /// 开始执行的时间。
    /// </summary>
    public DateTimeOffset ExecutedAt { get; set; }

    /// <summary>
    /// 耗时（毫秒）。
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// 是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 执行的节点。
    /// </summary>
    [StringLength(128)]
    public string? NodeName { get; set; }

    /// <summary>
    /// 失败的原因。
    /// </summary>
    [LongText]
    public string? ErrorMessage { get; set; }
}

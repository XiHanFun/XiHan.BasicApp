// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一次结构升级
/// </summary>
/// <remarks>
/// 一次启动、一次 <c>schema upgrade</c> 或一次重试是一次升级：平台库与各租户独立库的步骤都记同一个 <see cref="Id"/>（<see cref="SysMigrationHistory.RunId"/>）
/// </remarks>
/// <param name="Id">批次号</param>
/// <param name="Node">执行的节点（机器名与进程号）</param>
public sealed record SchemaRun(Guid Id, string Node)
{
    /// <summary>
    /// 在本节点开始一次升级
    /// </summary>
    public static SchemaRun Start() => new(Guid.CreateVersion7(), $"{Environment.MachineName}:{Environment.ProcessId}");
}

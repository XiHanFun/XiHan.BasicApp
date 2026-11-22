// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 插件主体的数据范围
/// </summary>
/// <remarks>
/// 插件主体（<see cref="PluginPrincipal"/>）在租户里缺省看不到受数据范围约束的行，平台批准了「全部」的插件才不受限
/// 插件模块实现；模块关掉时没有实现，也就没有插件主体
/// </remarks>
public interface IPluginDataScopes
{
    /// <summary>
    /// 插件主体是否不受数据范围限制
    /// </summary>
    /// <param name="manifestId">插件的清单 ID（即主体 ID）</param>
    bool IsUnrestricted(string manifestId);
}

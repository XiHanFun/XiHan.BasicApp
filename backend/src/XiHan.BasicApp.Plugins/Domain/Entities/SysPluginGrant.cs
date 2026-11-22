// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Plugins.Domain.Entities;

/// <summary>
/// 平台批准给插件的一项授予（平台库实体）
/// </summary>
/// <remarks>
/// 存授予的原文：框架认的 <c>capability:</c>、<c>fs.read:</c>、<c>fs.write:</c>、<c>net.outbound:</c>、<c>config:</c>、<c>binding:</c> 加载时原样交给插件宿主；
/// BasicApp 自己的 <c>callback:&lt;权限码&gt;</c>（插件主体回调宿主操作要持有的码）与 <c>data-scope:all</c>（插件主体在租户里看全部数据）由插件模块解释
/// 授权是整组替换，旧的物理删除，变更记在操作日志里
/// </remarks>
[SugarTable(TableName = "sys_plugin_grant", TableDescription = "平台批准给插件的一项授予（平台库实体）")]
[SugarIndex("ux_sys_plugin_grant_plugin_id_grant_value", nameof(PluginId), OrderByType.Asc, nameof(GrantValue), OrderByType.Asc, true)]
public sealed class SysPluginGrant : AppRecord
{
    /// <summary>
    /// 插件
    /// </summary>
    [SugarColumn(ColumnName = "plugin_id", IsNullable = false, ColumnDescription = "插件")]
    public Guid PluginId { get; set; }

    /// <summary>
    /// 授予原文
    /// </summary>
    /// <remarks>
    /// 不叫 <c>grant</c>：它是各库的保留字
    /// </remarks>
    [SugarColumn(ColumnName = "grant_value", Length = 512, IsNullable = false, ColumnDescription = "授予原文")]
    public string GrantValue { get; set; } = string.Empty;
}

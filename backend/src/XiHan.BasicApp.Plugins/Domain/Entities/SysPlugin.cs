// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Plugins.Domain.Entities;

/// <summary>
/// 已安装的插件（平台库实体）
/// </summary>
/// <remarks>
/// 插件进程全平台共用，只有平台管理，所以落平台库、不分侧；租户经套餐与角色使用插件贡献的功能
/// 清单 ID 与短名都唯一：清单 ID 是框架登记操作与权限码的归属，短名是权限码、页面码与路由的包段；软删除之后可以重新安装
/// 安装时的版本号、隔离级别与发布者从当前版本抄过来，列表不用联表；版本的包与清单原文在 <see cref="SysPluginVersion"/>
/// 刚装好是停用状态，批准的授予在 <see cref="SysPluginGrant"/>
/// </remarks>
[SugarTable(TableName = "sys_plugin", TableDescription = "已安装的插件（平台库实体）")]
[EntityIndex(nameof(ManifestId), IsUnique = true, Name = "ux_sys_plugin_manifest_id")]
[EntityIndex(nameof(Name), IsUnique = true, Name = "ux_sys_plugin_name")]
public sealed class SysPlugin : AppAuditedEntity
{
    /// <summary>
    /// 清单 ID
    /// </summary>
    [SugarColumn(ColumnName = "manifest_id", Length = 256, IsNullable = false, ColumnDescription = "清单 ID")]
    public string ManifestId { get; set; } = string.Empty;

    /// <summary>
    /// 短名
    /// </summary>
    /// <remarks>
    /// 权限码、页面码的包段与页面路由 <c>/plugins/&lt;短名&gt;/</c> 的一段
    /// </remarks>
    [SugarColumn(ColumnName = "name", Length = 64, IsNullable = false, ColumnDescription = "短名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 发布者
    /// </summary>
    [SugarColumn(ColumnName = "publisher", Length = 128, IsNullable = true, ColumnDescription = "发布者")]
    public string? Publisher { get; set; }

    /// <summary>
    /// 当前版本号
    /// </summary>
    [SugarColumn(ColumnName = "version", Length = 64, IsNullable = false, ColumnDescription = "当前版本号")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 当前版本
    /// </summary>
    [SugarColumn(ColumnName = "current_version_id", IsNullable = false, ColumnDescription = "当前版本")]
    public Guid CurrentVersionId { get; set; }

    /// <summary>
    /// 隔离级别
    /// </summary>
    [SugarColumn(ColumnName = "isolation", IsNullable = false, ColumnDescription = "隔离级别")]
    public PluginIsolation Isolation { get; set; } = PluginIsolation.OutOfProcess;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnName = "is_enabled", IsNullable = false, ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; }

    /// <summary>
    /// 最近一次加载失败的错误码
    /// </summary>
    /// <remarks>
    /// 对齐服务加载失败时写入、加载成功后清空；多个节点各自加载，记的是最后写入的那个节点的结论
    /// </remarks>
    [SugarColumn(ColumnName = "last_error_code", Length = 64, IsNullable = true, ColumnDescription = "最近一次加载失败的错误码")]
    public string? LastErrorCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 512, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

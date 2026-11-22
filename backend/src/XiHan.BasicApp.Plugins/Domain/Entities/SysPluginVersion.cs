// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Plugins.Domain.Entities;

/// <summary>
/// 插件的一个已上传版本（平台库实体）
/// </summary>
/// <remarks>
/// 包本身是 Files 里的一个文件，这里只按 ID 引用，并记下当时算出的 SHA-256，各实例同步包时据此核对没被换掉
/// 框架清单与旁挂清单原文照存：详情、授权页与运行期都按它们解释，不再回去解包
/// 同一插件的版本号唯一；留着旧版本是为了升级失败时能退回
/// </remarks>
[SugarTable(TableName = "sys_plugin_version", TableDescription = "插件的一个已上传版本（平台库实体）")]
[EntityIndex(nameof(PluginId), nameof(Version), IsUnique = true, Name = "ux_sys_plugin_version_plugin_id_version")]
public sealed class SysPluginVersion : AppAuditedEntity
{
    /// <summary>
    /// 插件
    /// </summary>
    [SugarColumn(ColumnName = "plugin_id", IsNullable = false, ColumnDescription = "插件")]
    public Guid PluginId { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [SugarColumn(ColumnName = "version", Length = 64, IsNullable = false, ColumnDescription = "版本号")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 包文件
    /// </summary>
    [SugarColumn(ColumnName = "file_id", IsNullable = false, ColumnDescription = "包文件")]
    public Guid FileId { get; set; }

    /// <summary>
    /// 包的 SHA-256（小写十六进制）
    /// </summary>
    [SugarColumn(ColumnName = "sha256", Length = 64, IsNullable = false, ColumnDescription = "包的 SHA-256")]
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>
    /// 包的字节数
    /// </summary>
    [SugarColumn(ColumnName = "size", IsNullable = false, ColumnDescription = "包的字节数")]
    public long Size { get; set; }

    /// <summary>
    /// 框架清单原文
    /// </summary>
    [SugarColumn(ColumnName = "manifest_json", IsNullable = false, ColumnDescription = "框架清单原文")]
    [LongText]
    public string ManifestJson { get; set; } = string.Empty;

    /// <summary>
    /// 旁挂清单原文
    /// </summary>
    [SugarColumn(ColumnName = "descriptor_json", IsNullable = false, ColumnDescription = "旁挂清单原文")]
    [LongText]
    public string DescriptorJson { get; set; } = string.Empty;
}

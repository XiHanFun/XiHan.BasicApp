#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysStorageConfig
// Guid:4c8d9e0f-2b3c-4d5e-6f7a-8b9c0d1e2f3a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统存储配置实体
/// 文件/附件存储提供商配置表：承载各存储连接凭证、端点信息及默认存储选择
/// </summary>
/// <remarks>
/// 关联：
/// - SysFileStorage.StorageConfigId → SysStorageConfig（多对一）
///
/// 写入：
/// - ConfigCode 在租户内必须唯一（UX_TeId_CoCd）
/// - 同一租户下有且仅有一条 IsDefault=true 的记录；服务层必须保证互斥
/// - SecretAccessKey 为敏感字段，建议按需脱敏传输/显示
///
/// 查询：
/// - 获取默认存储：WHERE IsDefault=true AND IsEnabled=true
/// - 按存储类型筛选：IX_TeId_StTy
/// - 获取启用的存储列表：WHERE IsEnabled=true ORDER BY Sort
///
/// 删除：
/// - 仅软删；正在被 SysFileStorage 引用的配置禁止软删
///
/// 状态：
/// - IsEnabled: true=启用 / false=禁用（禁用后该存储不可用）
/// - IsDefault: 标识默认存储配置
///
/// 场景：
/// - 多存储后端切换（本地/OSS/S3/MinIO）
/// - 按地域就近接入
/// - 冷热数据分层存储
/// </remarks>
[SugarTable("SysStorageConfig", "系统存储配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_CoCd", nameof(TenantId), OrderByType.Asc, nameof(ConfigCode), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_TeId_StTy", nameof(TenantId), OrderByType.Asc, nameof(StorageType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe_IsEn", nameof(TenantId), OrderByType.Asc, nameof(IsDefault), OrderByType.Desc, nameof(IsEnabled), OrderByType.Asc)]
public partial class SysStorageConfig : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 配置编码（租户内唯一标识）
    /// </summary>
    [SugarColumn(ColumnDescription = "配置编码", Length = 100, IsNullable = false)]
    public virtual string ConfigCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置名称
    /// </summary>
    [SugarColumn(ColumnDescription = "配置名称", Length = 200, IsNullable = false)]
    public virtual string ConfigName { get; set; } = string.Empty;

    /// <summary>
    /// 存储类型
    /// </summary>
    [SugarColumn(ColumnDescription = "存储类型")]
    public virtual StorageConfigType StorageType { get; set; } = StorageConfigType.Local;

    /// <summary>
    /// 端点URL（S3兼容接口地址）
    /// </summary>
    [SugarColumn(ColumnDescription = "端点URL", Length = 500, IsNullable = true)]
    public virtual string? Endpoint { get; set; }

    /// <summary>
    /// 区域/地域
    /// </summary>
    [SugarColumn(ColumnDescription = "区域", Length = 100, IsNullable = true)]
    public virtual string? Region { get; set; }

    /// <summary>
    /// 存储桶名称（Bucket/Container）
    /// </summary>
    [SugarColumn(ColumnDescription = "存储桶名称", Length = 200, IsNullable = true)]
    public virtual string? BucketName { get; set; }

    /// <summary>
    /// 访问密钥ID
    /// </summary>
    [SugarColumn(ColumnDescription = "访问密钥ID", Length = 200, IsNullable = true)]
    public virtual string? AccessKeyId { get; set; }

    /// <summary>
    /// 访问密钥（敏感字段，传输/显示时需脱敏）
    /// </summary>
    [SugarColumn(ColumnDescription = "访问密钥", Length = 500, IsNullable = true)]
    public virtual string? SecretAccessKey { get; set; }

    /// <summary>
    /// 是否默认存储（同一租户有且仅有一条为true）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否默认存储")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用")]
    public virtual bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序（数字越小越靠前）
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}

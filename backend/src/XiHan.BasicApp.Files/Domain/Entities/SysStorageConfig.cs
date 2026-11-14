// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Files.Domain.Entities;

/// <summary>
/// 存储配置：本地目录或 S3 兼容的对象存储
/// </summary>
/// <remarks>
/// 平台库实体、按 <see cref="OwnerTenantId"/> 分侧（方案 5.12）：租户可以有自己的存储，没有自己的缺省配置时上传落在平台的缺省配置上，平台的配置因此兜底所有租户
/// 每侧至多一条缺省配置、须启用，它就是这一侧的上传目标；已有副本按它们记下的配置读写，所以换缺省或停用一条配置都不影响已存的文件（停用只是不再接收新上传），缺省的不能停用也不能删
/// 还被副本引用的配置不能删，数引用时不分侧：平台的配置也会被租户的副本用到
/// S3、OSS、COS、MinIO 都走 S3 协议；每条 S3 兼容配置对应框架的一个命名存储实例，配置一改注册表按新定义换实例，本地配置共用一个以 <c>BasicApp:Files:LocalRootPath</c> 为根的实例，桶是根下的子目录
/// 访问密钥经 <c>ISecretProtector</c> 加密存储、响应里不回显，修改时留空即保留原值
/// 编码同侧唯一、建好后不改，部分唯一索引让软删除后同编码可以重建；种子只在编码不存在时写平台的 <c>local-default</c>（本地目录），全新部署即可上传，之后由运营维护
/// </remarks>
[SugarTable(TableName = "sys_storage_config", TableDescription = "存储配置：本地目录或 S3 兼容的对象存储")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_storage_config_owner_tenant_id_code")]
public sealed class SysStorageConfig : AppAuditedEntity, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧唯一，不含空白）
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 100, IsNullable = false, ColumnDescription = "编码（同一侧唯一，不含空白）")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 200, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 存储类型
    /// </summary>
    [SugarColumn(ColumnName = "type", IsNullable = false, ColumnDescription = "存储类型")]
    public StorageType Type { get; set; }

    /// <summary>
    /// 服务地址（S3 兼容存储必填）
    /// </summary>
    /// <remarks>
    /// 如 <c>https://oss-cn-hangzhou.aliyuncs.com</c>；本地存储为空
    /// </remarks>
    [SugarColumn(ColumnName = "endpoint", Length = 500, IsNullable = true, ColumnDescription = "服务地址（S3 兼容存储必填）")]
    public string? Endpoint { get; set; }

    /// <summary>
    /// 区域（S3 兼容存储，缺省 <c>us-east-1</c>）
    /// </summary>
    [SugarColumn(ColumnName = "region", Length = 100, IsNullable = true, ColumnDescription = "区域（S3 兼容存储，缺省 us-east-1）")]
    public string? Region { get; set; }

    /// <summary>
    /// 桶名
    /// </summary>
    /// <remarks>
    /// 本地存储是根目录下的子目录。小写字母、数字与连字符，3 到 63 个字符
    /// </remarks>
    [SugarColumn(ColumnName = "bucket", Length = 63, IsNullable = false, ColumnDescription = "桶名")]
    public string Bucket { get; set; } = string.Empty;

    /// <summary>
    /// 访问密钥 ID（S3 兼容存储必填）
    /// </summary>
    [SugarColumn(ColumnName = "access_key_id", Length = 200, IsNullable = true, ColumnDescription = "访问密钥 ID（S3 兼容存储必填）")]
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// 访问密钥（加密存储，S3 兼容存储必填）
    /// </summary>
    [SugarColumn(ColumnName = "secret_access_key", Length = 1000, IsNullable = true, ColumnDescription = "访问密钥（加密存储，S3 兼容存储必填）")]
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// 是否条件写入（S3 兼容存储，缺省开）
    /// </summary>
    /// <remarks>
    /// 开着时建实例前由框架探测服务是否认 <c>If-None-Match: *</c>（框架 ADR-0089），上传以不覆盖写入落盘，服务不认即建不出实例；
    /// 关掉时不探测、上传直接写入，给不认条件写入的服务用（对象键带新文件的 ID，不会重复）。本地存储恒为开
    /// </remarks>
    [SugarColumn(ColumnName = "conditional_writes", IsNullable = false, ColumnDescription = "是否条件写入（S3 兼容存储，缺省开）")]
    public bool ConditionalWrites { get; set; } = true;

    /// <summary>
    /// 是否这一侧的缺省配置（每侧至多一条，须启用）
    /// </summary>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否这一侧的缺省配置（每侧至多一条，须启用）")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public StorageConfigStatus Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 存储类型：本地目录，或经 S3 协议访问的对象存储
/// </summary>
public enum StorageType
{
    /// <summary>
    /// 本地目录
    /// </summary>
    /// <remarks>
    /// 根目录取配置 <c>BasicApp:Files:LocalRootPath</c>
    /// </remarks>
    Local = 0,

    /// <summary>
    /// Amazon S3 或其他 S3 兼容存储（路径式访问）
    /// </summary>
    S3 = 1,

    /// <summary>
    /// 阿里云 OSS（S3 兼容接口，虚拟主机式访问）
    /// </summary>
    Oss = 2,

    /// <summary>
    /// 腾讯云 COS（S3 兼容接口，虚拟主机式访问）
    /// </summary>
    Cos = 3,

    /// <summary>
    /// MinIO（路径式访问）
    /// </summary>
    MinIO = 4,
}

/// <summary>
/// 存储配置状态
/// </summary>
public enum StorageConfigStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：不再接收新上传，已有副本照常读取
    /// </summary>
    Disabled = 1,
}

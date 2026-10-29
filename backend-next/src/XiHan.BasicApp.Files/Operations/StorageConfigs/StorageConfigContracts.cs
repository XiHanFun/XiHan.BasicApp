// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;

namespace XiHan.BasicApp.Files.Operations.StorageConfigs;

/// <summary>
/// 一条存储配置（访问密钥不回显）。
/// </summary>
/// <param name="Id">配置 ID。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">存储类型。</param>
/// <param name="Endpoint">服务地址。</param>
/// <param name="Region">区域。</param>
/// <param name="Bucket">桶名。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="HasSecretAccessKey">是否已设访问密钥。</param>
/// <param name="IsDefault">是否这一侧的缺省配置。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
/// <param name="CreatedAt">创建时间。</param>
/// <param name="ModifiedAt">修改时间。</param>
public sealed record StorageConfigResponse(
    Guid Id,
    string Code,
    string Name,
    StorageType Type,
    string? Endpoint,
    string? Region,
    string Bucket,
    string? AccessKeyId,
    bool HasSecretAccessKey,
    bool IsDefault,
    StorageConfigStatus Status,
    int Order,
    string? Remark,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 由配置投影。
    /// </summary>
    /// <param name="config">配置。</param>
    /// <returns>响应。</returns>
    public static StorageConfigResponse From(SysStorageConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new(config.Id, config.Code, config.Name, config.Type, config.Endpoint, config.Region, config.Bucket, config.AccessKeyId, config.SecretAccessKey is not null, config.IsDefault, config.Status, config.Order, config.Remark, config.CreatedAt, config.ModifiedAt);
    }
}

/// <summary>
/// 针对一条存储配置的请求。
/// </summary>
/// <remarks>
/// 用于 <c>storage-configs.get</c>、<c>storage-configs.set-default</c>。
/// </remarks>
/// <param name="Id">配置 ID。</param>
public sealed record StorageConfigIdRequest([Required] Guid Id);

/// <summary>
/// <c>storage-configs.create</c> 的请求。
/// </summary>
/// <param name="Code">编码（同一侧唯一，不含空白）。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">存储类型。</param>
/// <param name="Bucket">桶名（本地存储是根目录下的子目录）。</param>
/// <param name="Endpoint">服务地址（S3 兼容存储必填）。</param>
/// <param name="Region">区域。</param>
/// <param name="AccessKeyId">访问密钥 ID（S3 兼容存储必填）。</param>
/// <param name="SecretAccessKey">访问密钥（S3 兼容存储必填）。</param>
/// <param name="IsDefault">是否设为这一侧的缺省配置。</param>
/// <param name="Status">状态。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record CreateStorageConfigRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Code,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    StorageType Type,
    [Required, StringLength(63, MinimumLength = 3)] string Bucket,
    [StringLength(500)] string? Endpoint = null,
    [StringLength(100)] string? Region = null,
    [StringLength(200)] string? AccessKeyId = null,
    [StringLength(500)] string? SecretAccessKey = null,
    bool IsDefault = false,
    StorageConfigStatus Status = StorageConfigStatus.Active,
    [Range(0, 100000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成写入内容。
    /// </summary>
    /// <returns>写入内容。</returns>
    public StorageConfigDefinition ToDefinition() => new(Code, Name, Type, Endpoint, Region, Bucket, AccessKeyId, SecretAccessKey, Order, Remark);
}

/// <summary>
/// <c>storage-configs.update</c> 的请求。
/// </summary>
/// <remarks>
/// 编码不改；访问密钥不填即保留原值。
/// </remarks>
/// <param name="Id">配置 ID。</param>
/// <param name="Name">名称。</param>
/// <param name="Type">存储类型。</param>
/// <param name="Bucket">桶名。</param>
/// <param name="Endpoint">服务地址。</param>
/// <param name="Region">区域。</param>
/// <param name="AccessKeyId">访问密钥 ID。</param>
/// <param name="SecretAccessKey">新的访问密钥；不填即保留原值。</param>
/// <param name="Order">排序。</param>
/// <param name="Remark">备注。</param>
public sealed record UpdateStorageConfigRequest(
    [Required] Guid Id,
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    StorageType Type,
    [Required, StringLength(63, MinimumLength = 3)] string Bucket,
    [StringLength(500)] string? Endpoint = null,
    [StringLength(100)] string? Region = null,
    [StringLength(200)] string? AccessKeyId = null,
    [StringLength(500)] string? SecretAccessKey = null,
    [Range(0, 100000)] int Order = 0,
    [StringLength(500)] string? Remark = null)
{
    /// <summary>
    /// 转成写入内容（编码沿用原值）。
    /// </summary>
    /// <param name="code">原编码。</param>
    /// <returns>写入内容。</returns>
    public StorageConfigDefinition ToDefinition(string code) => new(code, Name, Type, Endpoint, Region, Bucket, AccessKeyId, SecretAccessKey, Order, Remark);
}

/// <summary>
/// <c>storage-configs.set-status</c> 的请求。
/// </summary>
/// <param name="Id">配置 ID。</param>
/// <param name="Status">目标状态。</param>
public sealed record SetStorageConfigStatusRequest([Required] Guid Id, StorageConfigStatus Status);

/// <summary>
/// <c>storage-configs.delete</c> 的请求。
/// </summary>
/// <param name="Ids">配置 ID。</param>
public sealed record StorageConfigIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);

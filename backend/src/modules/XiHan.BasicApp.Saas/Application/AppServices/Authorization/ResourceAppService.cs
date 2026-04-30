#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceAppService
// Guid:94e41ec4-2e25-4c69-89c3-34c0150b8fd3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 资源定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "资源定义")]
public sealed class ResourceAppService(
    IResourceRepository resourceRepository,
    IPermissionRepository permissionRepository,
    IFieldLevelSecurityRepository fieldLevelSecurityRepository)
    : SaasApplicationService, IResourceAppService
{
    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository = resourceRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 字段级安全仓储
    /// </summary>
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository = fieldLevelSecurityRepository;

    /// <summary>
    /// 创建资源定义
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Create)]
    public async Task<ResourceDetailDto> CreateResourceAsync(ResourceCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var resourceCode = input.ResourceCode.Trim();
        if (await _resourceRepository.GetByCodeAsync(resourceCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("资源编码已存在。");
        }

        var resource = new SysResource
        {
            ResourceCode = resourceCode,
            ResourceName = input.ResourceName.Trim(),
            ResourceType = input.ResourceType,
            ResourcePath = NormalizeNullable(input.ResourcePath),
            Description = NormalizeNullable(input.Description),
            Metadata = NormalizeMetadata(input.Metadata),
            AccessLevel = input.AccessLevel,
            IsGlobal = false,
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedResource = await _resourceRepository.AddAsync(resource, cancellationToken);
        return ResourceApplicationMapper.ToDetailDto(savedResource);
    }

    /// <summary>
    /// 更新资源定义
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Update)]
    public async Task<ResourceDetailDto> UpdateResourceAsync(ResourceUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var resource = await GetEditableResourceOrThrowAsync(input.BasicId, cancellationToken);
        resource.ResourceName = input.ResourceName.Trim();
        resource.ResourceType = input.ResourceType;
        resource.ResourcePath = NormalizeNullable(input.ResourcePath);
        resource.Description = NormalizeNullable(input.Description);
        resource.Metadata = NormalizeMetadata(input.Metadata);
        resource.AccessLevel = input.AccessLevel;
        resource.Sort = input.Sort;
        resource.Remark = NormalizeNullable(input.Remark);

        var savedResource = await _resourceRepository.UpdateAsync(resource, cancellationToken);
        return ResourceApplicationMapper.ToDetailDto(savedResource);
    }

    /// <summary>
    /// 更新资源定义状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>资源详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Status)]
    public async Task<ResourceDetailDto> UpdateResourceStatusAsync(ResourceStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "资源主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var resource = await GetEditableResourceOrThrowAsync(input.BasicId, cancellationToken);
        resource.Status = input.Status;
        resource.Remark = NormalizeNullable(input.Remark) ?? resource.Remark;

        var savedResource = await _resourceRepository.UpdateAsync(resource, cancellationToken);
        return ResourceApplicationMapper.ToDetailDto(savedResource);
    }

    /// <summary>
    /// 删除资源定义
    /// </summary>
    /// <param name="id">资源主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Delete)]
    public async Task DeleteResourceAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resource = await GetEditableResourceOrThrowAsync(id, cancellationToken);
        await EnsureResourceNotReferencedAsync(resource.BasicId, cancellationToken);

        if (!await _resourceRepository.DeleteAsync(resource, cancellationToken))
        {
            throw new InvalidOperationException("资源定义删除失败。");
        }
    }

    /// <summary>
    /// 获取可维护资源定义
    /// </summary>
    private async Task<SysResource> GetEditableResourceOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "资源主键必须大于 0。");
        }

        var resource = await _resourceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("资源定义不存在。");

        if (resource.IsGlobal)
        {
            throw new InvalidOperationException("平台级全局资源必须通过平台运维流程维护。");
        }

        return resource;
    }

    /// <summary>
    /// 校验资源未被引用
    /// </summary>
    private async Task EnsureResourceNotReferencedAsync(long resourceId, CancellationToken cancellationToken)
    {
        if (await _permissionRepository.AnyAsync(permission => permission.ResourceId == resourceId, cancellationToken))
        {
            throw new InvalidOperationException("资源已被权限定义引用，不能删除。");
        }

        if (await _fieldLevelSecurityRepository.AnyAsync(policy => policy.ResourceId == resourceId, cancellationToken))
        {
            throw new InvalidOperationException("资源已被字段级安全策略引用，不能删除。");
        }
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(ResourceCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ResourceCode);
        ValidateResourceCode(input.ResourceCode);
        ValidateCommonInput(
            input.ResourceName,
            input.ResourceType,
            input.ResourcePath,
            input.Description,
            input.Metadata,
            input.AccessLevel,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(ResourceUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "资源主键必须大于 0。");
        }

        ValidateCommonInput(
            input.ResourceName,
            input.ResourceType,
            input.ResourcePath,
            input.Description,
            input.Metadata,
            input.AccessLevel,
            input.Remark);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
        string resourceName,
        ResourceType resourceType,
        string? resourcePath,
        string? description,
        string? metadata,
        ResourceAccessLevel accessLevel,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ValidateEnum(resourceType, nameof(resourceType));
        ValidateEnum(accessLevel, nameof(accessLevel));
        ValidateLength(resourceName, 100, nameof(resourceName), "资源名称不能超过 100 个字符。");
        ValidateOptionalLength(resourcePath, 500, nameof(resourcePath), "资源路径不能超过 500 个字符。");
        ValidateOptionalLength(description, 500, nameof(description), "资源描述不能超过 500 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        _ = NormalizeMetadata(metadata);
    }

    /// <summary>
    /// 校验资源编码
    /// </summary>
    private static void ValidateResourceCode(string resourceCode)
    {
        var normalizedResourceCode = resourceCode.Trim();
        ValidateLength(normalizedResourceCode, 100, nameof(resourceCode), "资源编码不能超过 100 个字符。");
        if (normalizedResourceCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("资源编码不能包含空白字符。");
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验字符串长度
    /// </summary>
    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 规范化资源元数据
    /// </summary>
    private static string? NormalizeMetadata(string? metadata)
    {
        var normalized = NormalizeNullable(metadata);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var _ = JsonDocument.Parse(normalized);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("资源元数据必须是合法 JSON。", exception);
        }

        return normalized;
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

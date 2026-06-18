#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionCatalogDomainService
// Guid:e62fd8e6-7283-4064-8c60-2b2e9d9ff1e7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限目录领域服务实现
/// </summary>
public sealed class PermissionCatalogDomainService
    : IPermissionCatalogDomainService
{
    private readonly ICurrentTenant _currentTenant;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    private readonly IMenuRepository _menuRepository;

    private readonly IOperationRepository _operationRepository;

    private readonly IPermissionDelegationRepository _permissionDelegationRepository;

    private readonly IPermissionRepository _permissionRepository;

    private readonly IPermissionRequestRepository _permissionRequestRepository;

    private readonly IResourceRepository _resourceRepository;

    private readonly IRolePermissionRepository _rolePermissionRepository;

    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository;

    private readonly IUserPermissionRepository _userPermissionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionCatalogDomainService(
        IPermissionRepository permissionRepository,
        IResourceRepository resourceRepository,
        IOperationRepository operationRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserPermissionRepository userPermissionRepository,
        ITenantEditionPermissionRepository tenantEditionPermissionRepository,
        IMenuRepository menuRepository,
        IPermissionDelegationRepository permissionDelegationRepository,
        IPermissionRequestRepository permissionRequestRepository,
        IFieldLevelSecurityRepository fieldLevelSecurityRepository,
        ICurrentTenant currentTenant)
    {
        _permissionRepository = permissionRepository;
        _resourceRepository = resourceRepository;
        _operationRepository = operationRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userPermissionRepository = userPermissionRepository;
        _tenantEditionPermissionRepository = tenantEditionPermissionRepository;
        _menuRepository = menuRepository;
        _permissionDelegationRepository = permissionDelegationRepository;
        _permissionRequestRepository = permissionRequestRepository;
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<PermissionCatalogCommandResult> CreatePermissionAsync(PermissionCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(command);

        var permissionCode = command.PermissionCode.Trim();
        var moduleCode = ResolveModuleCode(command.ModuleCode, permissionCode);
        if (await _permissionRepository.GetByCodeAsync(permissionCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("权限编码已存在。");
        }

        _ = await LoadAndValidatePermissionTargetAsync(command.PermissionType, command.ResourceId, command.OperationId, cancellationToken);
        if (command.PermissionType == PermissionType.ResourceBased
            && await _permissionRepository.GetByResourceOperationAsync(command.ResourceId!.Value, command.OperationId!.Value, cancellationToken) is not null)
        {
            throw new InvalidOperationException("资源和操作组合已存在权限定义。");
        }

        var permission = new SysPermission
        {
            PermissionType = command.PermissionType,
            ResourceId = command.PermissionType == PermissionType.ResourceBased ? command.ResourceId : null,
            OperationId = command.PermissionType == PermissionType.ResourceBased ? command.OperationId : null,
            ModuleCode = moduleCode,
            PermissionCode = permissionCode,
            PermissionName = command.PermissionName.Trim(),
            PermissionDescription = NormalizeNullable(command.PermissionDescription),
            Tags = NormalizeTags(command.Tags),
            IsRequireAudit = command.IsRequireAudit,
            Priority = command.Priority,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedPermission = await _permissionRepository.AddAsync(permission, cancellationToken);
        return new PermissionCatalogCommandResult(savedPermission.BasicId);
    }

    /// <inheritdoc />
    public async Task DeletePermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permission = await GetEditablePermissionOrThrowAsync(id, cancellationToken);
        await EnsurePermissionNotReferencedAsync(permission.BasicId, cancellationToken);

        if (!await _permissionRepository.DeleteAsync(permission, cancellationToken))
        {
            throw new InvalidOperationException("权限定义删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<PermissionCatalogCommandResult> UpdatePermissionAsync(PermissionUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(command);

        var permission = await GetEditablePermissionOrThrowAsync(command.BasicId, cancellationToken);
        permission.PermissionName = command.PermissionName.Trim();
        permission.PermissionDescription = NormalizeNullable(command.PermissionDescription);
        permission.Tags = NormalizeTags(command.Tags);
        permission.IsRequireAudit = command.IsRequireAudit;
        permission.Priority = command.Priority;
        permission.Sort = command.Sort;
        permission.Remark = NormalizeNullable(command.Remark);

        var savedPermission = await _permissionRepository.UpdateAsync(permission, cancellationToken);
        return new PermissionCatalogCommandResult(savedPermission.BasicId);
    }

    /// <inheritdoc />
    public async Task<PermissionCatalogCommandResult> UpdatePermissionStatusAsync(PermissionStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var permission = await GetEditablePermissionOrThrowAsync(command.BasicId, cancellationToken);
        if (command.Status == EnableStatus.Enabled)
        {
            _ = await LoadAndValidatePermissionTargetAsync(permission.PermissionType, permission.ResourceId, permission.OperationId, cancellationToken);
        }

        permission.Status = command.Status;
        permission.Remark = NormalizeNullable(command.Remark) ?? permission.Remark;

        var savedPermission = await _permissionRepository.UpdateAsync(permission, cancellationToken);
        return new PermissionCatalogCommandResult(savedPermission.BasicId);
    }

    /// <inheritdoc />
    public async Task<ResourceCatalogCommandResult> CreateResourceAsync(ResourceCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateResourceCreateInput(command);

        var resourceCode = command.ResourceCode.Trim();
        if (await _resourceRepository.GetByCodeAsync(resourceCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("资源编码已存在。");
        }

        var resource = new SysResource
        {
            ResourceCode = resourceCode,
            ResourceName = command.ResourceName.Trim(),
            ResourceType = command.ResourceType,
            ResourcePath = NormalizeNullable(command.ResourcePath),
            Description = NormalizeNullable(command.Description),
            Metadata = NormalizeMetadata(command.Metadata),
            AccessLevel = command.AccessLevel,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedResource = await _resourceRepository.AddAsync(resource, cancellationToken);
        return new ResourceCatalogCommandResult(savedResource.BasicId);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<ResourceCatalogCommandResult> UpdateResourceAsync(ResourceUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateResourceUpdateInput(command);

        var resource = await GetEditableResourceOrThrowAsync(command.BasicId, cancellationToken);
        resource.ResourceName = command.ResourceName.Trim();
        resource.ResourceType = command.ResourceType;
        resource.ResourcePath = NormalizeNullable(command.ResourcePath);
        resource.Description = NormalizeNullable(command.Description);
        resource.Metadata = NormalizeMetadata(command.Metadata);
        resource.AccessLevel = command.AccessLevel;
        resource.Sort = command.Sort;
        resource.Remark = NormalizeNullable(command.Remark);

        var savedResource = await _resourceRepository.UpdateAsync(resource, cancellationToken);
        return new ResourceCatalogCommandResult(savedResource.BasicId);
    }

    /// <inheritdoc />
    public async Task<ResourceCatalogCommandResult> UpdateResourceStatusAsync(ResourceStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "资源主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var resource = await GetEditableResourceOrThrowAsync(command.BasicId, cancellationToken);
        resource.Status = command.Status;
        resource.Remark = NormalizeNullable(command.Remark) ?? resource.Remark;

        var savedResource = await _resourceRepository.UpdateAsync(resource, cancellationToken);
        return new ResourceCatalogCommandResult(savedResource.BasicId);
    }

    /// <inheritdoc />
    public async Task<OperationCatalogCommandResult> CreateOperationAsync(OperationCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateOperationCreateInput(command);

        var operationCode = command.OperationCode.Trim();
        if (await _operationRepository.GetByCodeAsync(operationCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("操作编码已存在。");
        }

        var operation = new SysOperation
        {
            OperationCode = operationCode,
            OperationName = command.OperationName.Trim(),
            OperationTypeCode = command.OperationTypeCode,
            Category = command.Category,
            HttpMethod = command.HttpMethod,
            Description = NormalizeNullable(command.Description),
            Icon = NormalizeNullable(command.Icon),
            Color = NormalizeNullable(command.Color),
            IsDangerous = command.IsDangerous,
            IsRequireAudit = command.IsRequireAudit,
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedOperation = await _operationRepository.AddAsync(operation, cancellationToken);
        return new OperationCatalogCommandResult(savedOperation.BasicId);
    }

    /// <inheritdoc />
    public async Task DeleteOperationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var operation = await GetEditableOperationOrThrowAsync(id, cancellationToken);
        await EnsureOperationNotReferencedAsync(operation.BasicId, cancellationToken);

        if (!await _operationRepository.DeleteAsync(operation, cancellationToken))
        {
            throw new InvalidOperationException("操作定义删除失败。");
        }
    }

    /// <inheritdoc />
    public async Task<OperationCatalogCommandResult> UpdateOperationAsync(OperationUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateOperationUpdateInput(command);

        var operation = await GetEditableOperationOrThrowAsync(command.BasicId, cancellationToken);
        operation.OperationName = command.OperationName.Trim();
        operation.OperationTypeCode = command.OperationTypeCode;
        operation.Category = command.Category;
        operation.HttpMethod = command.HttpMethod;
        operation.Description = NormalizeNullable(command.Description);
        operation.Icon = NormalizeNullable(command.Icon);
        operation.Color = NormalizeNullable(command.Color);
        operation.IsDangerous = command.IsDangerous;
        operation.IsRequireAudit = command.IsRequireAudit;
        operation.Sort = command.Sort;
        operation.Remark = NormalizeNullable(command.Remark);

        var savedOperation = await _operationRepository.UpdateAsync(operation, cancellationToken);
        return new OperationCatalogCommandResult(savedOperation.BasicId);
    }

    /// <inheritdoc />
    public async Task<OperationCatalogCommandResult> UpdateOperationStatusAsync(OperationStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "操作主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var operation = await GetEditableOperationOrThrowAsync(command.BasicId, cancellationToken);
        operation.Status = command.Status;
        operation.Remark = NormalizeNullable(command.Remark) ?? operation.Remark;

        var savedOperation = await _operationRepository.UpdateAsync(operation, cancellationToken);
        return new OperationCatalogCommandResult(savedOperation.BasicId);
    }

    private static bool IsValidCodeSegmentChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code is '-' or '_' or '.';
    }

    private static bool IsValidOperationCodeChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code is '-' or '_';
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

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

    private static string? NormalizeTags(string? tags)
    {
        var normalized = NormalizeNullable(tags);
        if (normalized is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(normalized);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException("权限标签必须是 JSON 数组。");
            }
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("权限标签必须是合法 JSON 数组。", exception);
        }

        return normalized;
    }

    private static string ResolveModuleCode(string? moduleCode, string permissionCode)
    {
        var normalizedPermissionCode = permissionCode.Trim();
        var firstSeparatorIndex = normalizedPermissionCode.IndexOf(':', StringComparison.Ordinal);
        if (firstSeparatorIndex <= 0)
        {
            throw new InvalidOperationException("权限编码必须使用 module:resource:action 格式。");
        }

        var codeModule = normalizedPermissionCode[..firstSeparatorIndex];
        ValidateCodeSegment(codeModule, nameof(permissionCode), "权限编码模块段只能包含小写英文、数字、连字符、下划线或点。");

        var normalizedModuleCode = NormalizeNullable(moduleCode);
        if (normalizedModuleCode is null)
        {
            return codeModule;
        }

        ValidateLength(normalizedModuleCode, 50, nameof(moduleCode), "模块编码不能超过 50 个字符。");
        ValidateCodeSegment(normalizedModuleCode, nameof(moduleCode), "模块编码只能包含小写英文、数字、连字符、下划线或点。");
        if (!string.Equals(normalizedModuleCode, codeModule, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("模块编码必须与权限编码第一段一致。");
        }

        return normalizedModuleCode;
    }

    private static void ValidateCodeSegment(string segment, string paramName, string message)
    {
        if (segment.Any(static code => !IsValidCodeSegmentChar(code)))
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateCommonInput(string permissionName, string? permissionDescription, string? tags, string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);
        ValidateLength(permissionName, 200, nameof(permissionName), "权限名称不能超过 200 个字符。");
        ValidateOptionalLength(permissionDescription, 500, nameof(permissionDescription), "权限描述不能超过 500 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        _ = NormalizeTags(tags);
    }

    private static void ValidateCreateInput(PermissionCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.PermissionCode);
        ValidateEnum(command.PermissionType, nameof(command.PermissionType));
        ValidatePermissionCode(command.PermissionCode);
        _ = ResolveModuleCode(command.ModuleCode, command.PermissionCode);
        ValidatePermissionTargetInput(command.PermissionType, command.ResourceId, command.OperationId);
        ValidateCommonInput(command.PermissionName, command.PermissionDescription, command.Tags, command.Remark);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateOperationCode(string operationCode)
    {
        var normalizedOperationCode = operationCode.Trim();
        ValidateLength(normalizedOperationCode, 50, nameof(operationCode), "操作编码不能超过 50 个字符。");
        if (normalizedOperationCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("操作编码不能包含空白字符。");
        }

        if (normalizedOperationCode.Any(static code => !IsValidOperationCodeChar(code)))
        {
            throw new InvalidOperationException("操作编码只能包含小写英文、数字、连字符或下划线。");
        }
    }

    private static void ValidateOperationCommonInput(
        string operationName,
        OperationTypeCode operationTypeCode,
        OperationCategory category,
        HttpMethodType? httpMethod,
        string? description,
        string? icon,
        string? color,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        ValidateEnum(operationTypeCode, nameof(operationTypeCode));
        ValidateEnum(category, nameof(category));
        if (httpMethod.HasValue)
        {
            ValidateEnum(httpMethod.Value, nameof(httpMethod));
        }

        ValidateLength(operationName, 100, nameof(operationName), "操作名称不能超过 100 个字符。");
        ValidateOptionalLength(description, 500, nameof(description), "操作描述不能超过 500 个字符。");
        ValidateOptionalLength(icon, 100, nameof(icon), "操作图标不能超过 100 个字符。");
        ValidateOptionalLength(color, 20, nameof(color), "操作颜色不能超过 20 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
    }

    private static void ValidateOperationCreateInput(OperationCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.OperationCode);
        ValidateOperationCode(command.OperationCode);
        ValidateOperationCommonInput(command.OperationName, command.OperationTypeCode, command.Category, command.HttpMethod, command.Description, command.Icon, command.Color, command.Remark);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateOperationUpdateInput(OperationUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "操作主键必须大于 0。");
        }

        ValidateOperationCommonInput(command.OperationName, command.OperationTypeCode, command.Category, command.HttpMethod, command.Description, command.Icon, command.Color, command.Remark);
    }

    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidatePermissionCode(string permissionCode)
    {
        var normalizedPermissionCode = permissionCode.Trim();
        ValidateLength(normalizedPermissionCode, 200, nameof(permissionCode), "权限编码不能超过 200 个字符。");
        if (normalizedPermissionCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("权限编码不能包含空白字符。");
        }

        var segments = normalizedPermissionCode.Split(':');
        if (segments.Length < 3 || segments.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("权限编码必须使用 module:resource:action 格式。");
        }

        foreach (var segment in segments)
        {
            ValidateCodeSegment(segment, nameof(permissionCode), "权限编码只能包含小写英文、数字、冒号、连字符、下划线或点。");
        }
    }

    private static void ValidatePermissionTargetInput(PermissionType permissionType, long? resourceId, long? operationId)
    {
        if (permissionType == PermissionType.ResourceBased)
        {
            if (!resourceId.HasValue || resourceId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resourceId), "资源操作权限必须绑定有效资源。");
            }

            if (!operationId.HasValue || operationId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(operationId), "资源操作权限必须绑定有效操作。");
            }

            return;
        }

        if (resourceId.HasValue || operationId.HasValue)
        {
            throw new InvalidOperationException("功能权限和数据范围权限不能绑定资源或操作。");
        }
    }

    private static void ValidateResourceCode(string resourceCode)
    {
        var normalizedResourceCode = resourceCode.Trim();
        ValidateLength(normalizedResourceCode, 100, nameof(resourceCode), "资源编码不能超过 100 个字符。");
        if (normalizedResourceCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("资源编码不能包含空白字符。");
        }
    }

    private static void ValidateResourceCommonInput(
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

    private static void ValidateResourceCreateInput(ResourceCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ResourceCode);
        ValidateResourceCode(command.ResourceCode);
        ValidateResourceCommonInput(command.ResourceName, command.ResourceType, command.ResourcePath, command.Description, command.Metadata, command.AccessLevel, command.Remark);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateResourceUpdateInput(ResourceUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "资源主键必须大于 0。");
        }

        ValidateResourceCommonInput(command.ResourceName, command.ResourceType, command.ResourcePath, command.Description, command.Metadata, command.AccessLevel, command.Remark);
    }

    private static void ValidateUpdateInput(PermissionUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限主键必须大于 0。");
        }

        ValidateCommonInput(command.PermissionName, command.PermissionDescription, command.Tags, command.Remark);
    }

    private async Task EnsureOperationNotReferencedAsync(long operationId, CancellationToken cancellationToken)
    {
        if (await _permissionRepository.AnyAsync(permission => permission.OperationId == operationId, cancellationToken))
        {
            throw new InvalidOperationException("操作已被权限定义引用，不能删除。");
        }
    }

    private async Task EnsurePermissionNotReferencedAsync(long permissionId, CancellationToken cancellationToken)
    {
        if (await _rolePermissionRepository.AnyAsync(binding => binding.PermissionId == permissionId, cancellationToken))
        {
            throw new InvalidOperationException("权限已被角色授权引用，不能删除。");
        }

        if (await _userPermissionRepository.AnyAsync(binding => binding.PermissionId == permissionId, cancellationToken))
        {
            throw new InvalidOperationException("权限已被用户直授权引用，不能删除。");
        }

        if (await _tenantEditionPermissionRepository.AnyAsync(binding => binding.PermissionId == permissionId, cancellationToken))
        {
            throw new InvalidOperationException("权限已被租户版本引用，不能删除。");
        }

        if (await _menuRepository.AnyAsync(menu => menu.PermissionId == permissionId, cancellationToken))
        {
            throw new InvalidOperationException("权限已被菜单引用，不能删除。");
        }

        if (await _permissionDelegationRepository.AnyAsync(delegation => delegation.PermissionId == permissionId, cancellationToken))
        {
            throw new InvalidOperationException("权限已被权限委托引用，不能删除。");
        }

        if (await _permissionRequestRepository.AnyAsync(request => request.PermissionId == permissionId, cancellationToken))
        {
            throw new InvalidOperationException("权限已被权限申请引用，不能删除。");
        }

        if (await _fieldLevelSecurityRepository.AnyAsync(policy => policy.TargetType == FieldSecurityTargetType.Permission && policy.TargetId == permissionId, cancellationToken))
        {
            throw new InvalidOperationException("权限已被字段级安全策略引用，不能删除。");
        }
    }

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

    private async Task<SysOperation> GetEditableOperationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "操作主键必须大于 0。");
        }

        var operation = await _operationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");

        if (operation.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台级全局操作仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return operation;
    }

    private async Task<SysPermission> GetEditablePermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限主键必须大于 0。");
        }

        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");

        if (permission.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台级全局权限仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return permission;
    }

    private async Task<SysResource> GetEditableResourceOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "资源主键必须大于 0。");
        }

        var resource = await _resourceRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("资源定义不存在。");

        if (resource.IsGlobal && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台级全局资源仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return resource;
    }

    private async Task<(SysResource? Resource, SysOperation? Operation)> LoadAndValidatePermissionTargetAsync(
        PermissionType permissionType,
        long? resourceId,
        long? operationId,
        CancellationToken cancellationToken)
    {
        ValidatePermissionTargetInput(permissionType, resourceId, operationId);
        if (permissionType != PermissionType.ResourceBased)
        {
            return (null, null);
        }

        var resource = await _resourceRepository.GetByIdAsync(resourceId!.Value, cancellationToken)
            ?? throw new InvalidOperationException("资源定义不存在。");
        if (resource.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("资源定义未启用。");
        }

        var operation = await _operationRepository.GetByIdAsync(operationId!.Value, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");
        if (operation.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("操作定义未启用。");
        }

        return (resource, operation);
    }
}

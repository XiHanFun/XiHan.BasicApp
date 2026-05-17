#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionAppService
// Guid:c71b9028-19a3-4c87-9ad7-2f1211906dcc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限定义")]
public sealed class PermissionAppService
    : SaasApplicationService, IPermissionAppService
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionAppService(
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
        IPermissionConditionRepository permissionConditionRepository,
        IRoleRepository roleRepository,
        ITenantUserRepository tenantUserRepository,
        IReviewRepository reviewRepository,
        ICurrentUser currentUser)
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
        _permissionConditionRepository = permissionConditionRepository;
        _roleRepository = roleRepository;
        _tenantUserRepository = tenantUserRepository;
        _reviewRepository = reviewRepository;
        _currentUser = currentUser;
    }

    private const int MaxConditionGroups = 5;
    private const int MaxConditionsPerGroup = 10;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository;

    /// <summary>
    /// 操作仓储
    /// </summary>
    private readonly IOperationRepository _operationRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository;

    /// <summary>
    /// 用户权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository;

    /// <summary>
    /// 租户版本权限仓储
    /// </summary>
    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository;

    /// <summary>
    /// 菜单仓储
    /// </summary>
    private readonly IMenuRepository _menuRepository;

    /// <summary>
    /// 权限委托仓储
    /// </summary>
    private readonly IPermissionDelegationRepository _permissionDelegationRepository;

    /// <summary>
    /// 权限申请仓储
    /// </summary>
    private readonly IPermissionRequestRepository _permissionRequestRepository;

    /// <summary>
    /// 字段级安全仓储
    /// </summary>
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    /// <summary>
    /// 权限 ABAC 条件仓储
    /// </summary>
    private readonly IPermissionConditionRepository _permissionConditionRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 审批仓储
    /// </summary>
    private readonly IReviewRepository _reviewRepository;

    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 创建权限定义
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Create)]
    public async Task<PermissionDetailDto> CreatePermissionAsync(PermissionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var permissionCode = input.PermissionCode.Trim();
        var moduleCode = ResolveModuleCode(input.ModuleCode, permissionCode);
        if (await _permissionRepository.GetByCodeAsync(permissionCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("权限编码已存在。");
        }

        var (resource, operation) = await LoadAndValidatePermissionTargetAsync(
            input.PermissionType,
            input.ResourceId,
            input.OperationId,
            cancellationToken);
        if (input.PermissionType == PermissionType.ResourceBased
            && await _permissionRepository.GetByResourceOperationAsync(input.ResourceId!.Value, input.OperationId!.Value, cancellationToken) is not null)
        {
            throw new InvalidOperationException("资源和操作组合已存在权限定义。");
        }

        var permission = new SysPermission
        {
            PermissionType = input.PermissionType,
            ResourceId = input.PermissionType == PermissionType.ResourceBased ? input.ResourceId : null,
            OperationId = input.PermissionType == PermissionType.ResourceBased ? input.OperationId : null,
            ModuleCode = moduleCode,
            PermissionCode = permissionCode,
            PermissionName = input.PermissionName.Trim(),
            PermissionDescription = NormalizeNullable(input.PermissionDescription),
            Tags = NormalizeTags(input.Tags),
            IsRequireAudit = input.IsRequireAudit,
            IsGlobal = false,
            Priority = input.Priority,
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedPermission = await _permissionRepository.AddAsync(permission, cancellationToken);
        return PermissionApplicationMapper.ToDetailDto(savedPermission, resource, operation);
    }

    /// <summary>
    /// 更新权限定义
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Update)]
    public async Task<PermissionDetailDto> UpdatePermissionAsync(PermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var permission = await GetEditablePermissionOrThrowAsync(input.BasicId, cancellationToken);
        permission.PermissionName = input.PermissionName.Trim();
        permission.PermissionDescription = NormalizeNullable(input.PermissionDescription);
        permission.Tags = NormalizeTags(input.Tags);
        permission.IsRequireAudit = input.IsRequireAudit;
        permission.Priority = input.Priority;
        permission.Sort = input.Sort;
        permission.Remark = NormalizeNullable(input.Remark);

        var savedPermission = await _permissionRepository.UpdateAsync(permission, cancellationToken);
        return await ToDetailDtoAsync(savedPermission, cancellationToken);
    }

    /// <summary>
    /// 更新权限定义状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Status)]
    public async Task<PermissionDetailDto> UpdatePermissionStatusAsync(PermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var permission = await GetEditablePermissionOrThrowAsync(input.BasicId, cancellationToken);
        if (input.Status == EnableStatus.Enabled)
        {
            _ = await LoadAndValidatePermissionTargetAsync(permission.PermissionType, permission.ResourceId, permission.OperationId, cancellationToken);
        }

        permission.Status = input.Status;
        permission.Remark = NormalizeNullable(input.Remark) ?? permission.Remark;

        var savedPermission = await _permissionRepository.UpdateAsync(permission, cancellationToken);
        return await ToDetailDtoAsync(savedPermission, cancellationToken);
    }

    /// <summary>
    /// 删除权限定义
    /// </summary>
    /// <param name="id">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Permission.Delete)]
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

    /// <summary>
    /// 获取可维护权限定义
    /// </summary>
    private async Task<SysPermission> GetEditablePermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限主键必须大于 0。");
        }

        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限定义不存在。");

        if (permission.IsGlobal)
        {
            throw new InvalidOperationException("平台级全局权限必须通过平台运维流程维护。");
        }

        return permission;
    }

    /// <summary>
    /// 校验权限未被授权事实引用
    /// </summary>
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

    /// <summary>
    /// 加载并校验权限目标
    /// </summary>
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

    /// <summary>
    /// 转换权限详情
    /// </summary>
    private async Task<PermissionDetailDto> ToDetailDtoAsync(SysPermission permission, CancellationToken cancellationToken)
    {
        var resource = permission.ResourceId.HasValue
            ? await _resourceRepository.GetByIdAsync(permission.ResourceId.Value, cancellationToken)
            : null;
        var operation = permission.OperationId.HasValue
            ? await _operationRepository.GetByIdAsync(permission.OperationId.Value, cancellationToken)
            : null;

        return PermissionApplicationMapper.ToDetailDto(permission, resource, operation);
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(PermissionCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.PermissionCode);
        ValidateEnum(input.PermissionType, nameof(input.PermissionType));
        ValidatePermissionCode(input.PermissionCode);
        _ = ResolveModuleCode(input.ModuleCode, input.PermissionCode);
        ValidatePermissionTargetInput(input.PermissionType, input.ResourceId, input.OperationId);
        ValidateCommonInput(
            input.PermissionName,
            input.PermissionDescription,
            input.Tags,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(PermissionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限主键必须大于 0。");
        }

        ValidateCommonInput(
            input.PermissionName,
            input.PermissionDescription,
            input.Tags,
            input.Remark);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
        string permissionName,
        string? permissionDescription,
        string? tags,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionName);
        ValidateLength(permissionName, 200, nameof(permissionName), "权限名称不能超过 200 个字符。");
        ValidateOptionalLength(permissionDescription, 500, nameof(permissionDescription), "权限描述不能超过 500 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
        _ = NormalizeTags(tags);
    }

    /// <summary>
    /// 校验权限目标
    /// </summary>
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

    /// <summary>
    /// 解析模块编码
    /// </summary>
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

    /// <summary>
    /// 校验权限编码
    /// </summary>
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

    /// <summary>
    /// 校验编码片段
    /// </summary>
    private static void ValidateCodeSegment(string segment, string paramName, string message)
    {
        if (segment.Any(static code => !IsValidCodeSegmentChar(code)))
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 判断编码片段字符是否合法
    /// </summary>
    private static bool IsValidCodeSegmentChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code is '-' or '_' or '.';
    }

    /// <summary>
    /// 规范化权限标签
    /// </summary>
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

    #region Resource

    /// <summary>
    /// 创建资源定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Create)]
    public async Task<ResourceDetailDto> CreateResourceAsync(ResourceCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateResourceCreateInput(input);

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
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Resource.Update)]
    public async Task<ResourceDetailDto> UpdateResourceAsync(ResourceUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateResourceUpdateInput(input);

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
    /// 校验资源创建输入
    /// </summary>
    private static void ValidateResourceCreateInput(ResourceCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.ResourceCode);
        ValidateResourceCode(input.ResourceCode);
        ValidateResourceCommonInput(
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
    /// 校验资源更新输入
    /// </summary>
    private static void ValidateResourceUpdateInput(ResourceUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "资源主键必须大于 0。");
        }

        ValidateResourceCommonInput(
            input.ResourceName,
            input.ResourceType,
            input.ResourcePath,
            input.Description,
            input.Metadata,
            input.AccessLevel,
            input.Remark);
    }

    /// <summary>
    /// 校验资源通用输入
    /// </summary>
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

    #endregion Resource

    #region Operation

    /// <summary>
    /// 创建操作定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Create)]
    public async Task<OperationDetailDto> CreateOperationAsync(OperationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateOperationCreateInput(input);

        var operationCode = input.OperationCode.Trim();
        if (await _operationRepository.GetByCodeAsync(operationCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("操作编码已存在。");
        }

        var operation = new SysOperation
        {
            OperationCode = operationCode,
            OperationName = input.OperationName.Trim(),
            OperationTypeCode = input.OperationTypeCode,
            Category = input.Category,
            HttpMethod = input.HttpMethod,
            Description = NormalizeNullable(input.Description),
            Icon = NormalizeNullable(input.Icon),
            Color = NormalizeNullable(input.Color),
            IsDangerous = input.IsDangerous,
            IsRequireAudit = input.IsRequireAudit,
            IsGlobal = false,
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedOperation = await _operationRepository.AddAsync(operation, cancellationToken);
        return OperationApplicationMapper.ToDetailDto(savedOperation);
    }

    /// <summary>
    /// 更新操作定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Update)]
    public async Task<OperationDetailDto> UpdateOperationAsync(OperationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateOperationUpdateInput(input);

        var operation = await GetEditableOperationOrThrowAsync(input.BasicId, cancellationToken);
        operation.OperationName = input.OperationName.Trim();
        operation.OperationTypeCode = input.OperationTypeCode;
        operation.Category = input.Category;
        operation.HttpMethod = input.HttpMethod;
        operation.Description = NormalizeNullable(input.Description);
        operation.Icon = NormalizeNullable(input.Icon);
        operation.Color = NormalizeNullable(input.Color);
        operation.IsDangerous = input.IsDangerous;
        operation.IsRequireAudit = input.IsRequireAudit;
        operation.Sort = input.Sort;
        operation.Remark = NormalizeNullable(input.Remark);

        var savedOperation = await _operationRepository.UpdateAsync(operation, cancellationToken);
        return OperationApplicationMapper.ToDetailDto(savedOperation);
    }

    /// <summary>
    /// 更新操作定义状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Status)]
    public async Task<OperationDetailDto> UpdateOperationStatusAsync(OperationStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "操作主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var operation = await GetEditableOperationOrThrowAsync(input.BasicId, cancellationToken);
        operation.Status = input.Status;
        operation.Remark = NormalizeNullable(input.Remark) ?? operation.Remark;

        var savedOperation = await _operationRepository.UpdateAsync(operation, cancellationToken);
        return OperationApplicationMapper.ToDetailDto(savedOperation);
    }

    /// <summary>
    /// 删除操作定义
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Operation.Delete)]
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

    /// <summary>
    /// 获取可维护操作定义
    /// </summary>
    private async Task<SysOperation> GetEditableOperationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "操作主键必须大于 0。");
        }

        var operation = await _operationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("操作定义不存在。");

        if (operation.IsGlobal)
        {
            throw new InvalidOperationException("平台级全局操作必须通过平台运维流程维护。");
        }

        return operation;
    }

    /// <summary>
    /// 校验操作未被引用
    /// </summary>
    private async Task EnsureOperationNotReferencedAsync(long operationId, CancellationToken cancellationToken)
    {
        if (await _permissionRepository.AnyAsync(permission => permission.OperationId == operationId, cancellationToken))
        {
            throw new InvalidOperationException("操作已被权限定义引用，不能删除。");
        }
    }

    /// <summary>
    /// 校验操作创建输入
    /// </summary>
    private static void ValidateOperationCreateInput(OperationCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.OperationCode);
        ValidateOperationCode(input.OperationCode);
        ValidateOperationCommonInput(
            input.OperationName,
            input.OperationTypeCode,
            input.Category,
            input.HttpMethod,
            input.Description,
            input.Icon,
            input.Color,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验操作更新输入
    /// </summary>
    private static void ValidateOperationUpdateInput(OperationUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "操作主键必须大于 0。");
        }

        ValidateOperationCommonInput(
            input.OperationName,
            input.OperationTypeCode,
            input.Category,
            input.HttpMethod,
            input.Description,
            input.Icon,
            input.Color,
            input.Remark);
    }

    /// <summary>
    /// 校验操作通用输入
    /// </summary>
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

    /// <summary>
    /// 校验操作编码
    /// </summary>
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

    /// <summary>
    /// 判断操作编码字符是否合法
    /// </summary>
    private static bool IsValidOperationCodeChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= '0' and <= '9'
            || code is '-' or '_';
    }

    #endregion Operation

    #region PermissionCondition

    /// <summary>
    /// 创建权限 ABAC 条件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Create)]
    public async Task<PermissionConditionDetailDto> CreatePermissionConditionAsync(PermissionConditionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateConditionCreateInput(input);

        var now = DateTimeOffset.UtcNow;
        await EnsureAuthorizationBindingUsableAsync(input.RolePermissionId, input.UserPermissionId, now, cancellationToken);
        await EnsureConditionLimitsAsync(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName.Trim(),
            input.ValueType,
            null,
            cancellationToken);

        var condition = new SysPermissionCondition
        {
            RolePermissionId = input.RolePermissionId,
            UserPermissionId = input.UserPermissionId,
            ConditionGroup = input.ConditionGroup,
            AttributeName = input.AttributeName.Trim(),
            Operator = input.Operator,
            IsNegated = input.IsNegated,
            ValueType = input.ValueType,
            ConditionValue = input.ConditionValue.Trim(),
            Description = NormalizeNullable(input.Description),
            Status = input.Status,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedCondition = await _permissionConditionRepository.AddAsync(condition, cancellationToken);
        return await BuildConditionDetailDtoAsync(savedCondition, cancellationToken);
    }

    /// <summary>
    /// 更新权限 ABAC 条件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Update)]
    public async Task<PermissionConditionDetailDto> UpdatePermissionConditionAsync(PermissionConditionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateConditionUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var condition = await GetPermissionConditionOrThrowAsync(input.BasicId, cancellationToken);
        await EnsureAuthorizationBindingUsableAsync(input.RolePermissionId, input.UserPermissionId, now, cancellationToken);
        await EnsureConditionLimitsAsync(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName.Trim(),
            input.ValueType,
            condition.BasicId,
            cancellationToken);

        condition.RolePermissionId = input.RolePermissionId;
        condition.UserPermissionId = input.UserPermissionId;
        condition.ConditionGroup = input.ConditionGroup;
        condition.AttributeName = input.AttributeName.Trim();
        condition.Operator = input.Operator;
        condition.IsNegated = input.IsNegated;
        condition.ValueType = input.ValueType;
        condition.ConditionValue = input.ConditionValue.Trim();
        condition.Description = NormalizeNullable(input.Description);
        condition.Remark = NormalizeNullable(input.Remark);

        var savedCondition = await _permissionConditionRepository.UpdateAsync(condition, cancellationToken);
        return await BuildConditionDetailDtoAsync(savedCondition, cancellationToken);
    }

    /// <summary>
    /// 更新权限 ABAC 条件状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Status)]
    public async Task<PermissionConditionDetailDto> UpdatePermissionConditionStatusAsync(PermissionConditionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限 ABAC 条件主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var condition = await GetPermissionConditionOrThrowAsync(input.BasicId, cancellationToken);
        if (input.Status == ValidityStatus.Valid)
        {
            await EnsureAuthorizationBindingUsableAsync(condition.RolePermissionId, condition.UserPermissionId, DateTimeOffset.UtcNow, cancellationToken);
            await EnsureConditionLimitsAsync(
                condition.RolePermissionId,
                condition.UserPermissionId,
                condition.ConditionGroup,
                condition.AttributeName,
                condition.ValueType,
                condition.BasicId,
                cancellationToken);
        }

        condition.Status = input.Status;
        condition.Remark = NormalizeNullable(input.Remark) ?? condition.Remark;

        var savedCondition = await _permissionConditionRepository.UpdateAsync(condition, cancellationToken);
        return await BuildConditionDetailDtoAsync(savedCondition, cancellationToken);
    }

    /// <summary>
    /// 删除权限 ABAC 条件
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionCondition.Delete)]
    public async Task DeletePermissionConditionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var condition = await GetPermissionConditionOrThrowAsync(id, cancellationToken);
        if (!await _permissionConditionRepository.DeleteAsync(condition, cancellationToken))
        {
            throw new InvalidOperationException("权限 ABAC 条件删除失败。");
        }
    }

    /// <summary>
    /// 获取权限 ABAC 条件，不存在时抛出异常
    /// </summary>
    private async Task<SysPermissionCondition> GetPermissionConditionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限 ABAC 条件主键必须大于 0。");
        }

        return await _permissionConditionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限 ABAC 条件不存在。");
    }

    /// <summary>
    /// 校验授权绑定可用
    /// </summary>
    private async Task EnsureAuthorizationBindingUsableAsync(
        long? rolePermissionId,
        long? userPermissionId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ValidateBindingIds(rolePermissionId, userPermissionId);

        if (rolePermissionId.HasValue)
        {
            var rolePermission = await _rolePermissionRepository.GetByIdAsync(rolePermissionId.Value, cancellationToken)
                ?? throw new InvalidOperationException("角色权限绑定不存在。");

            EnsureValidity(rolePermission.Status, rolePermission.EffectiveTime, rolePermission.ExpirationTime, now, "无效角色权限绑定不能配置 ABAC 条件。");

            var role = await _roleRepository.GetByIdAsync(rolePermission.RoleId, cancellationToken)
                ?? throw new InvalidOperationException("角色不存在。");
            if (role.Status != EnableStatus.Enabled)
            {
                throw new InvalidOperationException("停用角色不能配置 ABAC 条件。");
            }

            var permission = await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken)
                ?? throw new InvalidOperationException("权限不存在。");
            if (permission.Status != EnableStatus.Enabled)
            {
                throw new InvalidOperationException("停用权限不能配置 ABAC 条件。");
            }
        }

        if (userPermissionId.HasValue)
        {
            var userPermission = await _userPermissionRepository.GetByIdAsync(userPermissionId.Value, cancellationToken)
                ?? throw new InvalidOperationException("用户直授权限绑定不存在。");

            EnsureValidity(userPermission.Status, userPermission.EffectiveTime, userPermission.ExpirationTime, now, "无效用户直授权限绑定不能配置 ABAC 条件。");

            _ = await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, cancellationToken);
            var permission = await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken)
                ?? throw new InvalidOperationException("权限不存在。");
            if (permission.Status != EnableStatus.Enabled)
            {
                throw new InvalidOperationException("停用权限不能配置 ABAC 条件。");
            }
        }
    }

    /// <summary>
    /// 校验授权有效性
    /// </summary>
    private static void EnsureValidity(
        ValidityStatus status,
        DateTimeOffset? effectiveTime,
        DateTimeOffset? expirationTime,
        DateTimeOffset now,
        string message)
    {
        if (status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException(message);
        }

        if (effectiveTime.HasValue && effectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效授权绑定不能配置 ABAC 条件。");
        }

        if (expirationTime.HasValue && expirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期授权绑定不能配置 ABAC 条件。");
        }
    }

    /// <summary>
    /// 获取可授权租户成员（ABAC 条件场景）
    /// </summary>
    private async Task<SysTenantUser> GetAssignableTenantMemberOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能配置 ABAC 条件。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能配置 ABAC 条件。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员 ABAC 条件必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能配置 ABAC 条件。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能配置 ABAC 条件。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 校验 ABAC 条件数量和值类型一致性
    /// </summary>
    private async Task EnsureConditionLimitsAsync(
        long? rolePermissionId,
        long? userPermissionId,
        int conditionGroup,
        string attributeName,
        ConfigDataType valueType,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        ValidateBindingIds(rolePermissionId, userPermissionId);

        var existingConditions = await GetConditionsByBindingAsync(rolePermissionId, userPermissionId, cancellationToken);
        var comparableConditions = existingConditions
            .Where(condition => !excludeId.HasValue || condition.BasicId != excludeId.Value)
            .ToArray();

        var groupCount = comparableConditions
            .Select(condition => condition.ConditionGroup)
            .Append(conditionGroup)
            .Distinct()
            .Count();
        if (groupCount > MaxConditionGroups)
        {
            throw new InvalidOperationException($"单条授权绑定最多允许 {MaxConditionGroups} 个 ABAC 条件组。");
        }

        var groupItemCount = comparableConditions.Count(condition => condition.ConditionGroup == conditionGroup) + 1;
        if (groupItemCount > MaxConditionsPerGroup)
        {
            throw new InvalidOperationException($"单个 ABAC 条件组最多允许 {MaxConditionsPerGroup} 条条件。");
        }

        var hasValueTypeConflict = comparableConditions.Any(condition =>
            string.Equals(condition.AttributeName.Trim(), attributeName, StringComparison.OrdinalIgnoreCase) &&
            condition.ValueType != valueType);
        if (hasValueTypeConflict)
        {
            throw new InvalidOperationException("同一授权绑定内相同 ABAC 属性必须使用一致的值类型。");
        }
    }

    /// <summary>
    /// 获取授权绑定下的条件集合
    /// </summary>
    private async Task<IReadOnlyList<SysPermissionCondition>> GetConditionsByBindingAsync(
        long? rolePermissionId,
        long? userPermissionId,
        CancellationToken cancellationToken)
    {
        if (rolePermissionId.HasValue)
        {
            return await _permissionConditionRepository.GetListAsync(
                condition => condition.RolePermissionId == rolePermissionId.Value,
                condition => condition.ConditionGroup,
                cancellationToken);
        }

        return await _permissionConditionRepository.GetListAsync(
            condition => condition.UserPermissionId == userPermissionId!.Value,
            condition => condition.ConditionGroup,
            cancellationToken);
    }

    /// <summary>
    /// 构建 ABAC 条件详情 DTO
    /// </summary>
    private async Task<PermissionConditionDetailDto> BuildConditionDetailDtoAsync(SysPermissionCondition condition, CancellationToken cancellationToken)
    {
        SysRolePermission? rolePermission = null;
        SysUserPermission? userPermission = null;
        SysPermission? permission = null;
        SysRole? role = null;
        SysTenantUser? tenantMember = null;

        if (condition.RolePermissionId.HasValue)
        {
            rolePermission = await _rolePermissionRepository.GetByIdAsync(condition.RolePermissionId.Value, cancellationToken);
            if (rolePermission is not null)
            {
                role = await _roleRepository.GetByIdAsync(rolePermission.RoleId, cancellationToken);
                permission = await _permissionRepository.GetByIdAsync(rolePermission.PermissionId, cancellationToken);
            }
        }

        if (condition.UserPermissionId.HasValue)
        {
            userPermission = await _userPermissionRepository.GetByIdAsync(condition.UserPermissionId.Value, cancellationToken);
            if (userPermission is not null)
            {
                tenantMember = await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);
                permission ??= await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);
            }
        }

        return PermissionConditionApplicationMapper.ToDetailDto(condition, rolePermission, userPermission, permission, role, tenantMember);
    }

    /// <summary>
    /// 校验 ABAC 条件创建输入
    /// </summary>
    private static void ValidateConditionCreateInput(PermissionConditionCreateDto input)
    {
        ValidateConditionCommonInput(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName,
            input.Operator,
            input.ValueType,
            input.ConditionValue,
            input.Description,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验 ABAC 条件更新输入
    /// </summary>
    private static void ValidateConditionUpdateInput(PermissionConditionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限 ABAC 条件主键必须大于 0。");
        }

        ValidateConditionCommonInput(
            input.RolePermissionId,
            input.UserPermissionId,
            input.ConditionGroup,
            input.AttributeName,
            input.Operator,
            input.ValueType,
            input.ConditionValue,
            input.Description,
            input.Remark);
    }

    /// <summary>
    /// 校验 ABAC 条件通用输入
    /// </summary>
    private static void ValidateConditionCommonInput(
        long? rolePermissionId,
        long? userPermissionId,
        int conditionGroup,
        string attributeName,
        ConditionOperator conditionOperator,
        ConfigDataType valueType,
        string conditionValue,
        string? description,
        string? remark)
    {
        ValidateBindingIds(rolePermissionId, userPermissionId);
        ValidateEnum(conditionOperator, nameof(conditionOperator));
        ValidateEnum(valueType, nameof(valueType));
        ArgumentException.ThrowIfNullOrWhiteSpace(attributeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(conditionValue);
        ValidateLength(attributeName, 200, nameof(attributeName), "属性名称不能超过 200 个字符。");
        ValidateOptionalLength(description, 500, nameof(description), "条件说明不能超过 500 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");

        if (conditionGroup < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(conditionGroup), "条件分组不能小于 0。");
        }

        if (!HasKnownAttributePrefix(attributeName.Trim()))
        {
            throw new InvalidOperationException("属性名称必须使用 subject./resource./environment. 命名空间前缀。");
        }
    }

    /// <summary>
    /// 校验绑定主键
    /// </summary>
    private static void ValidateBindingIds(long? rolePermissionId, long? userPermissionId)
    {
        if (rolePermissionId.HasValue == userPermissionId.HasValue)
        {
            throw new InvalidOperationException("ABAC 条件必须且只能绑定到角色权限或用户直授权限中的一种。");
        }

        if (rolePermissionId.HasValue && rolePermissionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rolePermissionId), "角色权限绑定主键必须大于 0。");
        }

        if (userPermissionId.HasValue && userPermissionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userPermissionId), "用户直授权限绑定主键必须大于 0。");
        }
    }

    /// <summary>
    /// 判断属性命名空间是否有效
    /// </summary>
    private static bool HasKnownAttributePrefix(string attributeName)
    {
        return attributeName.StartsWith("subject.", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("resource.", StringComparison.OrdinalIgnoreCase) ||
            attributeName.StartsWith("environment.", StringComparison.OrdinalIgnoreCase);
    }

    #endregion PermissionCondition

    #region PermissionDelegation

    /// <summary>
    /// 创建权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Create)]
    public async Task<PermissionDelegationDetailDto> CreatePermissionDelegationAsync(PermissionDelegationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateDelegationCreateInput(input, now);

        var delegator = await GetAvailableTenantMemberWithSubjectOrThrowAsync(input.DelegatorUserId, now, "委托人", cancellationToken);
        var delegatee = await GetAvailableTenantMemberWithSubjectOrThrowAsync(input.DelegateeUserId, now, "被委托人", cancellationToken);
        var permission = await GetDelegablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetDelegableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsureDelegationNotExistsAsync(input.DelegatorUserId, input.DelegateeUserId, input.PermissionId, null, cancellationToken);

        var delegation = new SysPermissionDelegation
        {
            DelegatorUserId = input.DelegatorUserId,
            DelegateeUserId = input.DelegateeUserId,
            PermissionId = input.PermissionId,
            RoleId = input.RoleId,
            DelegationStatus = ResolveWritableStatus(input.EffectiveTime, now),
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            DelegationReason = NormalizeNullable(input.DelegationReason),
            Remark = NormalizeNullable(input.Remark)
        };

        var savedDelegation = await _permissionDelegationRepository.AddAsync(delegation, cancellationToken);
        return PermissionDelegationApplicationMapper.ToDetailDto(savedDelegation, delegator, delegatee, permission, role, now);
    }

    /// <summary>
    /// 更新权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Update)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationAsync(PermissionDelegationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateDelegationUpdateInput(input, now);

        var delegation = await GetPermissionDelegationOrThrowAsync(input.BasicId, cancellationToken);
        if (delegation.DelegationStatus == DelegationStatus.Revoked)
        {
            throw new InvalidOperationException("已撤销权限委托不能更新。");
        }

        var delegator = await GetAvailableTenantMemberWithSubjectOrThrowAsync(input.DelegatorUserId, now, "委托人", cancellationToken);
        var delegatee = await GetAvailableTenantMemberWithSubjectOrThrowAsync(input.DelegateeUserId, now, "被委托人", cancellationToken);
        var permission = await GetDelegablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetDelegableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsureDelegationNotExistsAsync(input.DelegatorUserId, input.DelegateeUserId, input.PermissionId, delegation.BasicId, cancellationToken);

        delegation.DelegatorUserId = input.DelegatorUserId;
        delegation.DelegateeUserId = input.DelegateeUserId;
        delegation.PermissionId = input.PermissionId;
        delegation.RoleId = input.RoleId;
        delegation.DelegationStatus = ResolveWritableStatus(input.EffectiveTime, now);
        delegation.EffectiveTime = input.EffectiveTime;
        delegation.ExpirationTime = input.ExpirationTime;
        delegation.DelegationReason = NormalizeNullable(input.DelegationReason);
        delegation.Remark = NormalizeNullable(input.Remark);

        var savedDelegation = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
        return PermissionDelegationApplicationMapper.ToDetailDto(savedDelegation, delegator, delegatee, permission, role, now);
    }

    /// <summary>
    /// 更新权限委托状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Status)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationStatusAsync(PermissionDelegationStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限委托主键必须大于 0。");
        }

        ValidateEnum(input.DelegationStatus, nameof(input.DelegationStatus));

        var now = DateTimeOffset.UtcNow;
        var delegation = await GetPermissionDelegationOrThrowAsync(input.BasicId, cancellationToken);
        if (delegation.DelegationStatus == DelegationStatus.Revoked && input.DelegationStatus != DelegationStatus.Revoked)
        {
            throw new InvalidOperationException("已撤销权限委托不能重新生效。");
        }

        ValidateStatusMatchesPeriod(input.DelegationStatus, delegation.EffectiveTime, delegation.ExpirationTime, now);

        SysTenantUser? delegator;
        SysTenantUser? delegatee;
        SysPermission? permission;
        SysRole? role;
        if (input.DelegationStatus is DelegationStatus.Pending or DelegationStatus.Active)
        {
            delegator = await GetAvailableTenantMemberWithSubjectOrThrowAsync(delegation.DelegatorUserId, now, "委托人", cancellationToken);
            delegatee = await GetAvailableTenantMemberWithSubjectOrThrowAsync(delegation.DelegateeUserId, now, "被委托人", cancellationToken);
            permission = await GetDelegablePermissionOrDefaultAsync(delegation.PermissionId, cancellationToken);
            role = await GetDelegableRoleOrDefaultAsync(delegation.RoleId, cancellationToken);
        }
        else
        {
            delegator = await _tenantUserRepository.GetMembershipAsync(delegation.DelegatorUserId, cancellationToken);
            delegatee = await _tenantUserRepository.GetMembershipAsync(delegation.DelegateeUserId, cancellationToken);
            permission = await GetPermissionOrDefaultAsync(delegation.PermissionId, cancellationToken);
            role = await GetRoleOrDefaultAsync(delegation.RoleId, cancellationToken);
        }

        delegation.DelegationStatus = input.DelegationStatus;
        delegation.Remark = NormalizeNullable(input.Remark);

        var savedDelegation = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
        return PermissionDelegationApplicationMapper.ToDetailDto(savedDelegation, delegator, delegatee, permission, role, now);
    }

    /// <summary>
    /// 撤销权限委托
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Revoke)]
    public async Task DeletePermissionDelegationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var delegation = await GetPermissionDelegationOrThrowAsync(id, cancellationToken);
        delegation.DelegationStatus = DelegationStatus.Revoked;

        _ = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
    }

    /// <summary>
    /// 获取权限委托，不存在时抛出异常
    /// </summary>
    private async Task<SysPermissionDelegation> GetPermissionDelegationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限委托主键必须大于 0。");
        }

        return await _permissionDelegationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    /// <summary>
    /// 获取可参与委托的租户成员（带主体名称）
    /// </summary>
    private async Task<SysTenantUser> GetAvailableTenantMemberWithSubjectOrThrowAsync(long userId, DateTimeOffset now, string subjectName, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"{subjectName}不是当前租户成员。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException($"未接受邀请的{subjectName}不能参与权限委托。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException($"无效{subjectName}不能参与权限委托。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员权限委托必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException($"未生效{subjectName}不能参与权限委托。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException($"已过期{subjectName}不能参与权限委托。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 获取可委托权限
    /// </summary>
    private async Task<SysPermission?> GetDelegablePermissionOrDefaultAsync(long? permissionId, CancellationToken cancellationToken)
    {
        if (!permissionId.HasValue)
        {
            return null;
        }

        var permission = await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能参与权限委托。");
        }

        return permission;
    }

    /// <summary>
    /// 获取可委托角色
    /// </summary>
    private async Task<SysRole?> GetDelegableRoleOrDefaultAsync(long? roleId, CancellationToken cancellationToken)
    {
        if (!roleId.HasValue)
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(roleId.Value, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能参与权限委托。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色权限委托必须通过平台运维流程维护。");
        }

        return role;
    }

    /// <summary>
    /// 校验权限委托不存在
    /// </summary>
    private async Task EnsureDelegationNotExistsAsync(
        long delegatorUserId,
        long delegateeUserId,
        long? permissionId,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _permissionDelegationRepository.AnyAsync(
                delegation => delegation.DelegatorUserId == delegatorUserId
                    && delegation.DelegateeUserId == delegateeUserId
                    && delegation.PermissionId == permissionId
                    && delegation.BasicId != excludeId.Value,
                cancellationToken)
            : await _permissionDelegationRepository.AnyAsync(
                delegation => delegation.DelegatorUserId == delegatorUserId
                    && delegation.DelegateeUserId == delegateeUserId
                    && delegation.PermissionId == permissionId,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("权限委托已存在。");
        }
    }

    /// <summary>
    /// 校验委托创建输入
    /// </summary>
    private static void ValidateDelegationCreateInput(PermissionDelegationCreateDto input, DateTimeOffset now)
    {
        ValidateDelegationCommonInput(
            input.DelegatorUserId,
            input.DelegateeUserId,
            input.PermissionId,
            input.RoleId,
            input.EffectiveTime,
            input.ExpirationTime,
            now);
    }

    /// <summary>
    /// 校验委托更新输入
    /// </summary>
    private static void ValidateDelegationUpdateInput(PermissionDelegationUpdateDto input, DateTimeOffset now)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限委托主键必须大于 0。");
        }

        ValidateDelegationCommonInput(
            input.DelegatorUserId,
            input.DelegateeUserId,
            input.PermissionId,
            input.RoleId,
            input.EffectiveTime,
            input.ExpirationTime,
            now);
    }

    /// <summary>
    /// 校验委托通用输入
    /// </summary>
    private static void ValidateDelegationCommonInput(
        long delegatorUserId,
        long delegateeUserId,
        long? permissionId,
        long? roleId,
        DateTimeOffset? effectiveTime,
        DateTimeOffset expirationTime,
        DateTimeOffset now)
    {
        if (delegatorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delegatorUserId), "委托人用户主键必须大于 0。");
        }

        if (delegateeUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delegateeUserId), "被委托人用户主键必须大于 0。");
        }

        if (delegatorUserId == delegateeUserId)
        {
            throw new InvalidOperationException("委托人和被委托人不能相同。");
        }

        ValidateOptionalId(permissionId, nameof(permissionId), "权限主键必须大于 0。");
        ValidateOptionalId(roleId, nameof(roleId), "角色主键必须大于 0。");
        ValidateWritablePeriod(effectiveTime, expirationTime, now);
    }

    /// <summary>
    /// 校验可写有效期
    /// </summary>
    private static void ValidateWritablePeriod(DateTimeOffset? effectiveTime, DateTimeOffset expirationTime, DateTimeOffset now)
    {
        if (expirationTime == default)
        {
            throw new ArgumentOutOfRangeException(nameof(expirationTime), "权限委托失效时间不能为空。");
        }

        if (expirationTime <= now)
        {
            throw new InvalidOperationException("权限委托失效时间必须晚于当前时间。");
        }

        if (effectiveTime.HasValue && expirationTime <= effectiveTime.Value)
        {
            throw new InvalidOperationException("权限委托失效时间必须晚于生效时间。");
        }
    }

    /// <summary>
    /// 校验状态与有效期一致
    /// </summary>
    private static void ValidateStatusMatchesPeriod(DelegationStatus status, DateTimeOffset? effectiveTime, DateTimeOffset expirationTime, DateTimeOffset now)
    {
        if (status == DelegationStatus.Pending && (!effectiveTime.HasValue || effectiveTime.Value <= now))
        {
            throw new InvalidOperationException("待生效权限委托必须存在晚于当前时间的生效时间。");
        }

        if (status == DelegationStatus.Active && ((effectiveTime.HasValue && effectiveTime.Value > now) || expirationTime <= now))
        {
            throw new InvalidOperationException("生效中权限委托必须处于当前有效期内。");
        }

        if (status == DelegationStatus.Expired && expirationTime > now)
        {
            throw new InvalidOperationException("未到失效时间的权限委托不能标记为已过期。");
        }
    }

    /// <summary>
    /// 解析可写状态
    /// </summary>
    private static DelegationStatus ResolveWritableStatus(DateTimeOffset? effectiveTime, DateTimeOffset now)
    {
        return effectiveTime.HasValue && effectiveTime.Value > now
            ? DelegationStatus.Pending
            : DelegationStatus.Active;
    }

    #endregion PermissionDelegation

    #region PermissionRequest

    /// <summary>
    /// 创建权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Create)]
    public async Task<PermissionRequestDetailDto> CreatePermissionRequestAsync(PermissionRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateRequestCreateInput(input, now);

        var requestUserId = GetCurrentUserIdOrThrow();
        var requestUser = await GetAvailableTenantMemberForRequestOrThrowAsync(requestUserId, now, cancellationToken);
        var permission = await GetRequestablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetRequestableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsurePendingRequestNotExistsAsync(requestUserId, input.PermissionId, input.RoleId, null, cancellationToken);

        var permissionRequest = new SysPermissionRequest
        {
            RequestUserId = requestUserId,
            PermissionId = input.PermissionId,
            RoleId = input.RoleId,
            RequestReason = input.RequestReason.Trim(),
            ExpectedEffectiveTime = input.ExpectedEffectiveTime,
            ExpectedExpirationTime = input.ExpectedExpirationTime,
            RequestStatus = PermissionRequestStatus.Pending,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedRequest = await _permissionRequestRepository.AddAsync(permissionRequest, cancellationToken);
        return PermissionRequestApplicationMapper.ToDetailDto(savedRequest, requestUser, permission, role, null, now);
    }

    /// <summary>
    /// 更新权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Update)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestAsync(PermissionRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateRequestUpdateInput(input, now);

        var requestUserId = GetCurrentUserIdOrThrow();
        var permissionRequest = await GetPermissionRequestOrThrowAsync(input.BasicId, cancellationToken);
        EnsurePendingOwnerRequest(permissionRequest, requestUserId);

        var requestUser = await GetAvailableTenantMemberForRequestOrThrowAsync(requestUserId, now, cancellationToken);
        var permission = await GetRequestablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetRequestableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsurePendingRequestNotExistsAsync(requestUserId, input.PermissionId, input.RoleId, permissionRequest.BasicId, cancellationToken);

        permissionRequest.PermissionId = input.PermissionId;
        permissionRequest.RoleId = input.RoleId;
        permissionRequest.RequestReason = input.RequestReason.Trim();
        permissionRequest.ExpectedEffectiveTime = input.ExpectedEffectiveTime;
        permissionRequest.ExpectedExpirationTime = input.ExpectedExpirationTime;
        permissionRequest.Remark = NormalizeNullable(input.Remark);

        var savedRequest = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
        return PermissionRequestApplicationMapper.ToDetailDto(savedRequest, requestUser, permission, role, null, now);
    }

    /// <summary>
    /// 更新权限申请状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Status)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestStatusAsync(PermissionRequestStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限申请主键必须大于 0。");
        }

        ValidateEnum(input.RequestStatus, nameof(input.RequestStatus));
        ValidateOptionalId(input.ReviewId, nameof(input.ReviewId), "审批单主键必须大于 0。");

        _ = await GetAvailableTenantMemberForRequestOrThrowAsync(GetCurrentUserIdOrThrow(), DateTimeOffset.UtcNow, cancellationToken);
        var permissionRequest = await GetPermissionRequestOrThrowAsync(input.BasicId, cancellationToken);
        EnsureStatusCanBeChanged(permissionRequest, input.RequestStatus);

        var review = input.ReviewId.HasValue
            ? await GetEnabledReviewOrThrowAsync(input.ReviewId.Value, cancellationToken)
            : await GetReviewOrDefaultAsync(permissionRequest.ReviewId, cancellationToken);

        permissionRequest.RequestStatus = input.RequestStatus;
        permissionRequest.ReviewId = input.ReviewId ?? permissionRequest.ReviewId;
        permissionRequest.Remark = NormalizeNullable(input.Remark);

        var requestUser = await _tenantUserRepository.GetMembershipAsync(permissionRequest.RequestUserId, cancellationToken);
        var permission = await GetPermissionOrDefaultAsync(permissionRequest.PermissionId, cancellationToken);
        var role = await GetRoleOrDefaultAsync(permissionRequest.RoleId, cancellationToken);
        var savedRequest = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
        return PermissionRequestApplicationMapper.ToDetailDto(savedRequest, requestUser, permission, role, review, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 撤回权限申请
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Withdraw)]
    public async Task DeletePermissionRequestAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        var permissionRequest = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        EnsurePendingOwnerRequest(permissionRequest, requestUserId);

        permissionRequest.RequestStatus = PermissionRequestStatus.Withdrawn;
        _ = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
    }

    /// <summary>
    /// 获取权限申请，不存在时抛出异常
    /// </summary>
    private async Task<SysPermissionRequest> GetPermissionRequestOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限申请主键必须大于 0。");
        }

        return await _permissionRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 获取当前用户主键
    /// </summary>
    private long GetCurrentUserIdOrThrow()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("当前用户未登录。");
        }

        return _currentUser.UserId.Value;
    }

    /// <summary>
    /// 获取可提交申请的租户成员
    /// </summary>
    private async Task<SysTenantUser> GetAvailableTenantMemberForRequestOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能提交权限申请。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能提交权限申请。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员权限申请必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能提交权限申请。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能提交权限申请。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 获取可申请权限
    /// </summary>
    private async Task<SysPermission?> GetRequestablePermissionOrDefaultAsync(long? permissionId, CancellationToken cancellationToken)
    {
        if (!permissionId.HasValue)
        {
            return null;
        }

        var permission = await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能申请。");
        }

        return permission;
    }

    /// <summary>
    /// 获取可申请角色
    /// </summary>
    private async Task<SysRole?> GetRequestableRoleOrDefaultAsync(long? roleId, CancellationToken cancellationToken)
    {
        if (!roleId.HasValue)
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(roleId.Value, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能申请。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色权限申请必须通过平台运维流程维护。");
        }

        return role;
    }

    /// <summary>
    /// 获取启用审批单
    /// </summary>
    private async Task<SysReview> GetEnabledReviewOrThrowAsync(long reviewId, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId, cancellationToken)
            ?? throw new InvalidOperationException("审批单不存在。");

        if (review.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用审批单不能关联权限申请。");
        }

        return review;
    }

    /// <summary>
    /// 按需获取审批单
    /// </summary>
    private async Task<SysReview?> GetReviewOrDefaultAsync(long? reviewId, CancellationToken cancellationToken)
    {
        return reviewId.HasValue
            ? await _reviewRepository.GetByIdAsync(reviewId.Value, cancellationToken)
            : null;
    }

    /// <summary>
    /// 校验待审批申请不存在
    /// </summary>
    private async Task EnsurePendingRequestNotExistsAsync(
        long requestUserId,
        long? permissionId,
        long? roleId,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _permissionRequestRepository.AnyAsync(
                request => request.RequestUserId == requestUserId
                    && request.PermissionId == permissionId
                    && request.RoleId == roleId
                    && request.RequestStatus == PermissionRequestStatus.Pending
                    && request.BasicId != excludeId.Value,
                cancellationToken)
            : await _permissionRequestRepository.AnyAsync(
                request => request.RequestUserId == requestUserId
                    && request.PermissionId == permissionId
                    && request.RoleId == roleId
                    && request.RequestStatus == PermissionRequestStatus.Pending,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("相同权限或角色的待审批申请已存在。");
        }
    }

    /// <summary>
    /// 校验待审批本人申请
    /// </summary>
    private static void EnsurePendingOwnerRequest(SysPermissionRequest permissionRequest, long currentUserId)
    {
        if (permissionRequest.RequestUserId != currentUserId)
        {
            throw new InvalidOperationException("只能维护自己的权限申请。");
        }

        if (permissionRequest.RequestStatus != PermissionRequestStatus.Pending)
        {
            throw new InvalidOperationException("只有待审批权限申请可以维护。");
        }
    }

    /// <summary>
    /// 校验状态变更
    /// </summary>
    private static void EnsureStatusCanBeChanged(SysPermissionRequest permissionRequest, PermissionRequestStatus nextStatus)
    {
        if (nextStatus == PermissionRequestStatus.Approved)
        {
            throw new InvalidOperationException("权限申请审批通过必须走审批流并自动授权，不能直接更新为已批准。");
        }

        if (permissionRequest.RequestStatus != PermissionRequestStatus.Pending && permissionRequest.RequestStatus != nextStatus)
        {
            throw new InvalidOperationException("已完结权限申请不能变更状态。");
        }
    }

    /// <summary>
    /// 校验申请创建输入
    /// </summary>
    private static void ValidateRequestCreateInput(PermissionRequestCreateDto input, DateTimeOffset now)
    {
        ValidateRequestCommonInput(input.PermissionId, input.RoleId, input.RequestReason, input.ExpectedEffectiveTime, input.ExpectedExpirationTime, now);
    }

    /// <summary>
    /// 校验申请更新输入
    /// </summary>
    private static void ValidateRequestUpdateInput(PermissionRequestUpdateDto input, DateTimeOffset now)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限申请主键必须大于 0。");
        }

        ValidateRequestCommonInput(input.PermissionId, input.RoleId, input.RequestReason, input.ExpectedEffectiveTime, input.ExpectedExpirationTime, now);
    }

    /// <summary>
    /// 校验申请通用输入
    /// </summary>
    private static void ValidateRequestCommonInput(
        long? permissionId,
        long? roleId,
        string requestReason,
        DateTimeOffset? expectedEffectiveTime,
        DateTimeOffset? expectedExpirationTime,
        DateTimeOffset now)
    {
        if (!permissionId.HasValue && !roleId.HasValue)
        {
            throw new InvalidOperationException("权限申请必须指定权限或角色。");
        }

        ValidateOptionalId(permissionId, nameof(permissionId), "权限主键必须大于 0。");
        ValidateOptionalId(roleId, nameof(roleId), "角色主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(requestReason);

        if (requestReason.Trim().Length > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(requestReason), "申请原因不能超过 1000 个字符。");
        }

        if (expectedExpirationTime.HasValue && expectedExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("期望失效时间必须晚于当前时间。");
        }

        if (expectedEffectiveTime.HasValue && expectedExpirationTime.HasValue && expectedExpirationTime.Value <= expectedEffectiveTime.Value)
        {
            throw new InvalidOperationException("期望失效时间必须晚于期望生效时间。");
        }
    }

    #endregion PermissionRequest

    #region Shared Helpers

    /// <summary>
    /// 按需获取权限
    /// </summary>
    private async Task<SysPermission?> GetPermissionOrDefaultAsync(long? permissionId, CancellationToken cancellationToken)
    {
        return permissionId.HasValue
            ? await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken)
            : null;
    }

    /// <summary>
    /// 按需获取角色
    /// </summary>
    private async Task<SysRole?> GetRoleOrDefaultAsync(long? roleId, CancellationToken cancellationToken)
    {
        return roleId.HasValue
            ? await _roleRepository.GetByIdAsync(roleId.Value, cancellationToken)
            : null;
    }

    /// <summary>
    /// 校验可选主键
    /// </summary>
    private static void ValidateOptionalId(long? id, string paramName, string message)
    {
        if (id.HasValue && id.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    #endregion Shared Helpers

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
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

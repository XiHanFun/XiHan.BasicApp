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

using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限定义命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限定义")]
public sealed class PermissionAppService(
    IPermissionRepository permissionRepository,
    IResourceRepository resourceRepository,
    IOperationRepository operationRepository,
    IRolePermissionRepository rolePermissionRepository,
    IUserPermissionRepository userPermissionRepository,
    ITenantEditionPermissionRepository tenantEditionPermissionRepository,
    IMenuRepository menuRepository,
    IPermissionDelegationRepository permissionDelegationRepository,
    IPermissionRequestRepository permissionRequestRepository,
    IFieldLevelSecurityRepository fieldLevelSecurityRepository)
    : SaasApplicationService, IPermissionAppService
{
    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository = resourceRepository;

    /// <summary>
    /// 操作仓储
    /// </summary>
    private readonly IOperationRepository _operationRepository = operationRepository;

    /// <summary>
    /// 角色权限仓储
    /// </summary>
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;

    /// <summary>
    /// 用户权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;

    /// <summary>
    /// 租户版本权限仓储
    /// </summary>
    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository = tenantEditionPermissionRepository;

    /// <summary>
    /// 菜单仓储
    /// </summary>
    private readonly IMenuRepository _menuRepository = menuRepository;

    /// <summary>
    /// 权限委托仓储
    /// </summary>
    private readonly IPermissionDelegationRepository _permissionDelegationRepository = permissionDelegationRepository;

    /// <summary>
    /// 权限申请仓储
    /// </summary>
    private readonly IPermissionRequestRepository _permissionRequestRepository = permissionRequestRepository;

    /// <summary>
    /// 字段级安全仓储
    /// </summary>
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository = fieldLevelSecurityRepository;

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

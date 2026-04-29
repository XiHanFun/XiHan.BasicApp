#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionPermissionAppService
// Guid:70731b46-3a4d-4ec9-b4d6-40edc2cc473b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
/// 租户版本权限命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户版本权限")]
public sealed class TenantEditionPermissionAppService(
    ITenantEditionRepository tenantEditionRepository,
    ITenantEditionPermissionRepository tenantEditionPermissionRepository,
    IPermissionRepository permissionRepository)
    : SaasApplicationService, ITenantEditionPermissionAppService
{
    /// <summary>
    /// 租户版本仓储
    /// </summary>
    private readonly ITenantEditionRepository _tenantEditionRepository = tenantEditionRepository;

    /// <summary>
    /// 租户版本权限仓储
    /// </summary>
    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository = tenantEditionPermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 授予租户版本权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Grant)]
    public async Task<TenantEditionPermissionDetailDto> CreateTenantEditionPermissionAsync(TenantEditionPermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantInput(input);

        _ = await _tenantEditionRepository.GetByIdAsync(input.EditionId, cancellationToken)
            ?? throw new InvalidOperationException("租户版本不存在。");

        var permission = await GetGrantablePermissionOrThrowAsync(input.PermissionId, cancellationToken);
        if (await _tenantEditionPermissionRepository.AnyAsync(
            editionPermission => editionPermission.EditionId == input.EditionId && editionPermission.PermissionId == input.PermissionId,
            cancellationToken))
        {
            throw new InvalidOperationException("租户版本权限已绑定。");
        }

        var editionPermission = new SysTenantEditionPermission
        {
            EditionId = input.EditionId,
            PermissionId = input.PermissionId,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedEditionPermission = await _tenantEditionPermissionRepository.AddAsync(editionPermission, cancellationToken);
        return TenantEditionPermissionApplicationMapper.ToDetailDto(savedEditionPermission, permission);
    }

    /// <summary>
    /// 更新租户版本权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Update)]
    public async Task<TenantEditionPermissionDetailDto> UpdateTenantEditionPermissionStatusAsync(TenantEditionPermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户版本权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var editionPermission = await GetTenantEditionPermissionOrThrowAsync(input.BasicId, cancellationToken);
        var permission = input.Status == ValidityStatus.Valid
            ? await GetGrantablePermissionOrThrowAsync(editionPermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(editionPermission.PermissionId, cancellationToken);

        editionPermission.Status = input.Status;
        editionPermission.Remark = NormalizeNullable(input.Remark);

        var savedEditionPermission = await _tenantEditionPermissionRepository.UpdateAsync(editionPermission, cancellationToken);
        return TenantEditionPermissionApplicationMapper.ToDetailDto(savedEditionPermission, permission);
    }

    /// <summary>
    /// 撤销租户版本权限
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Revoke)]
    public async Task DeleteTenantEditionPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var editionPermission = await GetTenantEditionPermissionOrThrowAsync(id, cancellationToken);
        if (!await _tenantEditionPermissionRepository.DeleteAsync(editionPermission, cancellationToken))
        {
            throw new InvalidOperationException("租户版本权限撤销失败。");
        }
    }

    /// <summary>
    /// 获取租户版本权限绑定，不存在时抛出异常
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限绑定实体</returns>
    private async Task<SysTenantEditionPermission> GetTenantEditionPermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户版本权限绑定主键必须大于 0。");
        }

        return await _tenantEditionPermissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("租户版本权限绑定不存在。");
    }

    /// <summary>
    /// 获取可绑定权限，不满足版本门控规则时抛出异常
    /// </summary>
    /// <param name="permissionId">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限实体</returns>
    private async Task<SysPermission> GetGrantablePermissionOrThrowAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (!permission.IsGlobal)
        {
            throw new InvalidOperationException("租户版本只能绑定平台级全局权限。");
        }

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能绑定到租户版本。");
        }

        return permission;
    }

    /// <summary>
    /// 校验授权参数
    /// </summary>
    /// <param name="input">授权参数</param>
    private static void ValidateGrantInput(TenantEditionPermissionGrantDto input)
    {
        if (input.EditionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "租户版本主键必须大于 0。");
        }

        if (input.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限主键必须大于 0。");
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="paramName">参数名</param>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>规范化后的字符串</returns>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

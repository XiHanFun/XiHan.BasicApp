#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDataScopeAppService
// Guid:21069089-9094-4262-8573-223eb82e29a2
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
/// 角色数据范围命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色数据范围")]
public sealed class RoleDataScopeAppService(
    IRoleRepository roleRepository,
    IRoleDataScopeRepository roleDataScopeRepository,
    IDepartmentRepository departmentRepository)
    : SaasApplicationService, IRoleDataScopeAppService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 角色数据范围仓储
    /// </summary>
    private readonly IRoleDataScopeRepository _roleDataScopeRepository = roleDataScopeRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 授予角色数据范围
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Grant)]
    public async Task<RoleDataScopeDetailDto> CreateRoleDataScopeAsync(RoleDataScopeGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantInput(input);

        _ = await GetCustomDataScopeRoleOrThrowAsync(input.RoleId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(input.DepartmentId, cancellationToken);
        if (await _roleDataScopeRepository.AnyAsync(
            scope => scope.RoleId == input.RoleId && scope.DepartmentId == input.DepartmentId,
            cancellationToken))
        {
            throw new InvalidOperationException("角色数据范围已绑定。");
        }

        var dataScope = new SysRoleDataScope
        {
            RoleId = input.RoleId,
            DepartmentId = input.DepartmentId,
            IncludeChildren = input.IncludeChildren,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedDataScope = await _roleDataScopeRepository.AddAsync(dataScope, cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(savedDataScope, department);
    }

    /// <summary>
    /// 更新角色数据范围
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Update)]
    public async Task<RoleDataScopeDetailDto> UpdateRoleDataScopeAsync(RoleDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var dataScope = await GetRoleDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        _ = await GetCustomDataScopeRoleOrThrowAsync(dataScope.RoleId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken);

        dataScope.IncludeChildren = input.IncludeChildren;
        dataScope.EffectiveTime = input.EffectiveTime;
        dataScope.ExpirationTime = input.ExpirationTime;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(savedDataScope, department);
    }

    /// <summary>
    /// 更新角色数据范围状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Status)]
    public async Task<RoleDataScopeDetailDto> UpdateRoleDataScopeStatusAsync(RoleDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var dataScope = await GetRoleDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        var department = input.Status == ValidityStatus.Valid
            ? await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken)
            : await _departmentRepository.GetByIdAsync(dataScope.DepartmentId, cancellationToken);

        if (input.Status == ValidityStatus.Valid)
        {
            _ = await GetCustomDataScopeRoleOrThrowAsync(dataScope.RoleId, cancellationToken);
        }

        dataScope.Status = input.Status;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(savedDataScope, department);
    }

    /// <summary>
    /// 撤销角色数据范围
    /// </summary>
    /// <param name="id">角色数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Revoke)]
    public async Task DeleteRoleDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await GetRoleDataScopeOrThrowAsync(id, cancellationToken);
        dataScope.Status = ValidityStatus.Invalid;

        _ = await _roleDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
    }

    /// <summary>
    /// 获取角色数据范围绑定，不存在时抛出异常
    /// </summary>
    /// <param name="id">角色数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围实体</returns>
    private async Task<SysRoleDataScope> GetRoleDataScopeOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色数据范围绑定主键必须大于 0。");
        }

        return await _roleDataScopeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("角色数据范围绑定不存在。");
    }

    /// <summary>
    /// 获取可维护自定义数据范围角色，不满足规则时抛出异常
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体</returns>
    private async Task<SysRole> GetCustomDataScopeRoleOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能维护数据范围。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色必须通过平台运维流程维护。");
        }

        if (role.DataScope != DataPermissionScope.Custom)
        {
            throw new InvalidOperationException("只有自定义数据权限范围角色才能维护数据范围。");
        }

        return role;
    }

    /// <summary>
    /// 获取已启用部门，不满足规则时抛出异常
    /// </summary>
    /// <param name="departmentId">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门实体</returns>
    private async Task<SysDepartment> GetEnabledDepartmentOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能绑定到角色数据范围。");
        }

        return department;
    }

    /// <summary>
    /// 校验授权参数
    /// </summary>
    /// <param name="input">授权参数</param>
    private static void ValidateGrantInput(RoleDataScopeGrantDto input)
    {
        if (input.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色主键必须大于 0。");
        }

        if (input.DepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "部门主键必须大于 0。");
        }

        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(RoleDataScopeUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色数据范围绑定主键必须大于 0。");
        }

        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验有效期
    /// </summary>
    /// <param name="effectiveTime">生效时间</param>
    /// <param name="expirationTime">失效时间</param>
    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("角色数据范围失效时间必须晚于生效时间。");
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

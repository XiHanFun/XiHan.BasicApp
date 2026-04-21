#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentAppService
// Guid:70baa84a-073e-4f73-84bb-643c6d522be0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.InternalServices;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Saas.Application.AppServices.Implementations;

/// <summary>
/// 部门应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统Saas服务")]
[Authorize]
[PermissionAuthorize("department:read")]
public class DepartmentAppService
    : CrudApplicationServiceBase<SysDepartment, DepartmentDto, long, DepartmentCreateDto, DepartmentUpdateDto, BasicAppPRDto>,
        IDepartmentAppService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentQueryService _queryService;
    private readonly IDepartmentDomainService _domainService;
    private readonly IRbacChangeNotifier _rbacChangeNotifier;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="departmentRepository"></param>
    /// <param name="userRepository"></param>
    /// <param name="queryService"></param>
    /// <param name="domainService"></param>
    /// <param name="rbacChangeNotifier"></param>
    /// <param name="unitOfWorkManager"></param>
    public DepartmentAppService(
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        IDepartmentQueryService queryService,
        IDepartmentDomainService domainService,
        IRbacChangeNotifier rbacChangeNotifier,
        IUnitOfWorkManager unitOfWorkManager)
        : base(departmentRepository)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _queryService = queryService;
        _domainService = domainService;
        _rbacChangeNotifier = rbacChangeNotifier;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 根据ID获取部门
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [PermissionAuthorize("department:read")]
    public override async Task<DepartmentDto?> GetByIdAsync(long id)
    {
        return await _queryService.GetByIdAsync(id);
    }

    /// <summary>
    /// 获取子部门
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    [PermissionAuthorize("department:read")]
    public async Task<IReadOnlyList<DepartmentDto>> GetChildrenAsync(long? parentId, long? tenantId = null)
    {
        return await _queryService.GetChildrenAsync(parentId, tenantId);
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("department:create")]
    public override async Task<DepartmentDto> CreateAsync(DepartmentCreateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var entity = await MapDtoToEntityAsync(input);
        var created = await _domainService.CreateAsync(entity);
        await _departmentRepository.RebuildHierarchyAsync(created.TenantId);
        await _rbacChangeNotifier.NotifyAsync(created.TenantId, AuthorizationChangeType.DataScope);
        await uow.CompleteAsync();
        return await MapDepartmentToDtoInternalAsync(created);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [PermissionAuthorize("department:update")]
    public override async Task<DepartmentDto> UpdateAsync(DepartmentUpdateDto input)
    {
        input.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var department = await _departmentRepository.GetByIdAsync(input.BasicId)
                         ?? throw new KeyNotFoundException($"未找到部门: {input.BasicId}");

        await MapDtoToEntityAsync(input, department);
        var updated = await _domainService.UpdateAsync(department);
        await _departmentRepository.RebuildHierarchyAsync(updated.TenantId);
        await _rbacChangeNotifier.NotifyAsync(updated.TenantId, AuthorizationChangeType.DataScope);
        await uow.CompleteAsync();
        return await MapDepartmentToDtoInternalAsync(updated);
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [PermissionAuthorize("department:delete")]
    public override async Task<bool> DeleteAsync(long id)
    {
        if (id <= 0)
        {
            throw new ArgumentException("部门 ID 无效", nameof(id));
        }

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department is null)
        {
            return false;
        }

        var deleted = await _domainService.DeleteAsync(id);
        if (!deleted)
        {
            return false;
        }

        await _departmentRepository.RebuildHierarchyAsync(department.TenantId);
        await _rbacChangeNotifier.NotifyAsync(department.TenantId, AuthorizationChangeType.DataScope);
        await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 映射创建 DTO 到实体
    /// </summary>
    /// <param name="createDto"></param>
    /// <returns></returns>
    protected override Task<SysDepartment> MapDtoToEntityAsync(DepartmentCreateDto createDto)
    {
        var entity = new SysDepartment
        {
            TenantId = createDto.TenantId ?? 0,
            ParentId = createDto.ParentId,
            DepartmentName = createDto.DepartmentName.Trim(),
            DepartmentCode = createDto.DepartmentCode.Trim(),
            DepartmentType = createDto.DepartmentType,
            LeaderId = createDto.LeaderId,
            Phone = createDto.Phone,
            Email = createDto.Email,
            Address = createDto.Address,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新 DTO 到实体
    /// </summary>
    /// <param name="updateDto"></param>
    /// <param name="entity"></param>
    protected override Task MapDtoToEntityAsync(DepartmentUpdateDto updateDto, SysDepartment entity)
    {
        entity.ParentId = updateDto.ParentId;
        entity.DepartmentName = updateDto.DepartmentName.Trim();
        entity.DepartmentCode = updateDto.DepartmentCode.Trim();
        entity.DepartmentType = updateDto.DepartmentType;
        entity.LeaderId = updateDto.LeaderId;
        entity.Phone = updateDto.Phone;
        entity.Email = updateDto.Email;
        entity.Address = updateDto.Address;
        entity.Status = updateDto.Status;
        entity.Sort = updateDto.Sort;
        entity.Remark = updateDto.Remark;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射实体到 DTO，补齐负责人名称等展示字段。
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected override async Task<DepartmentDto> MapEntityToDtoAsync(SysDepartment entity)
    {
        return await MapDepartmentToDtoInternalAsync(entity);
    }

    /// <summary>
    /// 批量映射实体到 DTO。
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    protected override async Task<IList<DepartmentDto>> MapEntitiesToDtosAsync(IEnumerable<SysDepartment> entities)
    {
        return await MapDepartmentsToDtosInternalAsync(entities);
    }

    private async Task<DepartmentDto> MapDepartmentToDtoInternalAsync(SysDepartment department)
    {
        var dtos = await MapDepartmentsToDtosInternalAsync([department]);
        return dtos[0];
    }

    private async Task<IList<DepartmentDto>> MapDepartmentsToDtosInternalAsync(IEnumerable<SysDepartment> departments)
    {
        var departmentList = departments as SysDepartment[] ?? [.. departments];
        if (departmentList.Length == 0)
        {
            return [];
        }

        var leaderIds = departmentList
            .Where(item => item.LeaderId.HasValue && item.LeaderId.Value > 0)
            .Select(item => item.LeaderId!.Value)
            .Distinct()
            .ToArray();

        var leaderNameMap = await BuildLeaderNameMapAsync(leaderIds);
        var hasChildrenMap = await _departmentRepository.GetHasChildrenMapAsync(
            departmentList.Select(item => item.BasicId).ToArray(),
            departmentList[0].TenantId);

        return departmentList
            .Select(department => SaasReadModelMapper.MapDepartment(
                department,
                leaderNameMap,
                hasChildrenMap.TryGetValue(department.BasicId, out var hasChildren) && hasChildren))
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<long, string>> BuildLeaderNameMapAsync(IReadOnlyCollection<long> leaderIds)
    {
        if (leaderIds.Count == 0)
        {
            return new Dictionary<long, string>();
        }

        var users = await _userRepository.GetByIdsAsync(leaderIds);
        return users.ToDictionary(
            user => user.BasicId,
            ResolveUserDisplayName);
    }

    private static string ResolveUserDisplayName(SysUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.RealName))
        {
            return user.RealName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(user.NickName))
        {
            return user.NickName.Trim();
        }

        return user.UserName;
    }

}

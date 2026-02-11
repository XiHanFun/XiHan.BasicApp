#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentApplicationService
// Guid:232489a2-63b4-442a-8864-1781e65f8770
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Services.Departments.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Services;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Application.Services.Departments;

/// <summary>
/// 部门应用服务
/// </summary>
public class DepartmentApplicationService : CrudApplicationServiceBase<SysDepartment, SysDepartmentDto, long, CreateSysDepartmentDto, UpdateSysDepartmentDto, SysDepartmentPageRequestDto>
{
    private readonly ISysDepartmentRepository _departmentRepository;
    private readonly IDepartmentHierarchyService _hierarchyService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentApplicationService(
        ISysDepartmentRepository departmentRepository,
        IDepartmentHierarchyService hierarchyService,
        IUnitOfWorkManager unitOfWorkManager)
        : base(departmentRepository)
    {
        _departmentRepository = departmentRepository;
        _hierarchyService = hierarchyService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    public override async Task<SysDepartmentDto> CreateAsync(CreateSysDepartmentDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        // 检查部门编码是否已存在
        var exists = await _departmentRepository.IsDepartmentCodeExistsAsync(input.DepartmentCode);
        if (exists)
        {
            throw new InvalidOperationException($"部门编码 {input.DepartmentCode} 已存在");
        }

        // 验证父部门
        if (input.ParentId.HasValue)
        {
            var parent = await _departmentRepository.GetByIdAsync(input.ParentId.Value);
            if (parent == null)
            {
                throw new InvalidOperationException($"父部门 {input.ParentId.Value} 不存在");
            }
        }

        // 创建部门实体
        var department = input.Adapt<SysDepartment>();
        department.Status = YesOrNo.Yes;
        department.CreatedTime = DateTimeOffset.Now;

        // 保存部门
        department = await _departmentRepository.SaveAsync(department);

        //await uow.CompleteAsync();

        return department.Adapt<SysDepartmentDto>();
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    public override async Task<SysDepartmentDto> UpdateAsync(long id, UpdateSysDepartmentDto input)
    {
        //using var uow = _unitOfWorkManager.Begin();

        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            throw new KeyNotFoundException($"部门 {id} 不存在");
        }

        // 验证层级关系
        if (input.ParentId.HasValue)
        {
            var isValid = await _hierarchyService.ValidateDepartmentHierarchyAsync(id, input.ParentId);
            if (!isValid)
            {
                throw new InvalidOperationException("不能将部门设置为自己或其子部门的父部门");
            }
        }

        // 映射更新数据
        input.Adapt(department);
        department.ModifiedTime = DateTimeOffset.Now;

        // 保存部门
        department = await _departmentRepository.SaveAsync(department);

        //await uow.CompleteAsync();

        return department.Adapt<SysDepartmentDto>();
    }

    /// <summary>
    /// 获取租户下的部门树
    /// </summary>
    public async Task<List<SysDepartmentDto>> GetTenantDepartmentTreeAsync(long tenantId)
    {
        var departments = await _hierarchyService.BuildDepartmentTreeAsync(tenantId);
        return BuildDepartmentTreeDto(departments, null);
    }

    /// <summary>
    /// 获取用户所属的部门列表
    /// </summary>
    public async Task<List<SysDepartmentDto>> GetUserDepartmentsAsync(long userId)
    {
        var departments = await _departmentRepository.GetDepartmentsByUserIdAsync(userId);
        return departments.Adapt<List<SysDepartmentDto>>();
    }

    /// <summary>
    /// 获取部门路径
    /// </summary>
    public async Task<string> GetDepartmentPathAsync(long departmentId)
    {
        return await _hierarchyService.GetDepartmentPathAsync(departmentId);
    }

    /// <summary>
    /// 启用部门
    /// </summary>
    public async Task<bool> EnableAsync(long departmentId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _departmentRepository.EnableDepartmentAsync(departmentId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 禁用部门
    /// </summary>
    public async Task<bool> DisableAsync(long departmentId)
    {
        //using var uow = _unitOfWorkManager.Begin();
        await _departmentRepository.DisableDepartmentAsync(departmentId);
        //await uow.CompleteAsync();
        return true;
    }

    /// <summary>
    /// 构建部门树DTO
    /// </summary>
    private List<SysDepartmentDto> BuildDepartmentTreeDto(List<SysDepartment> allDepartments, long? parentId)
    {
        var result = new List<SysDepartmentDto>();

        var children = allDepartments.Where(d => d.ParentId == parentId).OrderBy(d => d.Sort).ToList();

        foreach (var dept in children)
        {
            var dto = dept.Adapt<SysDepartmentDto>();
            dto.Children = BuildDepartmentTreeDto(allDepartments, dept.BasicId);
            result.Add(dto);
        }

        return result;
    }
}

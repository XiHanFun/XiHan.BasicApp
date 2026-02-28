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
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 部门应用服务
/// </summary>
public class DepartmentAppService
    : CrudApplicationServiceBase<SysDepartment, DepartmentDto, long, DepartmentCreateDto, DepartmentUpdateDto, BasicAppPRDto>,
        IDepartmentAppService
{
    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="departmentRepository"></param>
    public DepartmentAppService(IDepartmentRepository departmentRepository)
        : base(departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 获取子部门
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<DepartmentDto>> GetChildrenAsync(long? parentId, long? tenantId = null)
    {
        var departments = await _departmentRepository.GetChildrenAsync(parentId, tenantId);
        return departments.Select(static department => department.Adapt<DepartmentDto>()).ToArray();
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<DepartmentDto> CreateAsync(DepartmentCreateDto input)
    {
        input.ValidateAnnotations();

        var normalizedCode = input.DepartmentCode.Trim();
        await EnsureDepartmentCodeUniqueAsync(normalizedCode, null, input.TenantId);

        return await base.CreateAsync(input);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    /// <param name="id"></param>
    /// <param name="input"></param>
    /// <returns></returns>
    public override async Task<DepartmentDto> UpdateAsync(long id, DepartmentUpdateDto input)
    {
        input.ValidateAnnotations();

        var department = await _departmentRepository.GetByIdAsync(id)
                         ?? throw new KeyNotFoundException($"未找到部门: {id}");

        var normalizedCode = input.DepartmentCode.Trim();
        await EnsureDepartmentCodeUniqueAsync(normalizedCode, id, department.TenantId);

        await MapDtoToEntityAsync(input, department);
        var updated = await _departmentRepository.UpdateAsync(department);
        return updated.Adapt<DepartmentDto>();
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
            TenantId = createDto.TenantId,
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
    /// 校验部门编码唯一性
    /// </summary>
    /// <param name="departmentCode"></param>
    /// <param name="excludeDepartmentId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task EnsureDepartmentCodeUniqueAsync(string departmentCode, long? excludeDepartmentId, long? tenantId)
    {
        var existing = await _departmentRepository.GetByDepartmentCodeAsync(departmentCode, tenantId);
        if (existing is not null && (!excludeDepartmentId.HasValue || existing.BasicId != excludeDepartmentId.Value))
        {
            throw new InvalidOperationException($"部门编码 '{departmentCode}' 已存在");
        }
    }
}

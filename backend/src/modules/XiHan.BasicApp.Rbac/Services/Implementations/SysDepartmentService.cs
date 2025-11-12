#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentService
// Guid:7c2b3c4d-5e6f-7890-abcd-ef12345678bc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Dtos.Departments;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 部门服务实现
/// </summary>
public class SysDepartmentService : CrudApplicationServiceBase<SysDepartment, DepartmentDto, RbacIdType, CreateDepartmentDto, UpdateDepartmentDto>, ISysDepartmentService
{
    private readonly ISysDepartmentRepository _departmentRepository;
    private readonly ISysUserRepository _userRepository;
    private readonly DepartmentManager _departmentManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDepartmentService(
        ISysDepartmentRepository departmentRepository,
        ISysUserRepository userRepository,
        DepartmentManager departmentManager) : base(departmentRepository)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _departmentManager = departmentManager;
    }

    #region 重写基类方法

    /// <summary>
    /// 根据ID获取部门（包含负责人信息）
    /// </summary>
    public override async Task<DepartmentDto?> GetByIdAsync(RbacIdType id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return null;
        }

        var dto = department.ToDto();

        // 获取负责人姓名
        if (department.LeaderId.HasValue)
        {
            var leader = await _userRepository.GetByIdAsync(department.LeaderId.Value);
            dto.LeaderName = leader?.RealName ?? leader?.UserName;
        }

        return dto;
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    public override async Task<DepartmentDto> CreateAsync(CreateDepartmentDto input)
    {
        // 验证部门编码唯一性
        if (!await _departmentManager.IsDepartmentCodeUniqueAsync(input.DepartmentCode))
        {
            throw new InvalidOperationException(ErrorMessageConstants.DepartmentCodeExists);
        }

        var department = new SysDepartment
        {
            ParentId = input.ParentId,
            DepartmentName = input.DepartmentName,
            DepartmentCode = input.DepartmentCode,
            DepartmentType = input.DepartmentType,
            LeaderId = input.LeaderId,
            Phone = input.Phone,
            Email = input.Email,
            Address = input.Address,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _departmentRepository.AddAsync(department);

        return department.ToDto();
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    public override async Task<DepartmentDto> UpdateAsync(RbacIdType id, UpdateDepartmentDto input)
    {
        var department = await _departmentRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.DepartmentNotFound);

        // 更新部门信息
        if (input.ParentId.HasValue)
        {
            department.ParentId = input.ParentId;
        }

        if (input.DepartmentName != null)
        {
            department.DepartmentName = input.DepartmentName;
        }

        if (input.DepartmentType.HasValue)
        {
            department.DepartmentType = input.DepartmentType.Value;
        }

        if (input.LeaderId.HasValue)
        {
            department.LeaderId = input.LeaderId;
        }

        if (input.Phone != null)
        {
            department.Phone = input.Phone;
        }

        if (input.Email != null)
        {
            department.Email = input.Email;
        }

        if (input.Address != null)
        {
            department.Address = input.Address;
        }

        if (input.Status.HasValue)
        {
            department.Status = input.Status.Value;
        }

        if (input.Sort.HasValue)
        {
            department.Sort = input.Sort.Value;
        }

        if (input.Remark != null)
        {
            department.Remark = input.Remark;
        }

        await _departmentRepository.UpdateAsync(department);

        return department.ToDto();
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    public override async Task<bool> DeleteAsync(RbacIdType id)
    {
        var department = await _departmentRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.DepartmentNotFound);

        // 检查是否可以删除
        if (!await _departmentManager.CanDeleteAsync(id))
        {
            var children = await _departmentRepository.GetChildrenAsync(id);
            if (children.Any())
            {
                throw new InvalidOperationException(ErrorMessageConstants.DepartmentHasChildren);
            }
            else
            {
                throw new InvalidOperationException(ErrorMessageConstants.DepartmentHasUsers);
            }
        }

        return await _departmentRepository.DeleteAsync(department);
    }

    #endregion 重写基类方法

    #region 业务特定方法

    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    public async Task<DepartmentDto?> GetByDepartmentCodeAsync(string departmentCode)
    {
        var department = await _departmentRepository.GetByDepartmentCodeAsync(departmentCode);
        return department?.ToDto();
    }

    /// <summary>
    /// 获取部门树
    /// </summary>
    public async Task<List<DepartmentTreeDto>> GetTreeAsync()
    {
        var allDepartments = await _departmentRepository.GetListAsync();
        var departmentDtos = allDepartments.ToDto();
        return departmentDtos.BuildTree();
    }

    /// <summary>
    /// 根据父级ID获取子部门
    /// </summary>
    public async Task<List<DepartmentDto>> GetChildrenAsync(RbacIdType parentId)
    {
        var children = await _departmentRepository.GetChildrenAsync(parentId);
        return children.ToDto();
    }

    /// <summary>
    /// 获取用户的部门列表
    /// </summary>
    public async Task<List<DepartmentDto>> GetByUserIdAsync(RbacIdType userId)
    {
        var departments = await _departmentRepository.GetByUserIdAsync(userId);
        return departments.ToDto();
    }

    #endregion 业务特定方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<DepartmentDto> MapToEntityDtoAsync(SysDepartment entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 DepartmentDto 到实体（基类方法）
    /// </summary>
    protected override Task<SysDepartment> MapToEntityAsync(DepartmentDto dto)
    {
        var entity = new SysDepartment
        {
            ParentId = dto.ParentId,
            DepartmentName = dto.DepartmentName,
            DepartmentCode = dto.DepartmentCode,
            DepartmentType = dto.DepartmentType,
            LeaderId = dto.LeaderId,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            Status = dto.Status,
            Sort = dto.Sort,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 DepartmentDto 到现有实体（基类方法）
    /// </summary>
    protected override Task MapToEntityAsync(DepartmentDto dto, SysDepartment entity)
    {
        entity.ParentId = dto.ParentId;
        if (dto.DepartmentName != null) entity.DepartmentName = dto.DepartmentName;
        entity.DepartmentType = dto.DepartmentType;
        entity.LeaderId = dto.LeaderId;
        if (dto.Phone != null) entity.Phone = dto.Phone;
        if (dto.Email != null) entity.Email = dto.Email;
        if (dto.Address != null) entity.Address = dto.Address;
        entity.Status = dto.Status;
        entity.Sort = dto.Sort;
        if (dto.Remark != null) entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysDepartment> MapToEntityAsync(CreateDepartmentDto createDto)
    {
        var entity = new SysDepartment
        {
            ParentId = createDto.ParentId,
            DepartmentName = createDto.DepartmentName,
            DepartmentCode = createDto.DepartmentCode,
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
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateDepartmentDto updateDto, SysDepartment entity)
    {
        if (updateDto.ParentId.HasValue) entity.ParentId = updateDto.ParentId;
        if (updateDto.DepartmentName != null) entity.DepartmentName = updateDto.DepartmentName;
        if (updateDto.DepartmentType.HasValue) entity.DepartmentType = updateDto.DepartmentType.Value;
        if (updateDto.LeaderId.HasValue) entity.LeaderId = updateDto.LeaderId;
        if (updateDto.Phone != null) entity.Phone = updateDto.Phone;
        if (updateDto.Email != null) entity.Email = updateDto.Email;
        if (updateDto.Address != null) entity.Address = updateDto.Address;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Sort.HasValue) entity.Sort = updateDto.Sort.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}

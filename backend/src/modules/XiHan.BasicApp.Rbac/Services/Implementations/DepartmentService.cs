#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentService
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
public class DepartmentService : ApplicationServiceBase, IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly DepartmentManager _departmentManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        DepartmentManager departmentManager)
    {
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _departmentManager = departmentManager;
    }

    /// <summary>
    /// 根据ID获取部门
    /// </summary>
    public async Task<DepartmentDto?> GetByIdAsync(long id)
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
    public async Task<List<DepartmentDto>> GetChildrenAsync(long parentId)
    {
        var children = await _departmentRepository.GetChildrenAsync(parentId);
        return children.ToDto();
    }

    /// <summary>
    /// 创建部门
    /// </summary>
    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto input)
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
    public async Task<DepartmentDto> UpdateAsync(UpdateDepartmentDto input)
    {
        var department = await _departmentRepository.GetByIdAsync(input.BasicId);
        if (department == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.DepartmentNotFound);
        }

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
    public async Task<bool> DeleteAsync(long id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.DepartmentNotFound);
        }

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

    /// <summary>
    /// 获取用户的部门列表
    /// </summary>
    public async Task<List<DepartmentDto>> GetByUserIdAsync(long userId)
    {
        var departments = await _departmentRepository.GetByUserIdAsync(userId);
        return departments.ToDto();
    }
}

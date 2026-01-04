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

using Mapster;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Departments;
using XiHan.BasicApp.Rbac.Repositories.Users;
using XiHan.BasicApp.Rbac.Services.Departments.Dtos;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Utils.Collections;

namespace XiHan.BasicApp.Rbac.Services.Departments;

/// <summary>
/// 系统部门服务实现
/// </summary>
public class SysDepartmentService : CrudApplicationServiceBase<SysDepartment, DepartmentDto, XiHanBasicAppIdType, CreateDepartmentDto, UpdateDepartmentDto>, ISysDepartmentService
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
    public override async Task<DepartmentDto?> GetByIdAsync(XiHanBasicAppIdType id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return null;
        }

        var dto = department.Adapt<DepartmentDto>();

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

        return department.Adapt<DepartmentDto>();
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    public override async Task<DepartmentDto> UpdateAsync(XiHanBasicAppIdType id, UpdateDepartmentDto input)
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

        return department.Adapt<DepartmentDto>();
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    public override async Task<bool> DeleteAsync(XiHanBasicAppIdType id)
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
        return department.Adapt<DepartmentDto>();
    }

    /// <summary>
    /// 获取部门树
    /// </summary>
    public async Task<List<DepartmentTreeDto>> GetTreeAsync()
    {
        var allDepartments = await _departmentRepository.GetListAsync();
        var departmentDtos = allDepartments.Adapt<List<DepartmentTreeDto>>();
        // TODO: 实现树形结构转换
        //return departmentDtos.ToTree<DepartmentTreeDto>();
        return departmentDtos;
    }

    /// <summary>
    /// 根据父级ID获取子部门
    /// </summary>
    public async Task<List<DepartmentDto>> GetChildrenAsync(XiHanBasicAppIdType parentId)
    {
        var children = await _departmentRepository.GetChildrenAsync(parentId);
        return children.Adapt<List<DepartmentDto>>();
    }

    /// <summary>
    /// 获取用户的部门列表
    /// </summary>
    public async Task<List<DepartmentDto>> GetByUserIdAsync(XiHanBasicAppIdType userId)
    {
        var departments = await _departmentRepository.GetByUserIdAsync(userId);
        return departments.Adapt<List<DepartmentDto>>();
    }

    #endregion 业务特定方法
}

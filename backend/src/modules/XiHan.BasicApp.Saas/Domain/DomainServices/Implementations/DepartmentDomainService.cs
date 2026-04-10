#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentDomainService
// Guid:3b4c5d6e-7f80-9012-bcde-f01234567804
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;
using XiHan.Framework.EventBus.Abstractions.Local;

namespace XiHan.BasicApp.Saas.Domain.DomainServices.Implementations;

/// <summary>
/// 部门领域服务
/// </summary>
public class DepartmentDomainService : IDepartmentDomainService, ITransientDependency
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly ILocalEventBus _localEventBus;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentDomainService(IDepartmentRepository departmentRepository, ILocalEventBus localEventBus)
    {
        _departmentRepository = departmentRepository;
        _localEventBus = localEventBus;
    }

    /// <inheritdoc />
    public async Task<SysDepartment> CreateAsync(SysDepartment department)
    {
        var created = await _departmentRepository.AddAsync(department);
        await _localEventBus.PublishAsync(new DepartmentChangedDomainEvent(created.BasicId));
        return created;
    }

    /// <inheritdoc />
    public async Task<SysDepartment> UpdateAsync(SysDepartment department)
    {
        var updated = await _departmentRepository.UpdateAsync(department);
        await _localEventBus.PublishAsync(new DepartmentChangedDomainEvent(updated.BasicId));
        return updated;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(long id)
    {
        var department = await _departmentRepository.GetByIdAsync(id);
        if (department == null)
        {
            return false;
        }

        var result = await _departmentRepository.DeleteAsync(department);
        if (result)
        {
            await _localEventBus.PublishAsync(new DepartmentChangedDomainEvent(id));
        }
        return result;
    }
}

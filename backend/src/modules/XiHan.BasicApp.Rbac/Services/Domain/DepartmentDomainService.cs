#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentDomainService
// Guid:a8b9c0d1-e2f3-4a5b-6c7d-8e9f0a1b2c3d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Services.Domain;

/// <summary>
/// 部门领域服务
/// 处理部门相关的业务逻辑（部门树构建、部门验证等）
/// </summary>
/// <remarks>
/// 从 DepartmentManager 迁移的逻辑
/// TODO: 后续需要更新为新的 IDepartmentRepository 接口
/// </remarks>
public class DepartmentDomainService : DomainService
{
    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentDomainService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 验证部门编码是否唯一
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <param name="excludeId">排除的部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否唯一</returns>
    public async Task<bool> IsDepartmentCodeUniqueAsync(string departmentCode, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = await _departmentRepository.ExistsByDepartmentCodeAsync(departmentCode, excludeId);
        return !exists;
    }

    /// <summary>
    /// 检查部门是否可以删除
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以删除</returns>
    public async Task<bool> CanDeleteAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanDeleteAsync), new { departmentId });

        // 检查是否有子部门
        var children = await _departmentRepository.GetChildrenAsync(departmentId);
        if (children.Count > 0)
        {
            throw new InvalidOperationException($"部门有 {children.Count} 个子部门，无法删除");
        }

        // 检查部门下是否有用户
        var userCount = await _departmentRepository.GetDepartmentUserCountAsync(departmentId);
        if (userCount > 0)
        {
            throw new InvalidOperationException($"部门下还有 {userCount} 个用户，无法删除");
        }

        Logger.LogInformation("部门 {DepartmentId} 可以删除", departmentId);
        return true;
    }

    /// <summary>
    /// 构建部门树（递归）
    /// </summary>
    /// <param name="parentId">父部门ID（null为根节点）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门树</returns>
    public async Task<List<SysDepartment>> BuildDepartmentTreeAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(BuildDepartmentTreeAsync), new { parentId });

        parentId ??= 0;

        var departments = await _departmentRepository.GetByParentIdAsync(parentId.Value);

        // 递归构建子部门（注意：实际实现中可能需要优化递归深度）
        foreach (var department in departments)
        {
            // 子部门可以通过导航属性或递归加载
            // 这里只是示例，实际实现可能在 Partial 类中处理
        }

        Logger.LogInformation("构建部门树，父节点: {ParentId}, 部门数: {DepartmentCount}", parentId, departments.Count);
        return departments;
    }

    /// <summary>
    /// 获取部门路径（从根到当前节点）
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门路径列表（从根到叶子）</returns>
    public async Task<List<SysDepartment>> GetDepartmentPathAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(GetDepartmentPathAsync), new { departmentId });

        var path = new List<SysDepartment>();
        var currentDepartment = await _departmentRepository.GetByIdAsync(departmentId);

        while (currentDepartment != null)
        {
            path.Insert(0, currentDepartment); // 插入到列表开头

            if (currentDepartment.ParentId.HasValue)
            {
                currentDepartment = await _departmentRepository.GetByIdAsync(currentDepartment.ParentId.Value);
            }
            else
            {
                break;
            }
        }

        Logger.LogInformation("部门 {DepartmentId} 路径长度: {PathLength}", departmentId, path.Count);
        return path;
    }

    /// <summary>
    /// 检查部门继承关系是否合法（避免循环继承）
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <param name="parentDepartmentId">父部门ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否可以设置父部门</returns>
    public async Task<bool> CanSetParentDepartmentAsync(long departmentId, long parentDepartmentId, CancellationToken cancellationToken = default)
    {
        LogDomainOperation(nameof(CanSetParentDepartmentAsync), new { departmentId, parentDepartmentId });

        // 不能将自己设为父部门
        if (departmentId == parentDepartmentId)
        {
            throw new InvalidOperationException("不能将部门设置为自己的父部门");
        }

        // 检查是否会形成循环继承
        var path = await GetDepartmentPathAsync(parentDepartmentId, cancellationToken);
        if (path.Any(d => d.BasicId == departmentId))
        {
            throw new InvalidOperationException("设置该父部门会形成循环继承关系");
        }

        Logger.LogInformation("部门 {DepartmentId} 可以设置父部门 {ParentDepartmentId}", departmentId, parentDepartmentId);
        return true;
    }
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ResourceCommandService
// Guid:b9c0d1e2-f3a4-4b5c-5d6e-8f9a0b1c2d3e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Application.Commands;

/// <summary>
/// 资源命令服务（处理资源的写操作）
/// </summary>
public class ResourceCommandService : CrudApplicationServiceBase<SysResource, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IResourceRepository _resourceRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ResourceCommandService(IResourceRepository resourceRepository)
        : base(resourceRepository)
    {
        _resourceRepository = resourceRepository;
    }

    /// <summary>
    /// 创建资源（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        var exists = await _resourceRepository.ExistsByResourceCodeAsync(input.ResourceCode);
        if (exists)
        {
            throw new InvalidOperationException($"资源编码 {input.ResourceCode} 已存在");
        }

        // 2. 映射并创建
        var resource = input.Adapt<SysResource>();

        // 3. 保存
        resource = await _resourceRepository.AddAsync(resource);

        return await MapToEntityDtoAsync(resource);
    }

    /// <summary>
    /// 更新资源（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取资源
        var resource = await _resourceRepository.GetByIdAsync(id);
        if (resource == null)
        {
            throw new KeyNotFoundException($"资源 {id} 不存在");
        }

        // 2. 业务验证
        if (resource.ResourceCode != input.ResourceCode)
        {
            var exists = await _resourceRepository.ExistsByResourceCodeAsync(input.ResourceCode, id);
            if (exists)
            {
                throw new InvalidOperationException($"资源编码 {input.ResourceCode} 已存在");
            }
        }

        // 3. 更新实体
        input.Adapt(resource);

        // 4. 保存
        resource = await _resourceRepository.UpdateAsync(resource);

        return await MapToEntityDtoAsync(resource);
    }

    /// <summary>
    /// 更新资源状态
    /// </summary>
    /// <param name="resourceId">资源ID</param>
    /// <param name="status">状态</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateStatusAsync(long resourceId, Enums.YesOrNo status)
    {
        var resource = await _resourceRepository.GetByIdAsync(resourceId);
        if (resource == null)
        {
            throw new KeyNotFoundException($"资源 {resourceId} 不存在");
        }

        resource.Status = status;
        await _resourceRepository.UpdateAsync(resource);
        return true;
    }
}

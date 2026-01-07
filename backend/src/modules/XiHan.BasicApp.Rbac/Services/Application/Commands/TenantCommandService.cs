#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantCommandService
// Guid:d1e2f3a4-b5c6-4d5e-7f8a-0b1c2d3e4f5a
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
/// 租户命令服务（处理租户的写操作）
/// </summary>
public class TenantCommandService : CrudApplicationServiceBase<SysTenant, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly ITenantRepository _tenantRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantCommandService(ITenantRepository tenantRepository)
        : base(tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    /// <summary>
    /// 创建租户（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        var codeExists = await _tenantRepository.ExistsByTenantCodeAsync(input.TenantCode);
        if (codeExists)
        {
            throw new InvalidOperationException($"租户编码 {input.TenantCode} 已存在");
        }

        if (!string.IsNullOrEmpty(input.Domain))
        {
            var domainExists = await _tenantRepository.ExistsByDomainAsync(input.Domain);
            if (domainExists)
            {
                throw new InvalidOperationException($"域名 {input.Domain} 已被使用");
            }
        }

        // 2. 映射并创建
        var tenant = input.Adapt<SysTenant>();

        // 3. 保存
        tenant = await _tenantRepository.AddAsync(tenant);

        return await MapToEntityDtoAsync(tenant);
    }

    /// <summary>
    /// 更新租户（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取租户
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"租户 {id} 不存在");
        }

        // 2. 业务验证
        if (tenant.TenantCode != input.TenantCode)
        {
            var codeExists = await _tenantRepository.ExistsByTenantCodeAsync(input.TenantCode, id);
            if (codeExists)
            {
                throw new InvalidOperationException($"租户编码 {input.TenantCode} 已存在");
            }
        }

        if (!string.IsNullOrEmpty(input.Domain) && tenant.Domain != input.Domain)
        {
            var domainExists = await _tenantRepository.ExistsByDomainAsync(input.Domain, id);
            if (domainExists)
            {
                throw new InvalidOperationException($"域名 {input.Domain} 已被使用");
            }
        }

        // 3. 更新实体
        input.Adapt(tenant);

        // 4. 保存
        tenant = await _tenantRepository.UpdateAsync(tenant);

        return await MapToEntityDtoAsync(tenant);
    }

    /// <summary>
    /// 更新租户状态
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="status">状态</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateStatusAsync(long tenantId, Enums.YesOrNo status)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new KeyNotFoundException($"租户 {tenantId} 不存在");
        }

        tenant.Status = status;
        await _tenantRepository.UpdateAsync(tenant);
        return true;
    }
}

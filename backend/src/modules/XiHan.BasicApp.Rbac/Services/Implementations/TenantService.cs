#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantService
// Guid:8c2b3c4d-5e6f-7890-abcd-ef12345678bd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Dtos.Tenants;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Abstractions;
using XiHan.BasicApp.Rbac.Services.Abstractions;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Implementations;

/// <summary>
/// 租户服务实现
/// </summary>
public class TenantService : ApplicationServiceBase, ITenantService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly TenantManager _tenantManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantService(
        ITenantRepository tenantRepository,
        TenantManager tenantManager)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
    }

    /// <summary>
    /// 根据ID获取租户
    /// </summary>
    public async Task<TenantDto?> GetByIdAsync(long id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        return tenant?.ToDto();
    }

    /// <summary>
    /// 获取租户详情
    /// </summary>
    public async Task<TenantDetailDto?> GetDetailAsync(long id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
        {
            return null;
        }

        var userCount = await _tenantRepository.GetTenantUserCountAsync(id);
        var usedStorage = await _tenantRepository.GetTenantUsedStorageAsync(id);

        return new TenantDetailDto
        {
            BasicId = tenant.BasicId,
            TenantCode = tenant.TenantCode,
            TenantName = tenant.TenantName,
            TenantShortName = tenant.TenantShortName,
            ContactPerson = tenant.ContactPerson,
            ContactPhone = tenant.ContactPhone,
            ContactEmail = tenant.ContactEmail,
            Address = tenant.Address,
            Logo = tenant.Logo,
            Domain = tenant.Domain,
            IsolationMode = tenant.IsolationMode,
            ConfigStatus = tenant.ConfigStatus,
            ExpireTime = tenant.ExpireTime,
            UserLimit = tenant.UserLimit,
            StorageLimit = tenant.StorageLimit,
            TenantStatus = tenant.TenantStatus,
            Status = tenant.Status,
            Sort = tenant.Sort,
            Remark = tenant.Remark,
            CreatedBy = tenant.CreatedBy,
            CreatedTime = tenant.CreatedTime,
            ModifiedBy = tenant.ModifiedBy,
            ModifiedTime = tenant.ModifiedTime,
            IsDeleted = tenant.IsDeleted,
            DeletedBy = tenant.DeletedBy,
            DeletedTime = tenant.DeletedTime,
            DatabaseType = tenant.DatabaseType,
            DatabaseHost = tenant.DatabaseHost,
            DatabasePort = tenant.DatabasePort,
            DatabaseName = tenant.DatabaseName,
            DatabaseSchema = tenant.DatabaseSchema,
            UserCount = userCount,
            UsedStorage = usedStorage
        };
    }

    /// <summary>
    /// 根据租户编码获取租户
    /// </summary>
    public async Task<TenantDto?> GetByTenantCodeAsync(string tenantCode)
    {
        var tenant = await _tenantRepository.GetByTenantCodeAsync(tenantCode);
        return tenant?.ToDto();
    }

    /// <summary>
    /// 根据域名获取租户
    /// </summary>
    public async Task<TenantDto?> GetByDomainAsync(string domain)
    {
        var tenant = await _tenantRepository.GetByDomainAsync(domain);
        return tenant?.ToDto();
    }

    /// <summary>
    /// 创建租户
    /// </summary>
    public async Task<TenantDto> CreateAsync(CreateTenantDto input)
    {
        // 验证租户编码唯一性
        if (!await _tenantManager.IsTenantCodeUniqueAsync(input.TenantCode))
        {
            throw new InvalidOperationException(ErrorMessageConstants.TenantCodeExists);
        }

        // 验证域名唯一性
        if (!string.IsNullOrEmpty(input.Domain) && !await _tenantManager.IsDomainUniqueAsync(input.Domain))
        {
            throw new InvalidOperationException(ErrorMessageConstants.DomainExists);
        }

        var tenant = new SysTenant
        {
            TenantCode = input.TenantCode,
            TenantName = input.TenantName,
            TenantShortName = input.TenantShortName,
            ContactPerson = input.ContactPerson,
            ContactPhone = input.ContactPhone,
            ContactEmail = input.ContactEmail,
            Address = input.Address,
            Logo = input.Logo,
            Domain = input.Domain,
            IsolationMode = input.IsolationMode,
            DatabaseType = input.DatabaseType,
            DatabaseHost = input.DatabaseHost,
            DatabasePort = input.DatabasePort,
            DatabaseName = input.DatabaseName,
            DatabaseSchema = input.DatabaseSchema,
            DatabaseUser = input.DatabaseUser,
            DatabasePassword = input.DatabasePassword,
            ExpireTime = input.ExpireTime,
            UserLimit = input.UserLimit,
            StorageLimit = input.StorageLimit,
            Sort = input.Sort,
            Remark = input.Remark
        };

        await _tenantRepository.InsertAsync(tenant);

        return tenant.ToDto();
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    public async Task<TenantDto> UpdateAsync(UpdateTenantDto input)
    {
        var tenant = await _tenantRepository.GetByIdAsync(input.BasicId);
        if (tenant == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.TenantNotFound);
        }

        // 验证域名唯一性
        if (!string.IsNullOrEmpty(input.Domain) && !await _tenantManager.IsDomainUniqueAsync(input.Domain, tenant.BasicId))
        {
            throw new InvalidOperationException(ErrorMessageConstants.DomainExists);
        }

        // 更新租户信息
        if (input.TenantName != null)
        {
            tenant.TenantName = input.TenantName;
        }

        if (input.TenantShortName != null)
        {
            tenant.TenantShortName = input.TenantShortName;
        }

        if (input.ContactPerson != null)
        {
            tenant.ContactPerson = input.ContactPerson;
        }

        if (input.ContactPhone != null)
        {
            tenant.ContactPhone = input.ContactPhone;
        }

        if (input.ContactEmail != null)
        {
            tenant.ContactEmail = input.ContactEmail;
        }

        if (input.Address != null)
        {
            tenant.Address = input.Address;
        }

        if (input.Logo != null)
        {
            tenant.Logo = input.Logo;
        }

        if (input.Domain != null)
        {
            tenant.Domain = input.Domain;
        }

        if (input.ExpireTime.HasValue)
        {
            tenant.ExpireTime = input.ExpireTime;
        }

        if (input.UserLimit.HasValue)
        {
            tenant.UserLimit = input.UserLimit;
        }

        if (input.StorageLimit.HasValue)
        {
            tenant.StorageLimit = input.StorageLimit;
        }

        if (input.TenantStatus.HasValue)
        {
            tenant.TenantStatus = input.TenantStatus.Value;
        }

        if (input.Status.HasValue)
        {
            tenant.Status = input.Status.Value;
        }

        if (input.Sort.HasValue)
        {
            tenant.Sort = input.Sort.Value;
        }

        if (input.Remark != null)
        {
            tenant.Remark = input.Remark;
        }

        await _tenantRepository.UpdateAsync(tenant);

        return tenant.ToDto();
    }

    /// <summary>
    /// 删除租户
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id);
        if (tenant == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.TenantNotFound);
        }

        return await _tenantRepository.DeleteAsync(tenant);
    }

    /// <summary>
    /// 配置租户数据库
    /// </summary>
    public async Task<bool> ConfigureDatabaseAsync(ConfigureTenantDatabaseDto input)
    {
        var tenant = await _tenantRepository.GetByIdAsync(input.TenantId);
        if (tenant == null)
        {
            throw new InvalidOperationException(ErrorMessageConstants.TenantNotFound);
        }

        tenant.DatabaseType = input.DatabaseType;
        tenant.DatabaseHost = input.DatabaseHost;
        tenant.DatabasePort = input.DatabasePort;
        tenant.DatabaseName = input.DatabaseName;
        tenant.DatabaseSchema = input.DatabaseSchema;
        tenant.DatabaseUser = input.DatabaseUser;
        tenant.DatabasePassword = input.DatabasePassword;

        // 这里可以添加连接字符串构建逻辑
        // tenant.ConnectionString = BuildConnectionString(input);

        return await _tenantRepository.UpdateAsync(tenant);
    }

    /// <summary>
    /// 分页查询租户
    /// </summary>
    public async Task<PageResponse<TenantDto>> GetPagedListAsync(PageQuery query)
    {
        var queryable = _tenantRepository.Queryable();

        // 应用筛选条件
        if (query.Conditions != null && query.Conditions.Any())
        {
            foreach (var condition in query.Conditions)
            {
                if (condition.Field == "TenantName" && !string.IsNullOrEmpty(condition.Value?.ToString()))
                {
                    queryable = queryable.Where(t => t.TenantName.Contains(condition.Value.ToString()!));
                }
            }
        }

        // 应用排序
        if (query.Sorts != null && query.Sorts.Any())
        {
            foreach (var sort in query.Sorts)
            {
                queryable = sort.Direction == Paging.Enums.SortDirection.Ascending
                    ? queryable.OrderBy($"{sort.Field} ASC")
                    : queryable.OrderBy($"{sort.Field} DESC");
            }
        }
        else
        {
            queryable = queryable.OrderBy(t => t.Sort);
        }

        // 分页
        var total = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.PageIndex - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PageResponse<TenantDto>
        {
            Items = items.ToDto(),
            PageInfo = new PageInfo
            {
                PageIndex = query.PageIndex,
                PageSize = query.PageSize,
                Total = total
            }
        };
    }
}

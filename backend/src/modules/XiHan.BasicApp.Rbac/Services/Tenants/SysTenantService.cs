#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantService
// Guid:8c2b3c4d-5e6f-7890-abcd-ef12345678bd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 7:15:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Constants;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Extensions;
using XiHan.BasicApp.Rbac.Managers;
using XiHan.BasicApp.Rbac.Repositories.Tenants;
using XiHan.BasicApp.Rbac.Services.Tenants.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Tenants;

/// <summary>
/// 系统租户服务实现
/// </summary>
public class SysTenantService : CrudApplicationServiceBase<SysTenant, TenantDto, XiHanBasicAppIdType, CreateTenantDto, UpdateTenantDto>, ISysTenantService
{
    private readonly ISysTenantRepository _tenantRepository;
    private readonly TenantManager _tenantManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysTenantService(
        ISysTenantRepository tenantRepository,
        TenantManager tenantManager) : base(tenantRepository)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
    }

    #region 业务特定方法

    /// <summary>
    /// 获取租户详情
    /// </summary>
    public async Task<TenantDetailDto?> GetDetailAsync(XiHanBasicAppIdType id)
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

        await _tenantRepository.UpdateAsync(tenant);
        return true;
    }

    #endregion 业务特定方法

    #region 重写基类方法

    /// <summary>
    /// 创建租户
    /// </summary>
    public override async Task<TenantDto> CreateAsync(CreateTenantDto input)
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

        await _tenantRepository.AddAsync(tenant);

        return tenant.ToDto();
    }

    /// <summary>
    /// 更新租户
    /// </summary>
    public override async Task<TenantDto> UpdateAsync(XiHanBasicAppIdType id, UpdateTenantDto input)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.TenantNotFound);

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
    public override async Task<bool> DeleteAsync(XiHanBasicAppIdType id)
    {
        var tenant = await _tenantRepository.GetByIdAsync(id) ??
            throw new InvalidOperationException(ErrorMessageConstants.TenantNotFound);

        return await _tenantRepository.DeleteAsync(tenant);
    }

    #endregion 重写基类方法

    #region 映射方法实现

    /// <summary>
    /// 映射实体到DTO
    /// </summary>
    protected override Task<TenantDto> MapToEntityDtoAsync(SysTenant entity)
    {
        return Task.FromResult(entity.ToDto());
    }

    /// <summary>
    /// 映射 TenantDto 到实体（基类方法）
    /// </summary>
    protected override Task<SysTenant> MapToEntityAsync(TenantDto dto)
    {
        var entity = new SysTenant
        {
            TenantCode = dto.TenantCode,
            TenantName = dto.TenantName,
            TenantShortName = dto.TenantShortName,
            ContactPerson = dto.ContactPerson,
            ContactPhone = dto.ContactPhone,
            ContactEmail = dto.ContactEmail,
            Address = dto.Address,
            Logo = dto.Logo,
            Domain = dto.Domain,
            ExpireTime = dto.ExpireTime,
            UserLimit = dto.UserLimit,
            StorageLimit = dto.StorageLimit,
            TenantStatus = dto.TenantStatus,
            Status = dto.Status,
            Sort = dto.Sort,
            Remark = dto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射 TenantDto 到现有实体（基类方法）
    /// </summary>
    protected override Task MapToEntityAsync(TenantDto dto, SysTenant entity)
    {
        if (dto.TenantName != null) entity.TenantName = dto.TenantName;
        if (dto.TenantShortName != null) entity.TenantShortName = dto.TenantShortName;
        if (dto.ContactPerson != null) entity.ContactPerson = dto.ContactPerson;
        if (dto.ContactPhone != null) entity.ContactPhone = dto.ContactPhone;
        if (dto.ContactEmail != null) entity.ContactEmail = dto.ContactEmail;
        if (dto.Address != null) entity.Address = dto.Address;
        if (dto.Logo != null) entity.Logo = dto.Logo;
        if (dto.Domain != null) entity.Domain = dto.Domain;
        entity.ExpireTime = dto.ExpireTime;
        entity.UserLimit = dto.UserLimit;
        entity.StorageLimit = dto.StorageLimit;
        entity.TenantStatus = dto.TenantStatus;
        entity.Status = dto.Status;
        entity.Sort = dto.Sort;
        if (dto.Remark != null) entity.Remark = dto.Remark;

        return Task.CompletedTask;
    }

    /// <summary>
    /// 映射创建DTO到实体
    /// </summary>
    protected override Task<SysTenant> MapToEntityAsync(CreateTenantDto createDto)
    {
        var entity = new SysTenant
        {
            TenantCode = createDto.TenantCode,
            TenantName = createDto.TenantName,
            TenantShortName = createDto.TenantShortName,
            ContactPerson = createDto.ContactPerson,
            ContactPhone = createDto.ContactPhone,
            ContactEmail = createDto.ContactEmail,
            Address = createDto.Address,
            Logo = createDto.Logo,
            Domain = createDto.Domain,
            IsolationMode = createDto.IsolationMode,
            DatabaseType = createDto.DatabaseType,
            DatabaseHost = createDto.DatabaseHost,
            DatabasePort = createDto.DatabasePort,
            DatabaseName = createDto.DatabaseName,
            DatabaseSchema = createDto.DatabaseSchema,
            DatabaseUser = createDto.DatabaseUser,
            DatabasePassword = createDto.DatabasePassword,
            ExpireTime = createDto.ExpireTime,
            UserLimit = createDto.UserLimit,
            StorageLimit = createDto.StorageLimit,
            Sort = createDto.Sort,
            Remark = createDto.Remark
        };

        return Task.FromResult(entity);
    }

    /// <summary>
    /// 映射更新DTO到现有实体
    /// </summary>
    protected override Task MapToEntityAsync(UpdateTenantDto updateDto, SysTenant entity)
    {
        if (updateDto.TenantName != null) entity.TenantName = updateDto.TenantName;
        if (updateDto.TenantShortName != null) entity.TenantShortName = updateDto.TenantShortName;
        if (updateDto.ContactPerson != null) entity.ContactPerson = updateDto.ContactPerson;
        if (updateDto.ContactPhone != null) entity.ContactPhone = updateDto.ContactPhone;
        if (updateDto.ContactEmail != null) entity.ContactEmail = updateDto.ContactEmail;
        if (updateDto.Address != null) entity.Address = updateDto.Address;
        if (updateDto.Logo != null) entity.Logo = updateDto.Logo;
        if (updateDto.Domain != null) entity.Domain = updateDto.Domain;
        if (updateDto.ExpireTime.HasValue) entity.ExpireTime = updateDto.ExpireTime;
        if (updateDto.UserLimit.HasValue) entity.UserLimit = updateDto.UserLimit;
        if (updateDto.StorageLimit.HasValue) entity.StorageLimit = updateDto.StorageLimit;
        if (updateDto.TenantStatus.HasValue) entity.TenantStatus = updateDto.TenantStatus.Value;
        if (updateDto.Status.HasValue) entity.Status = updateDto.Status.Value;
        if (updateDto.Sort.HasValue) entity.Sort = updateDto.Sort.Value;
        if (updateDto.Remark != null) entity.Remark = updateDto.Remark;

        return Task.CompletedTask;
    }

    #endregion 映射方法实现
}

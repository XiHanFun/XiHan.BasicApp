#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantManagementService
// Guid:e7f8a9bc-def1-2345-6789-0abcdef12345
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using System.Text;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Domain.Services.Implementations;

/// <summary>
/// 租户管理领域服务实现
/// </summary>
public class TenantManagementService : DomainService, ITenantManagementService
{
    private readonly ISysTenantRepository _tenantRepository;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantManagementService(ISysTenantRepository tenantRepository, ISqlSugarDbContext dbContext)
    {
        _tenantRepository = tenantRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 验证租户是否处于活跃状态
    /// </summary>
    public bool IsTenantActive(SysTenant tenant)
    {
        if (tenant == null)
        {
            return false;
        }

        // 检查租户状态
        if (tenant.Status != YesOrNo.Yes)
        {
            return false;
        }

        // 检查租户业务状态
        if (tenant.TenantStatus != TenantStatus.Normal)
        {
            return false;
        }

        // 检查是否过期
        if (IsTenantExpired(tenant))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 检查租户是否已过期
    /// </summary>
    public bool IsTenantExpired(SysTenant tenant)
    {
        if (tenant == null)
        {
            return true;
        }

        if (tenant.ExpireTime.HasValue && tenant.ExpireTime.Value <= DateTimeOffset.Now)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查租户用户数是否超限
    /// </summary>
    public async Task<bool> IsUserLimitExceededAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null || !tenant.UserLimit.HasValue)
        {
            return false;
        }

        var userCount = await _dbContext.GetClient().Queryable<SysUser>()
            .Where(u => u.TenantId == tenantId)
            .CountAsync(cancellationToken);

        return userCount >= tenant.UserLimit.Value;
    }

    /// <summary>
    /// 检查租户存储空间是否超限
    /// </summary>
    public async Task<bool> IsStorageLimitExceededAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null || !tenant.StorageLimit.HasValue)
        {
            return false;
        }

        var totalStorage = await _dbContext.GetClient().Queryable<SysFile>()
            .Where(f => f.TenantId == tenantId)
            .SumAsync(f => f.FileSize);

        // 转换为 MB
        var storageMB = totalStorage / (1024 * 1024);

        return storageMB >= tenant.StorageLimit.Value;
    }

    /// <summary>
    /// 初始化租户数据
    /// </summary>
    public async Task InitializeTenantDataAsync(SysTenant tenant, CancellationToken cancellationToken = default)
    {
        // TODO: 实现租户数据初始化逻辑
        // 1. 创建默认角色
        // 2. 创建默认权限
        // 3. 创建默认菜单
        // 4. 创建默认配置
        await Task.CompletedTask;
    }

    /// <summary>
    /// 验证租户数据库配置
    /// </summary>
    public bool ValidateDatabaseConfiguration(SysTenant tenant)
    {
        if (tenant == null)
        {
            return false;
        }

        // 如果使用字段隔离模式，不需要数据库配置
        if (tenant.IsolationMode == TenantIsolationMode.Field)
        {
            return true;
        }

        // 如果使用Schema或Database隔离模式，需要数据库配置
        if (tenant.IsolationMode == TenantIsolationMode.Schema || tenant.IsolationMode == TenantIsolationMode.Database)
        {
            if (!tenant.DatabaseType.HasValue)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(tenant.DatabaseHost))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(tenant.DatabaseName))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 生成租户连接字符串
    /// </summary>
    public string GenerateConnectionString(SysTenant tenant)
    {
        if (tenant == null || !tenant.DatabaseType.HasValue)
        {
            throw new ArgumentNullException(nameof(tenant));
        }

        // 如果已配置连接字符串，直接返回
        if (!string.IsNullOrWhiteSpace(tenant.ConnectionString))
        {
            return tenant.ConnectionString;
        }

        var builder = new StringBuilder();

        switch (tenant.DatabaseType.Value)
        {
            case TenantDatabaseType.MySql:
                builder.Append($"Server={tenant.DatabaseHost};");
                if (tenant.DatabasePort.HasValue)
                {
                    builder.Append($"Port={tenant.DatabasePort.Value};");
                }
                builder.Append($"Database={tenant.DatabaseName};");
                builder.Append($"Uid={tenant.DatabaseUser};");
                builder.Append($"Pwd={tenant.DatabasePassword};");
                builder.Append("CharSet=utf8mb4;");
                break;

            case TenantDatabaseType.SqlServer:
                builder.Append($"Server={tenant.DatabaseHost}");
                if (tenant.DatabasePort.HasValue)
                {
                    builder.Append($",{tenant.DatabasePort.Value}");
                }
                builder.Append(";");
                builder.Append($"Database={tenant.DatabaseName};");
                builder.Append($"User Id={tenant.DatabaseUser};");
                builder.Append($"Password={tenant.DatabasePassword};");
                break;

            case TenantDatabaseType.PostgreSql:
                builder.Append($"Host={tenant.DatabaseHost};");
                if (tenant.DatabasePort.HasValue)
                {
                    builder.Append($"Port={tenant.DatabasePort.Value};");
                }
                builder.Append($"Database={tenant.DatabaseName};");
                builder.Append($"Username={tenant.DatabaseUser};");
                builder.Append($"Password={tenant.DatabasePassword};");
                break;

            default:
                throw new NotSupportedException($"不支持的数据库类型：{tenant.DatabaseType.Value}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// 检查租户编码是否重复
    /// </summary>
    public async Task<bool> IsTenantCodeDuplicateAsync(string tenantCode, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        return await _tenantRepository.IsTenantCodeExistsAsync(tenantCode, excludeTenantId, cancellationToken);
    }

    /// <summary>
    /// 检查域名是否重复
    /// </summary>
    public async Task<bool> IsDomainDuplicateAsync(string domain, long? excludeTenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysTenant>()
            .Where(t => t.Domain == domain);

        if (excludeTenantId.HasValue)
        {
            query = query.Where(t => t.BasicId != excludeTenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}

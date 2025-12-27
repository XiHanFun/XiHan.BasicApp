#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysConfigRepository
// Guid:a7b2c3d4-e5f6-7890-abcd-ef1234567896
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统配置仓储接口
/// </summary>
public interface ISysConfigRepository : IRepositoryBase<SysConfig, XiHanBasicAppIdType>
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <returns></returns>
    Task<SysConfig?> GetByKeyAsync(string configKey);

    /// <summary>
    /// 根据配置类型获取配置列表
    /// </summary>
    /// <param name="configType">配置类型</param>
    /// <returns></returns>
    Task<List<SysConfig>> GetByTypeAsync(ConfigType configType);

    /// <summary>
    /// 根据租户ID获取配置列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<SysConfig>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="excludeId">排除的配置ID</param>
    /// <returns></returns>
    Task<bool> ExistsByKeyAsync(string configKey, XiHanBasicAppIdType? excludeId = null);
}

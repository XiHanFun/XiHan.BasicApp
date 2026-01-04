#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysConfigService
// Guid:o1p2q3r4-s5t6-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 17:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Services.Configs.Dtos;
using XiHan.Framework.Application.Services.Abstracts;

namespace XiHan.BasicApp.Rbac.Services.Configs;

/// <summary>
/// 系统配置服务接口
/// </summary>
public interface ISysConfigService : ICrudApplicationService<ConfigDto, XiHanBasicAppIdType, CreateConfigDto, UpdateConfigDto>
{
    /// <summary>
    /// 根据配置键获取配置
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <returns></returns>
    Task<ConfigDto?> GetByKeyAsync(string configKey);

    /// <summary>
    /// 根据配置类型获取配置列表
    /// </summary>
    /// <param name="configType">配置类型</param>
    /// <returns></returns>
    Task<List<ConfigDto>> GetByTypeAsync(ConfigType configType);

    /// <summary>
    /// 根据租户ID获取配置列表
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <returns></returns>
    Task<List<ConfigDto>> GetByTenantIdAsync(XiHanBasicAppIdType tenantId);

    /// <summary>
    /// 检查配置键是否存在
    /// </summary>
    /// <param name="configKey">配置键</param>
    /// <param name="excludeId">排除的配置ID</param>
    /// <returns></returns>
    Task<bool> ExistsByKeyAsync(string configKey, XiHanBasicAppIdType? excludeId = null);
}

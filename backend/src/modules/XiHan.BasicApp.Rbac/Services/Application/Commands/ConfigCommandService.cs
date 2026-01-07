#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigCommandService
// Guid:b5c6d7e8-f9a0-4b5c-1d2e-4f5a6b7c8d9e
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
/// 配置命令服务（处理配置的写操作）
/// </summary>
public class ConfigCommandService : CrudApplicationServiceBase<SysConfig, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigCommandService(IConfigRepository configRepository)
        : base(configRepository)
    {
        _configRepository = configRepository;
    }

    /// <summary>
    /// 创建配置（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        var exists = await _configRepository.ExistsByConfigKeyAsync(input.ConfigKey);
        if (exists)
        {
            throw new InvalidOperationException($"配置键 {input.ConfigKey} 已存在");
        }

        // 2. 映射并创建
        var config = input.Adapt<SysConfig>();

        // 3. 保存
        config = await _configRepository.AddAsync(config);

        return await MapToEntityDtoAsync(config);
    }

    /// <summary>
    /// 更新配置（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取配置
        var config = await _configRepository.GetByIdAsync(id);
        if (config == null)
        {
            throw new KeyNotFoundException($"配置 {id} 不存在");
        }

        // 2. 业务验证
        if (config.ConfigKey != input.ConfigKey)
        {
            var exists = await _configRepository.ExistsByConfigKeyAsync(input.ConfigKey, id);
            if (exists)
            {
                throw new InvalidOperationException($"配置键 {input.ConfigKey} 已存在");
            }
        }

        // 3. 更新实体
        input.Adapt(config);

        // 4. 保存
        config = await _configRepository.UpdateAsync(config);

        return await MapToEntityDtoAsync(config);
    }

    /// <summary>
    /// 批量更新配置
    /// </summary>
    /// <param name="configs">配置字典（Key-Value）</param>
    /// <returns>是否成功</returns>
    public async Task<bool> BatchUpdateAsync(Dictionary<string, string> configs)
    {
        return await _configRepository.BatchUpdateAsync(configs);
    }
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictCommandService
// Guid:f3a4b5c6-d7e8-4f5a-9b0c-2d3e4f5a6b7c
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
/// 字典命令服务（处理字典的写操作）
/// </summary>
public class DictCommandService : CrudApplicationServiceBase<SysDict, RbacDtoBase, long, RbacDtoBase, RbacDtoBase>
{
    private readonly IDictRepository _dictRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictCommandService(IDictRepository dictRepository)
        : base(dictRepository)
    {
        _dictRepository = dictRepository;
    }

    /// <summary>
    /// 创建字典（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> CreateAsync(RbacDtoBase input)
    {
        // 1. 业务验证
        var exists = await _dictRepository.ExistsByDictCodeAsync(input.DictCode);
        if (exists)
        {
            throw new InvalidOperationException($"字典编码 {input.DictCode} 已存在");
        }

        // 2. 映射并创建
        var dict = input.Adapt<SysDict>();

        // 3. 保存
        dict = await _dictRepository.AddAsync(dict);

        return await MapToEntityDtoAsync(dict);
    }

    /// <summary>
    /// 更新字典（重写以添加业务逻辑）
    /// </summary>
    public override async Task<RbacDtoBase> UpdateAsync(long id, RbacDtoBase input)
    {
        // 1. 获取字典
        var dict = await _dictRepository.GetByIdAsync(id);
        if (dict == null)
        {
            throw new KeyNotFoundException($"字典 {id} 不存在");
        }

        // 2. 业务验证
        if (dict.DictCode != input.DictCode)
        {
            var exists = await _dictRepository.ExistsByDictCodeAsync(input.DictCode, id);
            if (exists)
            {
                throw new InvalidOperationException($"字典编码 {input.DictCode} 已存在");
            }
        }

        // 3. 更新实体
        input.Adapt(dict);

        // 4. 保存
        dict = await _dictRepository.UpdateAsync(dict);

        return await MapToEntityDtoAsync(dict);
    }

    /// <summary>
    /// 更新字典状态
    /// </summary>
    /// <param name="dictId">字典ID</param>
    /// <param name="status">状态</param>
    /// <returns>是否成功</returns>
    public async Task<bool> UpdateStatusAsync(long dictId, Enums.YesOrNo status)
    {
        var dict = await _dictRepository.GetByIdAsync(dictId);
        if (dict == null)
        {
            throw new KeyNotFoundException($"字典 {dictId} 不存在");
        }

        dict.Status = status;
        await _dictRepository.UpdateAsync(dict);
        return true;
    }
}

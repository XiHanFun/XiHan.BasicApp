#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictQueryService
// Guid:a4b5c6d7-e8f9-4a5b-0c1d-3e4f5a6b7c8d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/7 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Domain.Paging.Dtos;

namespace XiHan.BasicApp.Rbac.Services.Application.Queries;

/// <summary>
/// 字典查询服务（处理字典的读操作 - CQRS）
/// </summary>
public class DictQueryService : ApplicationServiceBase
{
    private readonly IDictRepository _dictRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DictQueryService(IDictRepository dictRepository)
    {
        _dictRepository = dictRepository;
    }

    /// <summary>
    /// 根据ID获取字典
    /// </summary>
    /// <param name="id">字典ID</param>
    /// <returns>字典DTO</returns>
    public async Task<RbacDtoBase?> GetByIdAsync(long id)
    {
        var dict = await _dictRepository.GetByIdAsync(id);
        return dict?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据字典编码获取字典
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <returns>字典DTO（包含字典项）</returns>
    public async Task<RbacDtoBase?> GetByDictCodeAsync(string dictCode)
    {
        var dict = await _dictRepository.GetByDictCodeAsync(dictCode);
        return dict?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取字典及其所有字典项
    /// </summary>
    /// <param name="dictId">字典ID</param>
    /// <returns>字典DTO（包含字典项）</returns>
    public async Task<RbacDtoBase?> GetWithItemsAsync(long dictId)
    {
        var dict = await _dictRepository.GetWithItemsAsync(dictId);
        return dict?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 根据字典编码获取字典及其所有字典项
    /// </summary>
    /// <param name="dictCode">字典编码</param>
    /// <returns>字典DTO（包含字典项）</returns>
    public async Task<RbacDtoBase?> GetWithItemsByCodeAsync(string dictCode)
    {
        var dict = await _dictRepository.GetWithItemsByCodeAsync(dictCode);
        return dict?.Adapt<RbacDtoBase>();
    }

    /// <summary>
    /// 获取分页列表
    /// </summary>
    /// <param name="input">分页查询参数</param>
    /// <returns>分页响应</returns>
    public async Task<PageResponse<RbacDtoBase>> GetPagedAsync(PageQuery input)
    {
        var result = await _dictRepository.GetPagedAsync(input);
        var dtos = result.Items.Adapt<List<RbacDtoBase>>();
        return new PageResponse<RbacDtoBase>(dtos, result.PageData);
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户数据范围查询应用服务接口
/// </summary>
public interface IUserDataScopeQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户数据范围列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回当前有效数据范围覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围列表</returns>
    Task<IReadOnlyList<UserDataScopeListItemDto>> GetUserDataScopesAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户数据范围详情
    /// </summary>
    /// <param name="id">用户数据范围主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    Task<UserDataScopeDetailDto?> GetUserDataScopeDetailAsync(long id, CancellationToken cancellationToken = default);
}

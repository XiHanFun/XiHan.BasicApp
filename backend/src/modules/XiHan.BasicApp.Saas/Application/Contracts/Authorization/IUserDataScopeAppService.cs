// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户数据范围命令应用服务接口
/// </summary>
public interface IUserDataScopeAppService : IApplicationService
{
    /// <summary>
    /// 设置成员在本租户的数据范围：覆盖档位与自定义部门一次提交（单事务）
    /// </summary>
    /// <param name="input">设置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task SetUserDataScopeAsync(UserDataScopeSetDto input, CancellationToken cancellationToken = default);
}

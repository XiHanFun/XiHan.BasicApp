// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 当前用户字段脱敏规则下发应用服务接口
/// </summary>
public interface IMyFieldSecurityAppService : IApplicationService
{
    /// <summary>
    /// 获取当前用户在指定资源上的有效字段脱敏规则（无规则返回空，前端不脱敏）
    /// </summary>
    Task<List<MyFieldSecurityRuleDto>> GetMineAsync(string resourceCode, CancellationToken cancellationToken = default);
}

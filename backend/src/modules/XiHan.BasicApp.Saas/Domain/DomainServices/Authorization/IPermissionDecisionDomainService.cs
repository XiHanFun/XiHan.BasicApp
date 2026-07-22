// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.ValueObjects;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限裁决领域服务
/// </summary>
public interface IPermissionDecisionDomainService
{
    /// <summary>
    /// 根据授权快照裁决指定权限
    /// </summary>
    /// <param name="permissionCode">权限编码</param>
    /// <param name="grants">授权快照集合</param>
    /// <param name="now">当前时间</param>
    /// <returns>裁决结果</returns>
    AuthorizationDecision Decide(string permissionCode, IEnumerable<PermissionGrantSnapshot> grants, DateTimeOffset now);
}

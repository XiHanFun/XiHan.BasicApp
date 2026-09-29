// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 按实际发生的操作校验权限的守卫。
/// </summary>
/// <remarks>
/// 批量变更接口一次提交授予、撤销、启停等多种操作。方法上叠多个 <c>[PermissionAuthorize]</c> 是「全部都要」，
/// 只想加人也得同时持有撤销权限，而入口按钮只能挂一个权限码，于是看得到按钮、一保存就被拒。
/// 批量接口改为：特性只门控入口，本次请求里真的出现的操作再逐项经本守卫校验对应权限。
/// </remarks>
public interface IOperationPermissionGuard
{
    /// <summary>
    /// 校验当前用户持有指定权限，缺失时抛出友好异常（提示权限显示名）。
    /// </summary>
    /// <param name="permissionCode">权限码</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureGrantedAsync(string permissionCode, CancellationToken cancellationToken = default);
}

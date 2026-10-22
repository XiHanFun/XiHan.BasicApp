// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Http;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 条件属性的取值上下文：求值时的主体、租户、请求与时刻
/// </summary>
/// <remarks>
/// 当前请求用 <see cref="ISaasConditionAttributeSourceFactory.CreateCurrentContext"/> 生成；
/// 登录时的检查、试算等主体不是当前用户的场景，由调用方填好主体与租户。
/// </remarks>
public sealed record SaasConditionContext
{
    /// <summary>
    /// 租户主键，0 为平台
    /// </summary>
    public required long TenantId { get; init; }

    /// <summary>
    /// 求值时刻
    /// </summary>
    public required DateTimeOffset Now { get; init; }

    /// <summary>
    /// 用户主键，未登录为空
    /// </summary>
    public long? UserId { get; init; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; init; }

    /// <summary>
    /// 是否处于模仿登录
    /// </summary>
    public bool IsImpersonating { get; init; }

    /// <summary>
    /// 请求上下文；没有请求（后台、试算）时为空，请求类属性一律取不到
    /// </summary>
    public HttpContext? HttpContext { get; init; }

    /// <summary>
    /// 客户端 IP；为空时取 <see cref="HttpContext"/> 的 <c>Connection.RemoteIpAddress</c>
    /// </summary>
    public IPAddress? RemoteIpAddress { get; init; }

    /// <summary>
    /// 端点权限码；为空时取 <see cref="HttpContext"/> 端点上声明的 <c>[PermissionAuthorize]</c>
    /// </summary>
    public IReadOnlyCollection<string>? PermissionCodes { get; init; }
}

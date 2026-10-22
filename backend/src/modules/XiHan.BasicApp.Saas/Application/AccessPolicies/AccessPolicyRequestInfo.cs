// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 命中时的请求信息
/// </summary>
/// <param name="TraceId">链路追踪 ID</param>
/// <param name="Ip">客户端 IP</param>
/// <param name="UserId">用户主键</param>
/// <param name="UserName">用户名</param>
/// <param name="Method">请求方法</param>
/// <param name="Path">请求路径</param>
/// <param name="PermissionCodes">端点权限码，逗号分隔</param>
/// <param name="UserAgent">User-Agent</param>
/// <param name="ClientKind">客户端类型</param>
public sealed record AccessPolicyRequestInfo(
    string? TraceId,
    string? Ip,
    long? UserId,
    string? UserName,
    string? Method,
    string? Path,
    string? PermissionCodes,
    string? UserAgent,
    string? ClientKind);

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 登录会话签发结果
/// </summary>
/// <param name="Session">用户会话</param>
/// <param name="SupersededSessionBusinessIds">同设备重新登录被自动下线的旧会话业务标识（调用方需失效其闸门缓存）</param>
public sealed record LoginSessionIssueResult(SysUserSession Session, IReadOnlyList<string> SupersededSessionBusinessIds);

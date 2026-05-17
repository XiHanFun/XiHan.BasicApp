#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LoginSessionIssueResult
// Guid:79d77a8f-777d-4e9e-ae94-8ae2fcd0b2fa
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 登录会话签发结果
/// </summary>
/// <param name="Session">用户会话</param>
public sealed record LoginSessionIssueResult(SysUserSession Session);

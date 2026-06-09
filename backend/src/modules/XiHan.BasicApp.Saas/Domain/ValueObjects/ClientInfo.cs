#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ClientInfo
// Guid:ccf12693-2689-4eca-a2b7-2b095b0f72a3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 客户端信息
/// </summary>
public sealed record ClientInfo(string? Ip, string? Location, string? UserAgent, string? Browser, string? Os);

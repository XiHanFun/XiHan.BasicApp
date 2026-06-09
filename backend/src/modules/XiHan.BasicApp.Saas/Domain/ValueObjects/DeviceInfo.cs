#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DeviceInfo
// Guid:cd038410-1c51-4efd-b740-90965872796d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 设备信息
/// </summary>
public sealed record DeviceInfo(string? DeviceType, string? DeviceName, string? DeviceId, string? Os, string? Browser);

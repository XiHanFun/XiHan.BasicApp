// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 设备信息
/// </summary>
public sealed record DeviceInfo(string? DeviceType, string? DeviceName, string? DeviceId, string? Os, string? Browser);

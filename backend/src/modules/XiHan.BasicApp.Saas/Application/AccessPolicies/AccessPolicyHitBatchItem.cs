// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 合并好的命中：这一分钟里的第一次命中与次数
/// </summary>
/// <param name="First">第一次命中</param>
/// <param name="Count">次数</param>
public sealed record AccessPolicyHitBatchItem(AccessPolicyHitEntry First, int Count);

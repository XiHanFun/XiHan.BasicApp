// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Conditions;

/// <summary>
/// 访问策略模板
/// </summary>
/// <param name="Code">模板编码，也是平台预置策略的编码</param>
/// <param name="Name">名称</param>
/// <param name="Description">说明</param>
/// <param name="Action">平台预置策略写入时的动作</param>
/// <param name="PlatformStatus">平台预置策略写入时的状态</param>
/// <param name="Priority">优先级</param>
/// <param name="Conditions">条件</param>
public sealed record AccessPolicyTemplate(
    string Code,
    string Name,
    string Description,
    AccessPolicyAction Action,
    EnableStatus PlatformStatus,
    int Priority,
    IReadOnlyList<AccessPolicyConditionCommand> Conditions);

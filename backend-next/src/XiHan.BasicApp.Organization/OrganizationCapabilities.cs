// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织包提供的能力。
/// </summary>
public static class OrganizationCapabilities
{
    /// <summary>
    /// 组织能力：部门、岗位、用户部门归属。
    /// </summary>
    public const string Organization = AppInfo.CapabilityPrefix + "organization";

    /// <summary>
    /// 能力版本。
    /// </summary>
    public const int Version = 1;
}

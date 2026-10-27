// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织包的错误码：<c>ORGANIZATION_&lt;KIND&gt;_&lt;NNN&gt;</c>。
/// </summary>
public static class OrganizationErrors
{
    /// <summary>
    /// 部门不存在（或不在当前租户）。
    /// </summary>
    public const string DepartmentNotFound = "ORGANIZATION_NOTFOUND_001";

    /// <summary>
    /// 岗位不存在（或不在当前租户）。
    /// </summary>
    public const string PositionNotFound = "ORGANIZATION_NOTFOUND_002";

    /// <summary>
    /// 归属记录不存在。
    /// </summary>
    public const string MembershipNotFound = "ORGANIZATION_NOTFOUND_003";

    /// <summary>
    /// 部门编码在当前租户内已被占用。
    /// </summary>
    public const string DepartmentCodeTaken = "ORGANIZATION_CONFLICT_001";

    /// <summary>
    /// 部门还有子部门或成员，不能删除。
    /// </summary>
    public const string DepartmentInUse = "ORGANIZATION_CONFLICT_002";

    /// <summary>
    /// 岗位编码在当前租户内已被占用。
    /// </summary>
    public const string PositionCodeTaken = "ORGANIZATION_CONFLICT_003";

    /// <summary>
    /// 岗位还被归属记录引用，不能删除。
    /// </summary>
    public const string PositionInUse = "ORGANIZATION_CONFLICT_004";

    /// <summary>
    /// 用户已在该部门。
    /// </summary>
    public const string MembershipExists = "ORGANIZATION_CONFLICT_005";

    /// <summary>
    /// 用户不属于当前租户（注册地不同）。
    /// </summary>
    public const string UserNotInTenant = "ORGANIZATION_VALIDATION_001";

    /// <summary>
    /// 把部门移到自己或自己的子孙下面。
    /// </summary>
    public const string CyclicMove = "ORGANIZATION_VALIDATION_002";

    /// <summary>
    /// 列表查询的筛选或排序字段不支持。
    /// </summary>
    public const string QueryUnsupported = "ORGANIZATION_VALIDATION_003";

    /// <summary>
    /// 组织只在租户侧管理：没有租户上下文的调用被拒。
    /// </summary>
    public const string TenantOnly = "ORGANIZATION_DENIED_001";
}

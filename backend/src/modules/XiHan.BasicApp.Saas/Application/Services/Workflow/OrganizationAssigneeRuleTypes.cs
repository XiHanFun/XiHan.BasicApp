// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 组织架构受理人规则类型（工作流模块基于 Saas 组织数据注册的解析器）
/// </summary>
/// <remarks>
/// 与框架内置的 User / Starter / Expression 并列。角色、部门、岗位按「编码」引用（编码创建后不可改），
/// 流程定义因此可在租户间复用，默认流程模板也能直接引用系统角色。
/// 业务的默认流程模板可直接引用这些类型；解析器由工作流模块注册，前端设计器的规则目录与本类一一对应。
/// </remarks>
public static class OrganizationAssigneeRuleTypes
{
    /// <summary>
    /// 角色成员（参数为角色编码列表；只取启用角色里有效期内的成员）
    /// </summary>
    public const string Role = "Role";

    /// <summary>
    /// 部门成员（参数为部门编码列表；只取启用部门的有效成员，不含下级部门）
    /// </summary>
    public const string Department = "Department";

    /// <summary>
    /// 部门负责人（参数为部门编码列表）
    /// </summary>
    public const string DepartmentLeader = "DepartmentLeader";

    /// <summary>
    /// 岗位成员（参数为岗位编码列表；按用户部门任职上的岗位匹配）
    /// </summary>
    public const string Position = "Position";

    /// <summary>
    /// 发起人的第 N 级主管（参数为层级，缺省 1 即直属主管）
    /// </summary>
    /// <remarks>
    /// 从发起人的主部门（无主部门取任一有效任职）起沿上级部门逐级找负责人；
    /// 发起人自己是某部门负责人时跳过该部门，同一人连续负责多级时只算一级。
    /// </remarks>
    public const string StarterLeader = "StarterLeader";

    /// <summary>
    /// 发起人的逐级主管链（参数为最多层级，缺省 1；配合依次审批实现「连续多级主管」）
    /// </summary>
    public const string StarterLeaderChain = "StarterLeaderChain";
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Organization.Domain.Entities;

/// <summary>
/// 部门类型
/// </summary>
/// <remarks>
/// 名称与 4.x 相同。部门是缺省值、排在 0，加列时已有的行读出来就是它（报文按名称，数值只在库里）
/// </remarks>
public enum DepartmentType
{
    /// <summary>
    /// 部门
    /// </summary>
    Department = 0,

    /// <summary>
    /// 集团
    /// </summary>
    Corporation = 1,

    /// <summary>
    /// 总部
    /// </summary>
    Headquarters = 2,

    /// <summary>
    /// 公司
    /// </summary>
    Company = 3,

    /// <summary>
    /// 分公司
    /// </summary>
    Branch = 4,

    /// <summary>
    /// 事业部
    /// </summary>
    Division = 5,

    /// <summary>
    /// 中心
    /// </summary>
    Center = 6,

    /// <summary>
    /// 科室
    /// </summary>
    Section = 7,

    /// <summary>
    /// 团队
    /// </summary>
    Team = 8,

    /// <summary>
    /// 小组
    /// </summary>
    Group = 9,

    /// <summary>
    /// 项目组
    /// </summary>
    Project = 10,

    /// <summary>
    /// 工作组
    /// </summary>
    Workgroup = 11,

    /// <summary>
    /// 虚拟组织
    /// </summary>
    Virtual = 12,

    /// <summary>
    /// 办事处
    /// </summary>
    Office = 13,

    /// <summary>
    /// 子公司
    /// </summary>
    Subsidiary = 14,

    /// <summary>
    /// 其他
    /// </summary>
    Other = 99,
}

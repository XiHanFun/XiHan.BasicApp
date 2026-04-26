#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartment.Enum
// Guid:4ed2ca6e-d8e6-4528-a3f6-ed45a89ecbe6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 部门类型枚举
/// </summary>
public enum DepartmentType
{
    /// <summary>
    /// 集团（最高级别的组织单位）
    /// </summary>
    [Description("集团（最高级别的组织单位）")]
    Corporation = 0,

    /// <summary>
    /// 总公司/总部
    /// </summary>
    [Description("总公司/总部")]
    Headquarters = 1,

    /// <summary>
    /// 公司
    /// </summary>
    [Description("公司")]
    Company = 2,

    /// <summary>
    /// 分公司
    /// </summary>
    [Description("分公司")]
    Branch = 3,

    /// <summary>
    /// 事业部
    /// </summary>
    [Description("事业部")]
    Division = 4,

    /// <summary>
    /// 中心
    /// </summary>
    [Description("中心")]
    Center = 5,

    /// <summary>
    /// 部门
    /// </summary>
    [Description("部门")]
    Department = 6,

    /// <summary>
    /// 科室
    /// </summary>
    [Description("科室")]
    Section = 7,

    /// <summary>
    /// 团队
    /// </summary>
    [Description("团队")]
    Team = 8,

    /// <summary>
    /// 小组
    /// </summary>
    [Description("小组")]
    Group = 9,

    /// <summary>
    /// 项目组
    /// </summary>
    [Description("项目组")]
    Project = 10,

    /// <summary>
    /// 工作组
    /// </summary>
    [Description("工作组")]
    Workgroup = 11,

    /// <summary>
    /// 虚拟组织（跨部门协作组织）
    /// </summary>
    [Description("虚拟组织（跨部门协作组织）")]
    Virtual = 12,

    /// <summary>
    /// 办事处
    /// </summary>
    [Description("办事处")]
    Office = 13,

    /// <summary>
    /// 子公司
    /// </summary>
    [Description("子公司")]
    Subsidiary = 14,

    /// <summary>
    /// 其他
    /// </summary>
    [Description("其他")]
    Other = 99
}

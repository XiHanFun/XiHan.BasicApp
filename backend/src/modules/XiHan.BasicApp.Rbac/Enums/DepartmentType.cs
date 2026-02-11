#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentType
// Guid:cd28152c-d6e9-4396-addb-b479254bad32
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 部门类型枚举
/// </summary>
public enum DepartmentType
{
    /// <summary>
    /// 集团（最高级别的组织单位）
    /// </summary>
    Corporation = 0,

    /// <summary>
    /// 总公司/总部
    /// </summary>
    Headquarters = 1,

    /// <summary>
    /// 公司
    /// </summary>
    Company = 2,

    /// <summary>
    /// 分公司
    /// </summary>
    Branch = 3,

    /// <summary>
    /// 事业部
    /// </summary>
    Division = 4,

    /// <summary>
    /// 中心
    /// </summary>
    Center = 5,

    /// <summary>
    /// 部门
    /// </summary>
    Department = 6,

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
    /// 虚拟组织（跨部门协作组织）
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
    Other = 99
}

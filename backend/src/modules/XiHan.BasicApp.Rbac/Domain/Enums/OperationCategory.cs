#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationCategory
// Guid:fbf21541-6651-44e6-aa76-2f0a981f79e2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Domain.Enums;

/// <summary>
/// 操作分类枚举
/// </summary>
public enum OperationCategory
{
    /// <summary>
    /// CRUD 操作（基础增删改查）
    /// </summary>
    Crud = 0,

    /// <summary>
    /// 业务操作（审批、提交等）
    /// </summary>
    Business = 1,

    /// <summary>
    /// 管理操作（授权、配置等）
    /// </summary>
    Admin = 2,

    /// <summary>
    /// 系统操作（导入导出、备份等）
    /// </summary>
    System = 3,

    /// <summary>
    /// 自定义操作
    /// </summary>
    Custom = 99
}

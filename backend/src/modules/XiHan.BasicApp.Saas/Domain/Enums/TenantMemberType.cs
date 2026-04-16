#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMemberType
// Guid:d0e1f2a3-4567-8901-abcd-ef4567890123
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/17 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 租户成员类型枚举
/// 描述用户在某租户中的身份角色，用于区分主属成员 / 外部协作者 / 平台管理员
/// </summary>
public enum TenantMemberType
{
    /// <summary>
    /// 租户所有者：通常是租户创建者/购买订阅的法人代表，拥有最高权限
    /// </summary>
    Owner = 0,

    /// <summary>
    /// 租户管理员：次级管理权限，负责用户/角色/部门等日常管理
    /// </summary>
    Admin = 1,

    /// <summary>
    /// 普通成员：租户内正式员工，按分配的角色拥有权限
    /// </summary>
    Member = 2,

    /// <summary>
    /// 外部协作者：供应商/客户等外部组织成员，被邀请访问特定资源
    /// </summary>
    External = 3,

    /// <summary>
    /// 访客：短期临时访问（如演示/试用），通常受限于只读权限与时效
    /// </summary>
    Guest = 4,

    /// <summary>
    /// 顾问/审计师：跨租户只读访问，用于咨询/审计/合规检查
    /// </summary>
    Consultant = 5,

    /// <summary>
    /// 平台管理员：平台运营方，可跨租户访问以进行运维/客服/风控
    /// </summary>
    PlatformAdmin = 99
}

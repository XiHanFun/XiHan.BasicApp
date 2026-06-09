#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserManagementDetailDto
// Guid:42580264-d9a7-4a6f-a68c-fd0c5c1bc039
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/07 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 账号管理详情聚合 DTO
/// </summary>
public sealed class UserManagementDetailDto
{
    /// <summary>
    /// 用户基础详情
    /// </summary>
    public UserDetailDto User { get; set; } = new();

    /// <summary>
    /// 当前租户成员信息
    /// </summary>
    public TenantMemberListItemDto? TenantMembership { get; set; }

    /// <summary>
    /// 用户部门归属
    /// </summary>
    public List<UserDepartmentListItemDto> Departments { get; set; } = [];

    /// <summary>
    /// 用户角色授权
    /// </summary>
    public List<UserRoleListItemDto> Roles { get; set; } = [];

    /// <summary>
    /// 用户直授权限
    /// </summary>
    public List<UserPermissionListItemDto> Permissions { get; set; } = [];

    /// <summary>
    /// 用户数据范围覆盖
    /// </summary>
    public List<UserDataScopeListItemDto> DataScopes { get; set; } = [];

    /// <summary>
    /// 用户安全设置摘要
    /// </summary>
    public UserSecurityDetailDto? Security { get; set; }

    /// <summary>
    /// 最近登录会话
    /// </summary>
    public List<UserSessionListItemDto> Sessions { get; set; } = [];

    /// <summary>
    /// 最近用户统计
    /// </summary>
    public List<UserStatisticsDetailDto> Statistics { get; set; } = [];

    /// <summary>
    /// 第三方登录绑定
    /// </summary>
    public List<ExternalLoginListItemDto> ExternalLogins { get; set; } = [];

    /// <summary>
    /// 最近密码历史
    /// </summary>
    public List<PasswordHistoryListItemDto> PasswordHistories { get; set; } = [];

    /// <summary>
    /// 生成时间
    /// </summary>
    public DateTimeOffset GeneratedTime { get; set; }
}

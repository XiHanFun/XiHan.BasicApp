#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthorizationSnapshotQueryService
// Guid:ef916398-6b0c-4a08-bf28-309ef83df655
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 授权快照查询服务
/// </summary>
public interface IAuthorizationSnapshotQueryService
{
    /// <summary>
    /// 构建用户授权快照
    /// </summary>
    Task<AuthorizationSnapshot> BuildAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}

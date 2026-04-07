#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthQueryService
// Guid:b1c2d3e4-f5a6-4789-0123-456789abcdef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 认证权限查询服务接口
/// </summary>
public interface IAuthQueryService : IQueryService
{
    /// <summary>
    /// 获取用户权限编码列表（用于认证与授权）
    /// </summary>
    Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long userId, long? tenantId = null);
}

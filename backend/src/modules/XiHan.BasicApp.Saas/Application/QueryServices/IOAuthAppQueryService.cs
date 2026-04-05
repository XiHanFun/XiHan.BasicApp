#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthAppQueryService
// Guid:6a7b8c9d-0e1f-4345-f012-610000000001
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// OAuth 应用查询服务接口
/// </summary>
public interface IOAuthAppQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取 OAuth 应用
    /// </summary>
    Task<OAuthAppDto?> GetByIdAsync(long id);
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthAppDomainService
// Guid:6a7b8c9d-0e1f-4345-f012-630000000003
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// OAuth 应用领域服务接口
/// </summary>
public interface IOAuthAppDomainService
{
    /// <summary>
    /// 创建 OAuth 应用
    /// </summary>
    Task<SysOAuthApp> CreateAsync(SysOAuthApp oauthApp);

    /// <summary>
    /// 更新 OAuth 应用
    /// </summary>
    Task<SysOAuthApp> UpdateAsync(SysOAuthApp oauthApp);

    /// <summary>
    /// 删除 OAuth 应用
    /// </summary>
    Task<bool> DeleteAsync(long id);
}

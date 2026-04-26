#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOAuthCodeRepository
// Guid:d0e2bc8f-9a38-45d0-ab5c-3d7259463518
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// OAuth 授权码仓储接口
/// </summary>
public interface IOAuthCodeRepository : ISaasRepository<SysOAuthCode>
{
}

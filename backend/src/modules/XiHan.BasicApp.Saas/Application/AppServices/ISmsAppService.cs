#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsAppService
// Guid:fcfceec1-4a68-4a83-9738-ae8f217f5d95
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 12:38:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 短信应用服务
/// </summary>
public interface ISmsAppService
    : ICrudApplicationService<SmsDto, long, SmsCreateDto, SmsUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 获取待发送短信
    /// </summary>
    Task<IReadOnlyList<SmsDto>> GetPendingAsync(int maxCount = 100, long? tenantId = null);
}

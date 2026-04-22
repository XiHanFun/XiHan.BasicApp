#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldLevelSecurityAppService
// Guid:fedcba98-7654-3210-fedc-ba9876543210
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/22 18:33:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 字段级安全应用服务
/// </summary>
public interface IFieldLevelSecurityAppService
    : ICrudApplicationService<FieldLevelSecurityDto, long, FieldLevelSecurityCreateDto, FieldLevelSecurityUpdateDto, BasicAppPRDto>
{
}

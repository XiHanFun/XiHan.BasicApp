#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConstraintRuleAppService
// Guid:52b6655b-37e3-4df3-868c-84f379a5e8fd
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 约束规则应用服务
/// </summary>
public interface IConstraintRuleAppService
    : ICrudApplicationService<ConstraintRuleDto, long, ConstraintRuleCreateDto, ConstraintRuleUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据规则编码获取规则
    /// </summary>
    /// <param name="ruleCode"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<ConstraintRuleDto?> GetRuleByCodeAsync(string ruleCode, long? tenantId = null);
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IConstraintRuleAppService
// Guid:0c343b84-a9c1-4ac9-a0ff-2c3b7f208897
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 约束规则命令应用服务接口
/// </summary>
public interface IConstraintRuleAppService : IApplicationService
{
    /// <summary>
    /// 创建约束规则
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    Task<ConstraintRuleDetailDto> CreateConstraintRuleAsync(ConstraintRuleCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新约束规则
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    Task<ConstraintRuleDetailDto> UpdateConstraintRuleAsync(ConstraintRuleUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新约束规则状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>约束规则详情</returns>
    Task<ConstraintRuleDetailDto> UpdateConstraintRuleStatusAsync(ConstraintRuleStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除约束规则
    /// </summary>
    /// <param name="id">约束规则主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteConstraintRuleAsync(long id, CancellationToken cancellationToken = default);
}

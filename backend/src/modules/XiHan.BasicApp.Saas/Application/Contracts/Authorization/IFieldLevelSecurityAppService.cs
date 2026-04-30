#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldLevelSecurityAppService
// Guid:ad28974f-7354-40a6-9bd5-67ec8d04150f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 字段级安全命令应用服务接口
/// </summary>
public interface IFieldLevelSecurityAppService : IApplicationService
{
    /// <summary>
    /// 创建字段级安全策略
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    Task<FieldLevelSecurityDetailDto> CreateFieldLevelSecurityAsync(FieldLevelSecurityCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityAsync(FieldLevelSecurityUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新字段级安全策略状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    Task<FieldLevelSecurityDetailDto> UpdateFieldLevelSecurityStatusAsync(FieldLevelSecurityStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除字段级安全策略
    /// </summary>
    /// <param name="id">字段级安全主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteFieldLevelSecurityAsync(long id, CancellationToken cancellationToken = default);
}

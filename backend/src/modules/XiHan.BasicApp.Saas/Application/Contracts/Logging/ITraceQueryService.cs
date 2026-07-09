#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITraceQueryService
// Guid:9d4b7ec6-5f81-4a40-b293-2d0e6f8b5a14
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/09 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 链路追踪查询应用服务接口
/// </summary>
public interface ITraceQueryService : IApplicationService
{
    /// <summary>
    /// 按维度跨多类日志聚合链路追踪时间线（时间倒序）
    /// </summary>
    /// <param name="input">追踪查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>链路追踪时间线结果</returns>
    Task<TraceTimelineResultDto> GetTraceTimelineAsync(TraceTimelineQueryDto input, CancellationToken cancellationToken = default);
}

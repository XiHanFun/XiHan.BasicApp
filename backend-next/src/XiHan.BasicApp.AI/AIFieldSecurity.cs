// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.AI.Domain.Entities;
using XiHan.BasicApp.AI.Operations.AiProviders;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.AI;

/// <summary>
/// AI 模块的字段安全登记
/// </summary>
/// <remarks>
/// 照 4.x（<c>AddAIFieldSecurityEntities</c>）：提供商（新建、修改、启停）登记字段安全，规则由 Access 的中间件与响应过滤器统一套用
/// </remarks>
public static class AIFieldSecurity
{
    /// <summary>
    /// 登记本模块的实体与操作
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("ai", AIJsonContext.Default, fields => fields
            .Entity<AiProviderResponse>("ai.ai-providers", "ai.providers.title", "ai.providers.fields.")
                .Reads(ListAiProviders.Id, GetAiProvider.Id)
                .Creates(CreateAiProvider.Id)
                .Updates<UpdateAiProviderRequest>(UpdateAiProvider.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysAiProvider>(context, request.Id, AiProviderResponse.From))
                .Updates<SetAiProviderStatusRequest>(SetAiProviderStatus.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysAiProvider>(context, request.Id, AiProviderResponse.From))
                .Keyword("code", "name", "chatModel"));
}

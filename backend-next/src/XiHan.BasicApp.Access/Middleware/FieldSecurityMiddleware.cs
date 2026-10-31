// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Infrastructure;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Middleware;

/// <summary>
/// Handle 阶段的字段安全中间件。
/// </summary>
/// <remarks>
/// 方案 5.9。对各包经 <see cref="FieldSecurityRegistrationExtensions.AddFieldSecurity"/> 绑定的操作，按当前主体在实体上的规则统一套用：
/// 读取时，读受保护的字段不能筛选、排序（<c>ACCESS_DENIED_006</c>），关键字会匹配到它也不能搜；新建时只读字段不能填，
/// 修改时只读字段不能改（<c>ACCESS_DENIED_007</c>），交回原值、脱敏值或空值即保留原值。响应的掩码在 <see cref="FieldSecurityBindings"/>（操作成功、编码之前）。
/// 没有规则的调用原样放行。排在工作单元之内，修改时取原记录与操作同一个事务。
/// </remarks>
/// <param name="bindings">各包登记的操作绑定。</param>
public sealed class FieldSecurityMiddleware(FieldSecurityBindings bindings) : IPipelineMiddleware
{

    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        if (!context.GetOperation().TryGetValue(out var operation) || bindings.Find(operation.Id) is not { } binding || context.Services.GetService<IFieldSecurity>() is not { } security)
        {
            await nextStep(context).ConfigureAwait(false);
            return;
        }

        var rules = await security.ResolveAsync(binding.Entity, context).ConfigureAwait(false);
        if (rules.IsEmpty)
        {
            await nextStep(context).ConfigureAwait(false);
            return;
        }

        if (await GuardAsync(context, operation, binding, rules).ConfigureAwait(false) is { } denied)
        {
            context.SetOutcome(denied);
            return;
        }

        await nextStep(context).ConfigureAwait(false);
    }

    private static async ValueTask<Error?> GuardAsync(InvocationContext context, OperationDescriptor operation, FieldSecurityBinding binding, FieldRules rules)
    {
        if (!context.GetOperationRequest().TryGetValue(out var request))
        {
            return null;
        }

        switch (binding.Use)
        {
            case FieldSecurityUse.Read when request is PageRequest page:
                if (rules.FirstProtected(page) is { } filtered)
                {
                    return Protected(filtered);
                }

                return !string.IsNullOrWhiteSpace(page.Keyword) && binding.KeywordFields.FirstOrDefault(rules.IsReadProtected) is { } searched
                    ? Error.Denied(AccessErrors.FieldProtected, AccessMessages.FieldKeywordProtected, MessageArguments.Of(("field", searched)))
                    : null;
            case FieldSecurityUse.Create:
                var created = FieldSecurityBindings.ToObject(request, operation.RequestType, binding);
                foreach (var rule in rules.All)
                {
                    if (created.TryGetPropertyValue(rule.Field, out var value) && rules.OnCreate(rule.Field, FieldSecurityBindings.TextOf(value)) == FieldWrite.Denied)
                    {
                        return ReadOnly(rule.Field);
                    }
                }

                return null;
            case FieldSecurityUse.Update when binding.Original is { } load:
                if (await load(request, context).ConfigureAwait(false) is not { } original)
                {
                    return null;
                }

                var incoming = FieldSecurityBindings.ToObject(request, operation.RequestType, binding);
                var current = FieldSecurityBindings.ToObject(original, original.GetType(), binding);
                var kept = false;
                foreach (var rule in rules.All)
                {
                    if (!incoming.TryGetPropertyValue(rule.Field, out var value) || !current.TryGetPropertyValue(rule.Field, out var before))
                    {
                        continue;
                    }

                    switch (rules.OnUpdate(rule.Field, FieldSecurityBindings.TextOf(before), FieldSecurityBindings.TextOf(value)))
                    {
                        case FieldWrite.Denied:
                            return ReadOnly(rule.Field);
                        case FieldWrite.Keep when !JsonNode.DeepEquals(value, before):
                            incoming[rule.Field] = before?.DeepClone();
                            kept = true;
                            break;
                    }
                }

                if (kept)
                {
                    context.SetOperationRequest(incoming.Deserialize(FieldSecurityBindings.TypeInfo(binding, operation.RequestType))!);
                }

                return null;
            default:
                return null;
        }
    }

    private static Error Protected(string field) => Error.Denied(AccessErrors.FieldProtected, AccessMessages.FieldProtected, MessageArguments.Of(("field", field)));

    private static Error ReadOnly(string field) => Error.Denied(AccessErrors.FieldReadOnly, AccessMessages.FieldReadOnly, MessageArguments.Of(("field", field)));
}

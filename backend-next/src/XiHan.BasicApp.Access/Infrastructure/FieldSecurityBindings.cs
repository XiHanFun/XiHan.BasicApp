// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 各包登记的字段安全操作绑定（单例）。
/// </summary>
/// <remarks>
/// 按操作 ID 查绑定，并按接口的 JSON 字段名给响应套掩码：同一操作只能绑定一个实体，绑定两次即装配失败。
/// </remarks>
public sealed class FieldSecurityBindings
{
    private readonly Dictionary<string, FieldSecurityBinding> _byOperation;

    /// <summary>
    /// 由各包的绑定构造。
    /// </summary>
    /// <param name="bindings">绑定。</param>
    public FieldSecurityBindings(IEnumerable<FieldSecurityBinding> bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        _byOperation = new Dictionary<string, FieldSecurityBinding>(StringComparer.Ordinal);
        foreach (var binding in bindings)
        {
            if (!_byOperation.TryAdd(binding.OperationId, binding))
            {
                throw new InvalidOperationException($"Operation '{binding.OperationId}' is bound to field security twice ('{_byOperation[binding.OperationId].Entity}' and '{binding.Entity}').");
            }
        }
    }

    /// <summary>
    /// 按操作 ID 找绑定。
    /// </summary>
    /// <param name="operationId">操作 ID。</param>
    /// <returns>绑定；没绑定为空。</returns>
    public FieldSecurityBinding? Find(string operationId) => _byOperation.GetValueOrDefault(operationId);

    /// <summary>
    /// 给一次调用的响应套掩码。
    /// </summary>
    /// <remarks>
    /// 操作没绑定、没有字段安全或没有规则即原样返回。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="operation">操作。</param>
    /// <param name="response">响应对象。</param>
    /// <returns>套过掩码的响应。</returns>
    public async ValueTask<object> MaskAsync(InvocationContext context, OperationDescriptor operation, object response)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(response);
        if (Find(operation.Id) is not { } binding || context.Services.GetService<IFieldSecurity>() is not { } security)
        {
            return response;
        }

        var rules = await security.ResolveAsync(binding.Entity, context).ConfigureAwait(false);
        return rules.IsEmpty ? response : Mask(operation, binding, rules, response);
    }

    /// <summary>
    /// 响应里每条记录套掩码。
    /// </summary>
    /// <remarks>
    /// 记录是单条响应、分页的 <c>items</c> 或绑定声明的属性；文本按规则打码（隐藏为空），非文本受保护即为空；没动到即原样返回。
    /// </remarks>
    /// <param name="operation">操作。</param>
    /// <param name="binding">绑定。</param>
    /// <param name="rules">有效规则。</param>
    /// <param name="response">响应对象。</param>
    /// <returns>套过掩码的响应（同一类型）。</returns>
    public static object Mask(OperationDescriptor operation, FieldSecurityBinding binding, FieldRules rules, object response)
    {
        var protectedRules = rules.All.Where(static r => r.IsReadProtected).ToList();
        if (protectedRules.Count == 0 || JsonNode.Parse(operation.EncodeResponse(response).Span) is not JsonObject root)
        {
            return response;
        }

        var target = binding.Path is { } path ? root[path] : root["items"] is JsonArray items && operation.ResponseType.IsGenericType ? items : root;
        IEnumerable<JsonObject> records = target switch
        {
            JsonArray array => array.OfType<JsonObject>(),
            JsonObject single => [single],
            _ => [],
        };
        var changed = false;
        foreach (var record in records)
        {
            foreach (var rule in protectedRules)
            {
                if (record.TryGetPropertyValue(rule.Field, out var value) && value is not null)
                {
                    record[rule.Field] = value is JsonValue text && text.TryGetValue<string>(out var raw) ? rule.Mask(raw) is { } masked ? JsonValue.Create(masked) : null : null;
                    changed = true;
                }
            }
        }

        return changed ? root.Deserialize(TypeInfo(binding, operation.ResponseType))! : response;
    }

    internal static JsonObject ToObject(object value, Type type, FieldSecurityBinding binding)
        => JsonSerializer.SerializeToNode(value, TypeInfo(binding, type)) as JsonObject ?? [];

    internal static JsonTypeInfo TypeInfo(FieldSecurityBinding binding, Type type)
        => binding.Json.GetTypeInfo(type) ?? throw new InvalidOperationException($"{type.Name} of '{binding.OperationId}' is not in {binding.Json.GetType().Name}.");

    /// <summary>
    /// 规则比较用的文本：字符串取原文，空为空，其余取 JSON 表示。
    /// </summary>
    internal static string? TextOf(JsonNode? node)
        => node switch
        {
            null => null,
            JsonValue value when value.TryGetValue<string>(out var text) => text,
            _ => node.ToJsonString(),
        };
}

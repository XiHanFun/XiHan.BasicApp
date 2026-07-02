#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SmsGatewayClientBase
// Guid:b7f3d0a2-4c58-4e91-8a6d-1f9e2c5b7d40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 15:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Infrastructure.Messaging;

/// <summary>
/// 短信模板映射项（内部模板码 → 服务商模板码 + 参数序）
/// </summary>
/// <param name="TemplateCode">服务商模板码（如阿里云 SMS_xxx / 腾讯云模板ID）</param>
/// <param name="ParamOrder">参数顺序（腾讯云位置参数数组必填；阿里云按命名 JSON 不使用）</param>
public sealed record SmsTemplateMapping(string TemplateCode, string[]? ParamOrder);

/// <summary>
/// 短信网关客户端基类：承载配置快照、模板映射解析与参数解析
/// </summary>
public abstract class SmsGatewayClientBase : ISmsGatewayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IReadOnlyDictionary<string, SmsTemplateMapping> _templateMap;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="signName">短信签名</param>
    /// <param name="templateMapJson">模板映射 JSON（SysSmsConfig.TemplateMap）</param>
    protected SmsGatewayClientBase(string signName, string? templateMapJson)
    {
        SignName = signName;
        _templateMap = ParseTemplateMap(templateMapJson);
    }

    /// <inheritdoc />
    public abstract SmsProviderType Provider { get; }

    /// <summary>
    /// 短信签名
    /// </summary>
    protected string SignName { get; }

    /// <inheritdoc />
    public abstract Task<SmsGatewaySendResult> SendAsync(SmsGatewayRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解析内部模板码对应的服务商模板映射（云厂商必须按模板发送，缺映射即失败）
    /// </summary>
    protected SmsTemplateMapping ResolveMapping(string? internalTemplateCode)
    {
        if (string.IsNullOrWhiteSpace(internalTemplateCode))
        {
            throw new InvalidOperationException("云厂商短信必须按模板发送，短信记录缺少模板码（TemplateCode）。");
        }

        if (!_templateMap.TryGetValue(internalTemplateCode.Trim(), out var mapping))
        {
            throw new InvalidOperationException($"短信网关配置的模板映射缺少内部模板码 [{internalTemplateCode}] 对应的服务商模板码，请在网关配置 TemplateMap 中补充。");
        }

        return mapping;
    }

    /// <summary>
    /// 解析模板参数 JSON 为键值字典（数字等非字符串值取原始文本）
    /// </summary>
    protected static Dictionary<string, string> ParseParams(string? templateParamsJson)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(templateParamsJson))
        {
            return result;
        }

        using var document = JsonDocument.Parse(templateParamsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("短信模板参数必须是 JSON 对象。");
        }

        foreach (var property in document.RootElement.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.GetRawText();
        }

        return result;
    }

    private static IReadOnlyDictionary<string, SmsTemplateMapping> ParseTemplateMap(string? templateMapJson)
    {
        if (string.IsNullOrWhiteSpace(templateMapJson))
        {
            return new Dictionary<string, SmsTemplateMapping>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, SmsTemplateMapping>>(templateMapJson, JsonOptions)
            ?? throw new InvalidOperationException("短信网关配置的模板映射不是合法 JSON 对象。");
        return new Dictionary<string, SmsTemplateMapping>(parsed, StringComparer.OrdinalIgnoreCase);
    }
}

#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OpenApiClientSecurityConfig
// Guid:4d7f2fc2-2719-4f7f-8f52-6f2d6cb5e0ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/14 10:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace XiHan.BasicApp.Saas.Application.Security;

/// <summary>
/// OpenAPI 客户端安全配置工具
/// </summary>
public static class OpenApiClientSecurityConfigHelper
{
    /// <summary>
    /// 配置分组
    /// </summary>
    public const string ConfigGroup = "OpenApi.Security.Client";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 构建配置键
    /// </summary>
    public static string BuildConfigKey(string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        var normalizedClientId = clientId.Trim().ToUpperInvariant();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedClientId)));
        return $"openapi:client:{hash}";
    }

    /// <summary>
    /// 构建配置名称
    /// </summary>
    public static string BuildConfigName(string clientId)
    {
        var normalized = string.IsNullOrWhiteSpace(clientId) ? "UNKNOWN" : clientId.Trim();
        var name = $"OpenAPI客户端安全配置:{normalized}";
        return name.Length <= 100 ? name : name[..100];
    }

    /// <summary>
    /// 构建配置描述
    /// </summary>
    public static string BuildConfigDescription(string clientId)
    {
        var normalized = string.IsNullOrWhiteSpace(clientId) ? "UNKNOWN" : clientId.Trim();
        var description = $"OpenAPI security policy for client '{normalized}'.";
        return description.Length <= 500 ? description : description[..500];
    }

    /// <summary>
    /// 序列化配置
    /// </summary>
    public static string Serialize(OpenApiClientSecurityConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return JsonSerializer.Serialize(config, JsonSerializerOptions);
    }

    /// <summary>
    /// 反序列化配置
    /// </summary>
    public static OpenApiClientSecurityConfig? Deserialize(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OpenApiClientSecurityConfig>(payload, JsonSerializerOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 标准化配置并补齐默认值
    /// </summary>
    public static OpenApiClientSecurityConfig Normalize(OpenApiClientSecurityConfig? input, string? fallbackEncryptKey = null)
    {
        var config = input ?? new OpenApiClientSecurityConfig();
        config.SignatureAlgorithm = NormalizeOrDefault(config.SignatureAlgorithm, "HMACSHA256");
        config.ContentSignatureAlgorithm = NormalizeOrDefault(config.ContentSignatureAlgorithm, "SHA256");
        config.EncryptionAlgorithm = NormalizeOrDefault(config.EncryptionAlgorithm, "AES-CBC");
        config.EncryptKey = string.IsNullOrWhiteSpace(config.EncryptKey) ? fallbackEncryptKey?.Trim() : config.EncryptKey.Trim();
        config.PublicKey = NormalizeNullable(config.PublicKey);
        config.Sm2PublicKey = NormalizeNullable(config.Sm2PublicKey);
        config.IpWhitelist = NormalizeNullable(config.IpWhitelist);
        return config;
    }

    /// <summary>
    /// 解析白名单为列表
    /// </summary>
    public static List<string> ParseIpWhitelist(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw
            .Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeOrDefault(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

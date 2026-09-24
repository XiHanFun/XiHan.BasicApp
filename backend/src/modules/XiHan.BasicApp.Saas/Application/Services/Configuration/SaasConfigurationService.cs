// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Configurations;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// SaaS 运行时配置服务实现。
/// </summary>
public sealed class SaasConfigurationService
    : ISaasConfigurationService
{
    /// <summary>
    /// 配置值的 JSON 约定：camelCase 属性名，数字不接受字符串形式，不可空的字段不接受 null；写出的中文不转义，参数页里可直接阅读和修改
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict,
        RespectNullableAnnotations = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly ISaasConfigValueQueryService _configValueQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasConfigurationService(
        ISaasConfigValueQueryService configValueQueryService)
    {
        _configValueQueryService = configValueQueryService;
    }

    /// <summary>
    /// 获取字符串配置。
    /// </summary>
    public async Task<string?> GetStringAsync(string configKey, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        var item = await GetValueItemAsync(configKey, cancellationToken);
        return item.Exists ? item.Value ?? defaultValue : defaultValue;
    }

    /// <summary>
    /// 按类型读取 JSON 配置。
    /// </summary>
    public async Task<T> GetJsonAsync<T>(string configKey, T defaultValue, CancellationToken cancellationToken = default)
    {
        var value = await GetStringAsync(configKey, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value, JsonOptions)
                ?? throw new InvalidOperationException($"配置 {configKey} 的值是 null，不是合法的 {typeof(T).Name}。");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"配置 {configKey} 的值不是合法的 {typeof(T).Name}：{exception.Message}", exception);
        }
    }

    /// <summary>
    /// 获取登录配置。
    /// </summary>
    public async Task<LoginConfigDto> GetLoginConfigAsync(CancellationToken cancellationToken = default)
    {
        // 登录模型为「先登录后选租户」，登录页不提供租户选择（落点由后端按成员关系决定）
        var settings = await GetJsonAsync(SaasConfigKeys.Auth.Login, new SaasLoginSettings(), cancellationToken);
        var methods = settings.Methods
            .Where(static method => !string.IsNullOrWhiteSpace(method))
            .Select(static method => method.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (methods.Count == 0)
        {
            throw new InvalidOperationException($"配置 {SaasConfigKeys.Auth.Login} 至少要开放一种登录方式。");
        }

        return new LoginConfigDto
        {
            LoginMethods = methods,
            OAuthProviders = [.. settings.OAuthProviders
                .Where(static provider => !string.IsNullOrWhiteSpace(provider.Name))
                .Select(static provider => new OAuthProviderItemDto
                {
                    Name = provider.Name.Trim(),
                    DisplayName = string.IsNullOrWhiteSpace(provider.DisplayName) ? provider.Name.Trim() : provider.DisplayName.Trim()
                })]
        };
    }

    private async Task<SaasConfigValueCacheItem> GetValueItemAsync(string configKey, CancellationToken cancellationToken)
    {
        return await _configValueQueryService.GetValueItemAsync(configKey, cancellationToken);
    }
}

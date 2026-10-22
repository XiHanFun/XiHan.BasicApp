// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 条件求值选项
/// </summary>
/// <remarks>
/// 绑定配置节 <see cref="SectionName"/>，权限条件与访问策略共用。
/// 时间类属性（时刻、小时、星期、日期）与不带时区的日期时间条件值都按 <see cref="TimeZone"/> 解释；
/// 客户端带的 <c>X-Timezone</c> 可伪造，不参与条件。
/// </remarks>
public sealed class SaasConditionOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "Saas:Conditions";

    /// <summary>
    /// 时区标识（IANA 或 Windows 标识），缺省 <c>Asia/Shanghai</c>
    /// </summary>
    public string TimeZone { get; set; } = "Asia/Shanghai";

    /// <summary>
    /// 解析时区
    /// </summary>
    /// <returns>时区</returns>
    /// <exception cref="InvalidOperationException">时区标识为空或本机找不到</exception>
    public TimeZoneInfo ResolveTimeZone()
    {
        if (string.IsNullOrWhiteSpace(TimeZone))
        {
            throw new InvalidOperationException($"配置 {SectionName}:TimeZone 不能为空。");
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(TimeZone.Trim());
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException($"配置 {SectionName}:TimeZone 的时区 '{TimeZone}' 在本机找不到。", ex);
        }
    }
}

/// <summary>
/// 条件求值选项校验：时区解析不了即校验失败，配合 ValidateOnStart 在启动时报出
/// </summary>
public sealed class SaasConditionOptionsValidator : IValidateOptions<SaasConditionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SaasConditionOptions options)
    {
        try
        {
            _ = options.ResolveTimeZone();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 访问策略实体扩展
/// </summary>
public partial class SysAccessPolicy : IValidatableObject
{
    /// <summary>
    /// 是否平台级策略
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool IsPlatform => TenantId == 0;

    /// <summary>
    /// 在给定时刻是否生效：启用且在生效期内
    /// </summary>
    /// <param name="now">时刻</param>
    /// <returns>是否生效</returns>
    public bool IsEffectiveAt(DateTimeOffset now)
    {
        return Status == EnableStatus.Enabled
            && (EffectiveTime is null || EffectiveTime.Value <= now)
            && (ExpirationTime is null || ExpirationTime.Value > now);
    }

    /// <summary>
    /// 校验实体自身的业务规则
    /// </summary>
    /// <param name="validationContext">校验上下文</param>
    /// <returns>校验失败项集合，全部通过时为空集合</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(PolicyCode))
        {
            yield return new ValidationResult("PolicyCode 不能为空。", [nameof(PolicyCode)]);
        }

        if (string.IsNullOrWhiteSpace(PolicyName))
        {
            yield return new ValidationResult("PolicyName 不能为空。", [nameof(PolicyName)]);
        }

        if (EffectiveTime.HasValue && ExpirationTime.HasValue && EffectiveTime.Value >= ExpirationTime.Value)
        {
            yield return new ValidationResult("生效时间必须早于失效时间。", [nameof(EffectiveTime), nameof(ExpirationTime)]);
        }
    }
}

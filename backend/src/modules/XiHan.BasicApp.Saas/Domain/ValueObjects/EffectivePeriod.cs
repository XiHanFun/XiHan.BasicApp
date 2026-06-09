#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EffectivePeriod
// Guid:db3fbdd8-27ce-41f1-a6b2-bf59f1e4f9d1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 生效周期
/// </summary>
public sealed record EffectivePeriod(DateTimeOffset? EffectiveTime, DateTimeOffset? ExpirationTime)
{
    /// <summary>
    /// 永久有效周期
    /// </summary>
    public static EffectivePeriod Always { get; } = new(null, null);

    /// <summary>
    /// 是否在指定时间生效
    /// </summary>
    /// <param name="now">判断时间</param>
    /// <returns>是否生效</returns>
    public bool IsActive(DateTimeOffset now)
    {
        return (!EffectiveTime.HasValue || EffectiveTime.Value <= now)
               && (!ExpirationTime.HasValue || ExpirationTime.Value > now);
    }

    /// <summary>
    /// 校验时间范围
    /// </summary>
    /// <exception cref="InvalidOperationException">失效时间早于或等于生效时间</exception>
    public void EnsureValidRange()
    {
        if (EffectiveTime.HasValue && ExpirationTime.HasValue && ExpirationTime.Value <= EffectiveTime.Value)
        {
            throw new InvalidOperationException("失效时间必须晚于生效时间。");
        }
    }
}

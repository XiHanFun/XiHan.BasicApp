// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 验证码防刷阈值常量（发送间隔 / 日配额 / 错误计数封禁）
/// </summary>
public static class VerificationThrottleConsts
{
    /// <summary>
    /// 同一联系方式 + 用途的最小重发间隔（秒）
    /// </summary>
    public const int SendIntervalSeconds = 60;

    /// <summary>
    /// 同一联系方式每日发送上限（跨用途累计）
    /// </summary>
    public const int DailyTargetQuota = 10;

    /// <summary>
    /// 同一来源 IP 每日发送上限
    /// </summary>
    public const int DailyIpQuota = 50;

    /// <summary>
    /// 触发封禁的连续校验失败次数
    /// </summary>
    public const int MaxVerifyFailures = 5;

    /// <summary>
    /// 封禁时长（分钟；封禁期内发送与校验均拒绝）
    /// </summary>
    public const int BanMinutes = 10;

    /// <summary>
    /// 防刷缓存键统一前缀
    /// </summary>
    public const string KeyPrefix = "saas:verify:throttle";
}

/// <summary>
/// 验证码防刷限流服务（发送间隔 / 日配额 / 错误计数封禁）
/// </summary>
/// <remarks>
/// 覆盖 <see cref="IProfileVerificationService"/> 的全部发码用途（含 2FA phone/email）与消费校验：
/// - 发送侧：同一 target+purpose 60 秒内不得重发；同一 target 每日上限 10 次；同一 IP 每日上限 50 次；
/// - 校验侧：按 userId+purpose 计连续失败，5 次失败触发 10 分钟封禁（封禁期发送/校验均拒绝），成功消费清零计数。
/// 阈值集中于 <see cref="VerificationThrottleConsts"/>；超限一律抛用户友好异常。
/// </remarks>
public interface IVerificationThrottleService
{
    /// <summary>
    /// 校验并占用一次发送额度（封禁 → 间隔 → 日配额依序检查，全部通过后记账）
    /// </summary>
    /// <param name="userId">用户标识（封禁维度）</param>
    /// <param name="purpose">验证码用途</param>
    /// <param name="target">接收目标（手机号/邮箱）</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureSendAllowedAsync(long userId, ProfileVerificationPurpose purpose, string target, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验当前是否允许消费验证码（封禁期内拒绝）
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="purpose">验证码用途</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureVerifyAllowedAsync(long userId, ProfileVerificationPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录一次校验失败（达到阈值时触发封禁）
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="purpose">验证码用途</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>true 表示本次失败触发了封禁（调用方应作废在途码）</returns>
    Task<bool> OnVerifyFailedAsync(long userId, ProfileVerificationPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录一次校验成功（清零失败计数）
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="purpose">验证码用途</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task OnVerifySucceededAsync(long userId, ProfileVerificationPurpose purpose, CancellationToken cancellationToken = default);
}

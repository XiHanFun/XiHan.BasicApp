// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Services;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 手机登录发码接口必须真的接了日配额限流，回归"匿名短信发送只有 60 秒 phone+IP 键、
/// 无日配额，可被每 61 秒发一条刷一整天"这个问题。
/// </summary>
/// <remarks>
/// <c>AuthAppService</c> 构造函数参数过多（30+ 依赖），不值得为一条限流调用搭一整套 mock；
/// 用 <see cref="SaasAppIlCallGraph"/> 直接看方法体 IL 里有没有真的调到
/// <see cref="IVerificationThrottleService.EnsureSendAllowedAsync"/>——这条调用本身的行为
/// （日配额阈值、拒绝语义）由 <see cref="VerificationThrottleServiceTests"/> 单独覆盖。
/// </remarks>
public sealed class AuthAppServicePhoneLoginCodeThrottleTests
{
    /// <summary>
    /// <c>PhoneLoginCodeAsync</c> 必须触达 <c>IVerificationThrottleService.EnsureSendAllowedAsync</c>。
    /// </summary>
    [Fact]
    public void PhoneLoginCodeAsync_ShouldReachVerificationThrottleServiceEnsureSendAllowed()
    {
        var method = typeof(AuthAppService).GetMethod(
            nameof(AuthAppService.PhoneLoginCodeAsync),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(method);

        var reaches = SaasAppIlCallGraph.Reaches(
            method!,
            typeof(AuthAppService),
            callee => callee.DeclaringType == typeof(IVerificationThrottleService)
                && callee.Name == nameof(IVerificationThrottleService.EnsureSendAllowedAsync));

        Assert.True(reaches, "PhoneLoginCodeAsync 没有调用 IVerificationThrottleService.EnsureSendAllowedAsync，日配额形同虚设。");
    }
}

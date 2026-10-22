// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限条件相关服务的测试装配：真实的编译器、判定与执行器，属性来源与记录器可替换
/// </summary>
internal static class PermissionConditionTestKit
{
    /// <summary>
    /// 条件资源登记表
    /// </summary>
    public static IPermissionResourceRegistry Resources(params PermissionResourceRegistration[] registrations)
    {
        var options = new PermissionResourceOptions();
        options.Registrations.AddRange(registrations);
        return new PermissionResourceRegistry(Options.Create(options));
    }

    /// <summary>
    /// 条件集编译器（Asia/Shanghai）
    /// </summary>
    public static SaasConditionSetCompiler Compiler(IPermissionResourceRegistry? resources = null)
    {
        return new SaasConditionSetCompiler(
            new DefaultConditionCompiler(new DefaultConditionAttributeRegistry(SaasConditionAttributes.All)),
            resources ?? Resources(),
            Options.Create(new SaasConditionOptions()));
    }

    /// <summary>
    /// 判定
    /// </summary>
    public static PermissionConditionDecider Decider(IPermissionResourceRegistry? resources = null)
    {
        return new PermissionConditionDecider(Compiler(resources), new DefaultConditionEvaluator());
    }

    /// <summary>
    /// 执行器
    /// </summary>
    public static PermissionConditionEnforcer Enforcer(
        ICurrentUser currentUser,
        PermissionConditionMode mode = PermissionConditionMode.Enforce,
        ISaasConditionAttributeSourceFactory? sources = null,
        IPermissionResourceRegistry? resources = null,
        IPermissionConditionHitRecorder? hitRecorder = null,
        ICurrentTenant? currentTenant = null)
    {
        resources ??= Resources();
        return new PermissionConditionEnforcer(
            Decider(resources),
            sources ?? new Mock<ISaasConditionAttributeSourceFactory>().Object,
            resources,
            new FixedOptionsMonitor<SaasPermissionConditionOptions>(new SaasPermissionConditionOptions { Mode = mode }),
            hitRecorder ?? new Mock<IPermissionConditionHitRecorder>().Object,
            currentUser,
            currentTenant ?? new TestCurrentTenant());
    }

    /// <summary>
    /// 固定值的选项监视器，可改值模拟热更新
    /// </summary>
    public sealed class FixedOptionsMonitor<TOptions>(TOptions value) : IOptionsMonitor<TOptions>
    {
        /// <summary>
        /// 当前值
        /// </summary>
        public TOptions CurrentValue { get; set; } = value;

        /// <inheritdoc />
        public TOptions Get(string? name)
        {
            return CurrentValue;
        }

        /// <inheritdoc />
        public IDisposable? OnChange(Action<TOptions, string?> listener)
        {
            return null;
        }
    }
}

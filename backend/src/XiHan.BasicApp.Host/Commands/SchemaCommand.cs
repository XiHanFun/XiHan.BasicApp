// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Hosting.CommandLine;
using XiHan.Framework.Hosting.CommandLine.Commands;
using XiHan.Framework.Hosting.CommandLine.Runtime;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Host.Commands;

/// <summary>
/// 结构升级命令
/// </summary>
/// <remarks>
/// <c>schema upgrade</c>：执行平台库与已就绪的租户独立库比版本台账新的升级脚本、按实体建表补列，完成后输出报告；平台库失败即非零退出，
/// 只有租户独立库失败时照样输出报告（带失败的库与原因），再以失败退出
/// <c>schema plan</c>：只读预览每个库要执行的脚本、要建的表、列与索引，不执行；有读不到的租户库即非零退出
/// 关掉启动自动升级（<c>BasicApp:Schema:AutoUpgrade=false</c>）时由运维先跑 plan 看一眼、再跑 upgrade、再启动
/// 命令行上不启动应用（<see cref="RunBeforeStartAsync"/>）：新库上 Feature 一启动就要读写自己的表，升级与预览要排在它们之前，和正常启动同序
/// </remarks>
public sealed class SchemaCommand : ICommand
{
    /// <summary>
    /// 命令名
    /// </summary>
    public const string CommandName = "schema";

    private const string UpgradeAction = "upgrade";
    private const string PlanAction = "plan";

    /// <inheritdoc />
    public string Name => CommandName;

    /// <inheritdoc />
    public string DescriptionKey => HostMessages.SchemaDescription;

    /// <summary>
    /// 是不是 <c>schema upgrade</c>
    /// </summary>
    public static bool IsUpgrade(IReadOnlyList<string> args) => IsAction(args, UpgradeAction);

    /// <summary>
    /// 是不是要在启动应用之前执行的 <c>schema upgrade</c> 或 <c>schema plan</c>
    /// </summary>
    public static bool IsBeforeStart(IReadOnlyList<string> args) => IsAction(args, UpgradeAction) || IsAction(args, PlanAction);

    /// <summary>
    /// 不启动应用执行 <c>schema upgrade</c> 或 <c>schema plan</c>
    /// </summary>
    /// <param name="args">命令行参数</param>
    /// <param name="services">宿主的服务（应用不用启动）</param>
    /// <param name="output">报告写到这里</param>
    /// <param name="error">失败写到这里</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>退出码</returns>
    public static Task<int> RunBeforeStartAsync(IReadOnlyList<string> args, IServiceProvider services, TextWriter output, TextWriter error, CancellationToken cancellationToken)
        => IsAction(args, PlanAction)
            ? PlanBeforeStartAsync(services, output, error, cancellationToken)
            : UpgradeBeforeStartAsync(services, output, error, cancellationToken);

    /// <summary>
    /// 不启动应用执行结构升级并输出报告
    /// </summary>
    /// <param name="services">宿主的服务（应用不用启动）</param>
    /// <param name="output">报告写到这里</param>
    /// <param name="error">失败写到这里</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>退出码</returns>
    public static async Task<int> UpgradeBeforeStartAsync(IServiceProvider services, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var upgraded = await services.GetRequiredService<SchemaUpgrader>().UpgradeAsync(cancellationToken).ConfigureAwait(false);
        if (!upgraded.TryGetValue(out var report))
        {
            return await CommandLineFailure.WriteAsync(error, upgraded.Error!, services, null).ConfigureAwait(false);
        }

        await output.WriteLineAsync(JsonSerializer.Serialize(report, HostJsonContext.Default.SchemaUpgradeReport)).ConfigureAwait(false);
        return report.Failures.Count == 0
            ? CommandLineExitCodes.Success
            : await CommandLineFailure.WriteAsync(error, TenantsFailed(report.Failures), services, null).ConfigureAwait(false);
    }

    /// <summary>
    /// 不启动应用输出升级预览
    /// </summary>
    /// <param name="services">宿主的服务（应用不用启动）</param>
    /// <param name="output">预览写到这里</param>
    /// <param name="error">失败写到这里</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>退出码</returns>
    public static async Task<int> PlanBeforeStartAsync(IServiceProvider services, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        var planned = await services.GetRequiredService<SchemaUpgrader>().PlanAsync(cancellationToken).ConfigureAwait(false);
        if (!planned.TryGetValue(out var plan))
        {
            return await CommandLineFailure.WriteAsync(error, planned.Error!, services, null).ConfigureAwait(false);
        }

        await output.WriteLineAsync(JsonSerializer.Serialize(plan, HostJsonContext.Default.SchemaPlanResult)).ConfigureAwait(false);
        return plan.Failures.Count == 0
            ? CommandLineExitCodes.Success
            : await CommandLineFailure.WriteAsync(error, TenantsFailed(plan.Failures), services, null).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<int> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var action = context.Arguments.Count > 0 ? context.Arguments[0] : string.Empty;
        return action switch
        {
            UpgradeAction => await UpgradeBeforeStartAsync(context.Services, context.Output, context.Error, context.CancellationToken).ConfigureAwait(false),
            PlanAction => await PlanBeforeStartAsync(context.Services, context.Output, context.Error, context.CancellationToken).ConfigureAwait(false),
            _ => await CommandLineFailure.WriteAsync(
                context.Error,
                Error.Validation(HostErrors.SchemaActionUnknown, HostMessages.SchemaActionUnknown, MessageArguments.Of(("action", action))),
                context.Services,
                null,
                CommandLineExitCodes.Usage).ConfigureAwait(false),
        };
    }

    private static bool IsAction(IReadOnlyList<string> args, string action)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Count > 1 && string.Equals(args[0], CommandName, StringComparison.Ordinal) && string.Equals(args[1], action, StringComparison.Ordinal);
    }

    private static Error TenantsFailed(IReadOnlyList<SchemaUpgradeFailure> failures)
        => Error.Literal(RuntimeErrors.TenantDatabaseFailed, $"{failures.Count} tenant database(s) failed: " + string.Join("; ", failures.Select(static f => $"{f.Database} ({f.Code}): {f.Message}")));
}

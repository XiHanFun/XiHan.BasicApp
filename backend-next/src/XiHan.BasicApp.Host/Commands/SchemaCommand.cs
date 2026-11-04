// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
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
/// <c>schema upgrade</c>：按版本台账执行平台库还没执行的升级脚本（新库从基线建起），完成后输出报告；失败即非零退出
/// 关掉启动自动升级（<c>BasicApp:Schema:AutoUpgrade=false</c>）时由运维先跑它再启动
/// 命令行上不启动应用（<see cref="UpgradeBeforeStartAsync"/>）：新库上 Feature 一启动就要读写自己的表，升级要排在它们之前，和正常启动同序
/// </remarks>
public sealed class SchemaCommand : ICommand
{
    /// <summary>
    /// 命令名
    /// </summary>
    public const string CommandName = "schema";

    private const string UpgradeAction = "upgrade";

    /// <inheritdoc />
    public string Name => CommandName;

    /// <inheritdoc />
    public string DescriptionKey => HostMessages.SchemaDescription;

    /// <summary>
    /// 是不是 <c>schema upgrade</c>
    /// </summary>
    public static bool IsUpgrade(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Count > 1 && string.Equals(args[0], CommandName, StringComparison.Ordinal) && string.Equals(args[1], UpgradeAction, StringComparison.Ordinal);
    }

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
        return CommandLineExitCodes.Success;
    }

    /// <inheritdoc />
    public async ValueTask<int> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var action = context.Arguments.Count > 0 ? context.Arguments[0] : string.Empty;
        if (!string.Equals(action, UpgradeAction, StringComparison.Ordinal))
        {
            var usage = Error.Validation(HostErrors.SchemaActionUnknown, HostMessages.SchemaActionUnknown, MessageArguments.Of(("action", action)));
            return await CommandLineFailure.WriteAsync(context.Error, usage, context.Services, null, CommandLineExitCodes.Usage).ConfigureAwait(false);
        }

        return await UpgradeBeforeStartAsync(context.Services, context.Output, context.Error, context.CancellationToken).ConfigureAwait(false);
    }
}

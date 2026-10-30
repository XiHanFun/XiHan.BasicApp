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
/// 结构升级命令。
/// </summary>
/// <remarks>
/// <c>schema upgrade</c>：按版本台账执行平台库还没执行的升级脚本（新库从基线建起），完成后输出报告；失败即非零退出。
/// 关掉启动自动升级（<c>BasicApp:Schema:AutoUpgrade=false</c>）时由运维先跑它再启动。
/// </remarks>
public sealed class SchemaCommand : ICommand
{
    /// <summary>
    /// 命令名。
    /// </summary>
    public const string CommandName = "schema";

    /// <inheritdoc />
    public string Name => CommandName;

    /// <inheritdoc />
    public string DescriptionKey => HostMessages.SchemaDescription;

    /// <inheritdoc />
    public async ValueTask<int> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var action = context.Arguments.Count > 0 ? context.Arguments[0] : string.Empty;
        if (!string.Equals(action, "upgrade", StringComparison.Ordinal))
        {
            var usage = Error.Validation(HostErrors.SchemaActionUnknown, HostMessages.SchemaActionUnknown, MessageArguments.Of(("action", action)));
            return await CommandLineFailure.WriteAsync(context.Error, usage, context.Services, null, CommandLineExitCodes.Usage).ConfigureAwait(false);
        }

        var upgraded = await context.Services.GetRequiredService<SchemaUpgrader>().UpgradeAsync(context.CancellationToken).ConfigureAwait(false);
        if (!upgraded.TryGetValue(out var report))
        {
            return await CommandLineFailure.WriteAsync(context.Error, upgraded.Error!, context.Services, null).ConfigureAwait(false);
        }

        await context.Output.WriteLineAsync(JsonSerializer.Serialize(report, HostJsonContext.Default.SchemaUpgradeReport)).ConfigureAwait(false);
        return CommandLineExitCodes.Success;
    }
}

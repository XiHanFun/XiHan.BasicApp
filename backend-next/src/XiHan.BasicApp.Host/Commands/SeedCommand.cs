// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Hosting.CommandLine;
using XiHan.Framework.Hosting.CommandLine.Commands;
using XiHan.Framework.Hosting.CommandLine.Runtime;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Host.Commands;

/// <summary>
/// <c>seed [--only &lt;名,...&gt;] [--list]</c>：按号段顺序执行各包登记的种子（每个种子一次独立的内核调用与工作单元）。
/// 成功把 <see cref="SeedReport"/> 写到 stdout；失败按框架命令行约定把 <c>{"code","detail"}</c> 写到 stderr，退出码按错误 KIND。
/// </summary>
public sealed class SeedCommand : ICommand
{
    /// <summary>
    /// 命令名。
    /// </summary>
    public const string CommandName = "seed";

    /// <inheritdoc />
    public string Name => CommandName;

    /// <inheritdoc />
    public string DescriptionKey => HostMessages.SeedDescription;

    /// <inheritdoc />
    public async ValueTask<int> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var runner = context.Services.GetRequiredService<SeedRunner>();
        var all = await runner.DescribeAsync(context.CancellationToken).ConfigureAwait(false);
        var only = context.CommandLine.OptionValues("only")
            .SelectMany(static value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var unknown = only.FirstOrDefault(name => !all.Any(s => string.Equals(s.Name, name, StringComparison.Ordinal)));
        if (unknown is not null)
        {
            var usage = Error.Validation(HostErrors.SeederUnknown, HostMessages.SeederUnknown, MessageArguments.Of(("name", unknown)));
            return await CommandLineFailure.WriteAsync(context.Error, usage, context.Services, null, CommandLineExitCodes.Usage).ConfigureAwait(false);
        }

        var selected = all.Where(s => only.Count == 0 || only.Contains(s.Name, StringComparer.Ordinal)).Select(static s => new SeedEntry(s.Name, s.Order)).ToList();
        var listOnly = context.HasOption("list");
        if (!listOnly)
        {
            var outcome = await runner.RunAsync(only, context.CancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure)
            {
                return await CommandLineFailure.WriteAsync(context.Error, outcome.Error!, context.Services, null).ConfigureAwait(false);
            }
        }

        var report = new SeedReport(listOnly ? "listed" : "completed", selected);
        await context.Output.WriteLineAsync(JsonSerializer.Serialize(report, HostJsonContext.Default.SeedReport)).ConfigureAwait(false);
        return CommandLineExitCodes.Success;
    }
}

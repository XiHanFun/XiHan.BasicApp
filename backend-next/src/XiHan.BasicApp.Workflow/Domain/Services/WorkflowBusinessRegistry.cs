// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Workflow;
using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Activities;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Workflow.Domain.Services;

/// <summary>
/// 审批业务登记表
/// </summary>
/// <remarks>
/// 汇总各包经 Contracts 的 <c>AddWorkflowBusiness</c> 登记的处理器，按业务码（即流程定义名）索引；同一业务码登记两次即抛出，
/// Workflow 在启动时构造一次（启动核对），照 4.x 的「同编码多个即抛错」但提前到装配阶段
/// </remarks>
public sealed class WorkflowBusinessRegistry
{
    /// <summary>
    /// 业务没写结论时允许的结论
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultOutcomes = ["approved", "rejected"];

    private readonly Dictionary<string, IWorkflowBusinessHandler> _byCode = new(StringComparer.Ordinal);

    /// <summary>
    /// 汇总处理器
    /// </summary>
    /// <param name="handlers">各包登记的处理器</param>
    /// <exception cref="InvalidOperationException">同一业务码登记了两次，或业务码不合流程定义名的文法</exception>
    public WorkflowBusinessRegistry(IEnumerable<IWorkflowBusinessHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);
        foreach (var handler in handlers)
        {
            var code = handler.Descriptor.Code;
            if (!WorkflowName.IsValid(code))
            {
                throw new InvalidOperationException($"The workflow business code '{code}' of {handler.GetType().FullName} is not a valid workflow definition name.");
            }

            if (!_byCode.TryAdd(code, handler))
            {
                throw new InvalidOperationException($"The workflow business code '{code}' is registered twice ({_byCode[code].GetType().FullName}, {handler.GetType().FullName}).");
            }
        }
    }

    /// <summary>
    /// 全部业务描述（按业务码排序）
    /// </summary>
    public IReadOnlyList<WorkflowBusinessDescriptor> Descriptors => _byCode.Values.Select(static h => h.Descriptor).OrderBy(static d => d.Code, StringComparer.Ordinal).ToList();

    /// <summary>
    /// 按业务码取处理器
    /// </summary>
    /// <param name="code">业务码（流程定义名）</param>
    /// <returns>处理器，没登记时为空</returns>
    public IWorkflowBusinessHandler? Find(string code) => _byCode.GetValueOrDefault(code);

    /// <summary>
    /// 业务允许的结论
    /// </summary>
    /// <param name="descriptor">业务描述</param>
    /// <returns>结论</returns>
    public static IReadOnlyList<string> OutcomesOf(WorkflowBusinessDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        return descriptor.Outcomes.Count == 0 ? DefaultOutcomes : descriptor.Outcomes;
    }
}

/// <summary>
/// 发布前的业务校验
/// </summary>
/// <remarks>
/// 照 4.x：关联了审批业务（定义名等于业务码）的定义，每个结束节点都要写结论、结论要在业务允许的范围里，且不许有终止节点
/// （业务的结论回写只在带结论的结束节点上发生，终止不回写）。先过这一步，再过框架的结构校验
/// </remarks>
public static class WorkflowDefinitionGuard
{
    /// <summary>
    /// 校验
    /// </summary>
    /// <param name="definition">定义</param>
    /// <param name="business">关联的业务</param>
    /// <returns>是否通过</returns>
    public static Result Check(WorkflowDefinition definition, WorkflowBusinessDescriptor business)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(business);
        var allowed = WorkflowBusinessRegistry.OutcomesOf(business);
        foreach (var node in definition.Nodes)
        {
            if (node.ActivityType.Value == ActivityTypes.Terminate)
            {
                return Error.Validation(WorkflowErrors.BusinessTerminateForbidden, WorkflowMessages.BusinessTerminateForbidden, MessageArguments.Of(("node", node.Id)));
            }

            if (node.ActivityType.Value == ActivityTypes.End)
            {
                var outcome = node.Properties.TryGetPropertyValue(EndActivity.OutcomeProperty, out var value) ? value?.GetValue<string>() : null;
                if (string.IsNullOrWhiteSpace(outcome) || !allowed.Contains(outcome, StringComparer.Ordinal))
                {
                    return Error.Validation(WorkflowErrors.BusinessOutcomeInvalid, WorkflowMessages.BusinessOutcomeInvalid, MessageArguments.Of(("node", node.Id), ("outcome", outcome ?? string.Empty), ("allowed", string.Join(", ", allowed))));
                }
            }
        }

        return Result.Success();
    }
}

// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Workflow.Abstractions;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Application.Business;

/// <summary>
/// 审批业务流程的发布校验（框架结构校验之外，业务流程还要能得出结论）
/// </summary>
/// <remarks>
/// 编码登记为审批业务的流程：每个结束节点都必须声明业务允许的结果（业务据此回写单据）；
/// 不允许终止节点——终止是「强制结束」，业务得不到结论，驳回请走声明 rejected 的结束节点。
/// </remarks>
public sealed class WorkflowBusinessDefinitionGuard
{
    private const string OutcomePropertyName = "Outcome";

    private readonly WorkflowBusinessRegistry _registry;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="registry">审批业务登记表</param>
    public WorkflowBusinessDefinitionGuard(WorkflowBusinessRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// 校验流程定义
    /// </summary>
    /// <param name="definition">流程定义</param>
    /// <returns>错误信息列表（非业务流程或合法时为空）</returns>
    public IReadOnlyList<string> Validate(WorkflowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var handler = _registry.Find(definition.Code);
        if (handler is null)
        {
            return [];
        }

        var descriptor = handler.Descriptor;
        var allowed = string.Join(" / ", descriptor.Outcomes);
        var errors = new List<string>();

        var ends = definition.Nodes.Where(node => node.ActivityType == WorkflowActivityTypes.End).ToList();
        if (ends.Count == 0)
        {
            errors.Add($"「{descriptor.Name}」流程至少需要一个声明结果（{allowed}）的结束节点");
        }

        foreach (var end in ends)
        {
            var outcome = end.Properties.TryGetValue(OutcomePropertyName, out var raw)
                ? WorkflowValueConverter.ConvertTo<string>(WorkflowValueConverter.Normalize(raw))?.Trim()
                : null;

            if (string.IsNullOrEmpty(outcome))
            {
                errors.Add($"「{descriptor.Name}」流程的结束节点 {end.Id} 必须声明结果（{allowed}），业务据此回写单据");
            }
            else if (!descriptor.Outcomes.Contains(outcome, StringComparer.Ordinal))
            {
                errors.Add($"「{descriptor.Name}」流程的结束节点 {end.Id} 声明了不支持的结果 {outcome}（允许：{allowed}）");
            }
        }

        errors.AddRange(definition.Nodes
            .Where(node => node.ActivityType == WorkflowActivityTypes.Terminate)
            .Select(node => $"「{descriptor.Name}」流程不能使用终止节点 {node.Id}：业务得不到结论，否决请连到声明 rejected 的结束节点"));

        return errors;
    }
}

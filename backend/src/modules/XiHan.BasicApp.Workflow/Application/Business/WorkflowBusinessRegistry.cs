// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.Framework.Core.Exceptions;

namespace XiHan.BasicApp.Workflow.Application.Business;

/// <summary>
/// 审批业务登记表（按业务编码找处理器）
/// </summary>
/// <remarks>
/// 处理器在调用时才从当前作用域解析：处理器常依赖业务查询服务，而查询服务又依赖审批业务服务，
/// 构造期注入会形成循环依赖。业务编码大小写敏感、与流程定义编码完全一致。
/// </remarks>
public sealed class WorkflowBusinessRegistry
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="serviceProvider">服务提供者</param>
    public WorkflowBusinessRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 全部已登记的业务描述（按编码排序）
    /// </summary>
    /// <returns>业务描述列表</returns>
    public IReadOnlyList<WorkflowBusinessDescriptor> GetDescriptors()
    {
        return [.. GetHandlers().Values.Select(handler => handler.Descriptor).OrderBy(descriptor => descriptor.Code, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 按业务编码查找处理器
    /// </summary>
    /// <param name="businessCode">业务编码（即流程定义编码）</param>
    /// <returns>处理器（未登记返回 null）</returns>
    public IWorkflowBusinessHandler? Find(string? businessCode)
    {
        return string.IsNullOrWhiteSpace(businessCode) ? null : GetHandlers().GetValueOrDefault(businessCode);
    }

    /// <summary>
    /// 按业务编码获取处理器
    /// </summary>
    /// <param name="businessCode">业务编码</param>
    /// <returns>处理器</returns>
    /// <exception cref="BusinessException">业务未登记时抛出</exception>
    public IWorkflowBusinessHandler Get(string businessCode)
    {
        return Find(businessCode) ?? throw new BusinessException(message: $"审批业务 {businessCode} 未登记");
    }

    private Dictionary<string, IWorkflowBusinessHandler> GetHandlers()
    {
        var handlers = new Dictionary<string, IWorkflowBusinessHandler>(StringComparer.Ordinal);
        foreach (var handler in _serviceProvider.GetServices<IWorkflowBusinessHandler>())
        {
            var code = handler.Descriptor.Code;
            if (!handlers.TryAdd(code, handler))
            {
                throw new InvalidOperationException(
                    $"审批业务 {code} 登记了多个处理器：{handlers[code].GetType().FullName}、{handler.GetType().FullName}");
            }
        }

        return handlers;
    }
}

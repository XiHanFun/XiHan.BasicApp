// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using System.Globalization;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.BasicApp.Workflow.Domain.Repositories;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Engine;
using XiHan.Framework.Workflow.Abstractions.Exceptions;
using XiHan.Framework.Workflow.Abstractions.Runtime;
using XiHan.Framework.Workflow.Abstractions.Stores;
using XiHan.Framework.Workflow.Abstractions.UserTasks;

namespace XiHan.BasicApp.Workflow.Application.Business;

/// <summary>
/// 审批业务服务（<see cref="IWorkflowBusinessService"/> 的工作流实现）
/// </summary>
/// <remarks>
/// 业务编码即流程定义编码，单据标识写入实例的业务相关性标识；同一单据同一时刻只允许一个进行中的实例。
/// 在业务的事务型工作单元内调用时，引擎写入随业务事务一起提交或回滚（工作流存储会登记进当前工作单元）。
/// </remarks>
public sealed class WorkflowBusinessService : IWorkflowBusinessService
{
    private readonly WorkflowBusinessRegistry _registry;
    private readonly WorkflowUserNameResolver _userNameResolver;
    private readonly IWorkflowEngine _engine;
    private readonly IWorkflowDefinitionManager _definitionManager;
    private readonly IWorkflowDefinitionRepository _definitionRepository;
    private readonly IWorkflowDefinitionStore _definitionStore;
    private readonly IWorkflowInstanceRepository _instanceRepository;
    private readonly IWorkflowUserTaskService _userTaskService;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ILogger<WorkflowBusinessService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public WorkflowBusinessService(
        WorkflowBusinessRegistry registry,
        WorkflowUserNameResolver userNameResolver,
        IWorkflowEngine engine,
        IWorkflowDefinitionManager definitionManager,
        IWorkflowDefinitionRepository definitionRepository,
        IWorkflowDefinitionStore definitionStore,
        IWorkflowInstanceRepository instanceRepository,
        IWorkflowUserTaskService userTaskService,
        ICurrentUser currentUser,
        IUnitOfWorkManager unitOfWorkManager,
        ILogger<WorkflowBusinessService> logger)
    {
        _registry = registry;
        _userNameResolver = userNameResolver;
        _engine = engine;
        _definitionManager = definitionManager;
        _definitionRepository = definitionRepository;
        _definitionStore = definitionStore;
        _instanceRepository = instanceRepository;
        _userTaskService = userTaskService;
        _currentUser = currentUser;
        _unitOfWorkManager = unitOfWorkManager;
        _logger = logger;
    }

    /// <summary>
    /// 发起审批流（当前登录用户为发起人）
    /// </summary>
    public async Task<WorkflowBusinessStartResult> StartAsync(WorkflowBusinessStartRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.BusinessId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);

        var descriptor = _registry.Get(request.BusinessCode).Descriptor;
        var businessId = request.BusinessId.Trim();

        var active = await _instanceRepository.GetActiveByCorrelationIdAsync(descriptor.Code, businessId, cancellationToken);
        if (active.Count > 0)
        {
            throw new BusinessException(message: $"{descriptor.Name}「{businessId}」已有进行中的审批，不能重复发起");
        }

        await EnsureDefaultDefinitionAsync(descriptor, cancellationToken);

        WorkflowInstance instance;
        try
        {
            instance = await _engine.StartAsync(new WorkflowStartRequest
            {
                DefinitionCode = descriptor.Code,
                Name = request.Title.Trim(),
                CorrelationId = businessId,
                StarterId = _currentUser.UserId?.ToString(CultureInfo.InvariantCulture),
                Variables = new Dictionary<string, object?>(request.Variables)
            }, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: $"{descriptor.Name}审批流程发起失败：{ex.Message}（请检查流程定义「{descriptor.Code}」是否已发布）");
        }

        // 发起即故障（如首个审批节点解析不到审批人）：不留半截审批，整单失败
        if (instance.Status == WorkflowInstanceStatus.Faulted)
        {
            if (!IsInTransaction())
            {
                // 不在事务里时实例已落库，终止掉，避免留下无单据可对应的故障实例
                await _engine.TerminateAsync(instance.Id, $"发起失败：{instance.FaultMessage}", cancellationToken);
            }

            throw new BusinessException(message: $"{descriptor.Name}审批流程发起失败：{instance.FaultMessage}");
        }

        return new WorkflowBusinessStartResult(ParseId(instance.Id), instance.Status, instance.Outcome);
    }

    /// <summary>
    /// 撤回进行中的审批流（取消实例，不回调业务）
    /// </summary>
    public async Task<bool> WithdrawAsync(string businessCode, string businessId, string? reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(businessCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessId);

        var descriptor = _registry.Get(businessCode).Descriptor;
        var active = await _instanceRepository.GetActiveByCorrelationIdAsync(descriptor.Code, businessId.Trim(), cancellationToken);

        foreach (var instance in active)
        {
            try
            {
                await _engine.CancelAsync(instance.BasicId.ToString(CultureInfo.InvariantCulture), reason, cancellationToken);
            }
            catch (WorkflowException ex)
            {
                throw new BusinessException(message: $"撤回{descriptor.Name}审批流程失败：{ex.Message}");
            }
        }

        return active.Count > 0;
    }

    /// <summary>
    /// 批量查询单据的审批进度
    /// </summary>
    public async Task<IReadOnlyDictionary<string, WorkflowBusinessProgress>> GetProgressAsync(
        string businessCode,
        IReadOnlyCollection<string> businessIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(businessCode);
        ArgumentNullException.ThrowIfNull(businessIds);

        var instances = await _instanceRepository.GetLatestByCorrelationIdsAsync(businessCode, businessIds, cancellationToken);
        if (instances.Count == 0)
        {
            return new Dictionary<string, WorkflowBusinessProgress>(StringComparer.Ordinal);
        }

        // 只有运行中的实例才有待办，按实例取一次
        var pendingTasks = new Dictionary<long, List<WorkflowUserTask>>();
        foreach (var instance in instances.Values.Where(item => item.Status == WorkflowInstanceStatus.Running))
        {
            pendingTasks[instance.BasicId] = await _userTaskService.GetPendingByInstanceAsync(
                instance.BasicId.ToString(CultureInfo.InvariantCulture), cancellationToken);
        }

        var names = await _userNameResolver.ResolveAsync(
            pendingTasks.Values.SelectMany(tasks => tasks).Select(task => task.AssigneeId), cancellationToken);
        var nodeNames = new WorkflowNodeNameCache(_definitionStore);

        var result = new Dictionary<string, WorkflowBusinessProgress>(StringComparer.Ordinal);
        foreach (var (businessId, instance) in instances)
        {
            var tasks = pendingTasks.GetValueOrDefault(instance.BasicId) ?? [];
            var pendingNodeNames = new List<string>();
            foreach (var nodeId in tasks.Select(task => task.NodeId).Distinct(StringComparer.Ordinal))
            {
                pendingNodeNames.Add(await nodeNames.GetAsync(instance.DefinitionId, nodeId, cancellationToken));
            }

            result[businessId] = new WorkflowBusinessProgress
            {
                InstanceId = instance.BasicId,
                Status = instance.Status,
                Outcome = instance.Outcome,
                PendingNodeNames = pendingNodeNames,
                PendingAssignees = [.. tasks
                    .Select(task => task.AssigneeId)
                    .Distinct(StringComparer.Ordinal)
                    .Select(id => new WorkflowBusinessUser(
                        long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var userId) ? userId : 0,
                        names.GetValueOrDefault(id) ?? id))],
                FaultMessage = instance.FaultMessage,
                StartTime = instance.StartTime,
                EndTime = instance.EndTime
            };
        }

        return result;
    }

    /// <summary>
    /// 本租户还没有任何同编码流程定义时，按业务默认模板建一个并发布
    /// </summary>
    private async Task EnsureDefaultDefinitionAsync(WorkflowBusinessDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (descriptor.DefaultDefinition is null
            || await _definitionRepository.AnyAsync(definition => definition.Code == descriptor.Code, cancellationToken))
        {
            return;
        }

        var template = descriptor.DefaultDefinition();
        if (!string.Equals(template.Code, descriptor.Code, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"审批业务 {descriptor.Code} 的默认流程模板编码为 {template.Code}，必须与业务编码一致");
        }

        try
        {
            var created = await _definitionManager.CreateAsync(template, cancellationToken);
            _ = await _definitionManager.PublishAsync(created.Id, cancellationToken);
        }
        catch (WorkflowException ex)
        {
            throw new BusinessException(message: $"{descriptor.Name}默认审批流程创建失败：{ex.Message}");
        }

        _logger.LogInformation("审批业务 {BusinessCode} 首次发起，已按默认模板创建并发布流程定义", descriptor.Code);
    }

    private bool IsInTransaction()
    {
        var unitOfWork = _unitOfWorkManager.Current;
        return unitOfWork is { IsCompleted: false, IsDisposed: false } && unitOfWork.Options.IsTransactional;
    }

    private static long ParseId(string id)
    {
        return long.Parse(id, NumberStyles.None, CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// 节点名称缓存（按定义标识缓存定义，单次请求内复用）
/// </summary>
internal sealed class WorkflowNodeNameCache
{
    private readonly IWorkflowDefinitionStore _definitionStore;
    private readonly Dictionary<long, WorkflowDefinition?> _definitions = [];

    public WorkflowNodeNameCache(IWorkflowDefinitionStore definitionStore)
    {
        _definitionStore = definitionStore;
    }

    /// <summary>
    /// 获取定义（不存在返回 null）
    /// </summary>
    public async Task<WorkflowDefinition?> GetDefinitionAsync(long definitionId, CancellationToken cancellationToken)
    {
        if (!_definitions.TryGetValue(definitionId, out var definition))
        {
            definition = await _definitionStore.FindAsync(definitionId.ToString(CultureInfo.InvariantCulture), cancellationToken);
            _definitions[definitionId] = definition;
        }

        return definition;
    }

    /// <summary>
    /// 获取节点名称（定义或节点不存在时回退为节点标识）
    /// </summary>
    public async Task<string> GetAsync(long definitionId, string nodeId, CancellationToken cancellationToken)
    {
        var definition = await GetDefinitionAsync(definitionId, cancellationToken);
        var node = definition?.Nodes.FirstOrDefault(item => item.Id == nodeId);
        return string.IsNullOrWhiteSpace(node?.Name) ? nodeId : node.Name;
    }
}

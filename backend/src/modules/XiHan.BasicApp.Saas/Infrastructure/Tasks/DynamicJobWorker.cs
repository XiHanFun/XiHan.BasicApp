// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Tasks.ScheduledJobs.Abstractions;
using XiHan.Framework.Tasks.ScheduledJobs.Models;

namespace XiHan.BasicApp.Saas.Infrastructure.Tasks;

/// <summary>
/// 动态任务执行器，桥接框架 IJobWorker 与 SysTask 实体的 TaskClass/TaskMethod 反射模型
/// </summary>
/// <remarks>
/// <para>
/// 该 Worker 从 JobInfo.DefaultParameters 中读取 SysTask 配置（TaskId、TaskClass、TaskMethod、TaskParams），
/// 然后通过 ITaskRepository 获取最新的 SysTask 记录，使用反射调用对应的类和方法。
/// </para>
/// <para>
/// JobInfo.DefaultParameters 应包含：
/// - "taskId": SysTask.BasicId
/// - "taskClass": 任务类全名
/// - "taskMethod": 任务方法名
/// - "taskParams": JSON 字符串参数
/// </para>
/// </remarks>
public sealed class DynamicJobWorker : IJobWorker
{
    private readonly ILogger<DynamicJobWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="scopeFactory"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public DynamicJobWorker(ILogger<DynamicJobWorker> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <inheritdoc />
    public async Task<JobResult> ExecuteAsync(IJobContext context, CancellationToken cancellationToken = default)
    {
        var jobName = context.JobInstance.JobName;
        _logger.LogInformation("动态任务开始执行: {JobName}, TraceId={TraceId}", jobName, context.TraceId);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

            // 通过 TaskCode 查找 SysTask。
            // 注意：谓词不能写成 (cond ? 列条件 : true) 的三元式——SqlSugar 会把布尔常量分支
            // 翻译成 boolean = integer 比较，PostgreSQL 直接报 42883。
            long? tenantId = context.TenantId;
            var tasks = tenantId is > 0
                ? await repository.GetListAsync(task => task.TaskCode == jobName && task.TenantId == tenantId.Value)
                : await repository.GetListAsync(task => task.TaskCode == jobName);

            var sysTask = tasks.FirstOrDefault();
            if (sysTask is null)
            {
                _logger.LogWarning("未找到对应 SysTask: {JobName}", jobName);
                return JobResult.Failure($"未找到对应任务配置: {jobName}");
            }

            // 验证任务状态
            if (sysTask.Status == EnableStatus.Disabled)
            {
                _logger.LogWarning("任务已禁用，跳过执行: {JobName}", jobName);
                return JobResult.Success(new { Skipped = true, Reason = "Disabled" });
            }

            if (string.IsNullOrWhiteSpace(sysTask.TaskClass))
            {
                _logger.LogWarning("任务未配置 TaskClass，跳过执行: {JobName}", jobName);
                return JobResult.Failure("TaskClass 未配置");
            }

            var result = await InvokeTaskMethodAsync(sysTask, scope.ServiceProvider, cancellationToken);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("动态任务已取消: {JobName}", jobName);
            return JobResult.Canceled();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "动态任务执行失败: {JobName}", jobName);
            return JobResult.Failure($"动态任务执行失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 通过反射调用 SysTask 配置的类和方法
    /// </summary>
    private async Task<JobResult> InvokeTaskMethodAsync(
        SysTask sysTask,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        var type = Type.GetType(sysTask.TaskClass);
        if (type is null)
        {
            // 尝试从已加载的程序集中查找
            type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException) { return []; }
                })
                .FirstOrDefault(t => t.FullName == sysTask.TaskClass || t.Name == sysTask.TaskClass);
        }

        if (type is null)
        {
            return JobResult.Failure($"无法加载任务类型: {sysTask.TaskClass}");
        }

        // 尝试通过 DI 创建实例，失败则使用 Activator
        object? instance = null;
        try
        {
            instance = ActivatorUtilities.CreateInstance(serviceProvider, type);
        }
        catch
        {
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                return JobResult.Failure($"无法创建任务实例: {type.FullName}, 错误: {ex.Message}");
            }
        }

        if (instance is null)
        {
            return JobResult.Failure($"无法创建任务实例: {type.FullName}");
        }

        // 解析参数
        object?[] methodParams = [];
        var methodName = sysTask.TaskMethod ?? "Execute";
        MethodInfo? method = null;

        if (!string.IsNullOrWhiteSpace(sysTask.TaskParams))
        {
            try
            {
                // 尝试将 JSON 参数反序列化为目标方法的参数
                method = type.GetMethod(methodName);
                if (method is not null)
                {
                    var paramInfos = method.GetParameters();
                    if (paramInfos.Length > 0)
                    {
                        var jsonDoc = JsonDocument.Parse(sysTask.TaskParams);
                        methodParams = paramInfos.Select(p =>
                        {
                            if (p.ParameterType == typeof(CancellationToken))
                            {
                                return (object)cancellationToken;
                            }
                            if (jsonDoc.RootElement.TryGetProperty(p.Name!, out var element))
                            {
                                return JsonSerializer.Deserialize(element.GetRawText(), p.ParameterType)!;
                            }

                            // 无匹配参数：有缺省值用缺省值（p.DefaultValue 在无缺省值时是 DBNull，
                            // 直传必炸）；值类型给零值，引用类型给 null
                            return p.HasDefaultValue
                                ? p.DefaultValue
                                : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
                        }).ToArray();
                    }
                }
            }
            catch (JsonException ex)
            {
                // JSON 非法时按"无参数"继续（原实现把整串塞成唯一参数，几乎必然导致找不到方法）
                _logger.LogWarning(ex, "任务 {TaskCode} 的 TaskParams 不是合法 JSON，已按无参调用处理", sysTask.TaskCode);
                methodParams = [];
            }
        }

        // 查找方法
        method ??= type.GetMethod(methodName, methodParams.Select(p => p?.GetType() ?? typeof(object)).ToArray());
        if (method is null)
        {
            // 兜底：尝试无参方法
            method = type.GetMethod(methodName, Type.EmptyTypes);
            if (method is not null)
            {
                methodParams = [];
            }
            else
            {
                return JobResult.Failure($"未找到方法: {type.FullName}.{methodName}");
            }
        }

        // 调用方法
        try
        {
            var result = method.Invoke(instance, methodParams);

            if (result is Task taskResult)
            {
                await taskResult.ConfigureAwait(false);

                // 尝试提取 Task<T> 的结果
                var resultType = taskResult.GetType();
                if (resultType.IsGenericType)
                {
                    var value = resultType.GetProperty("Result")?.GetValue(taskResult);
                    return JobResult.Success(value);
                }

                return JobResult.Success();
            }

            return JobResult.Success(result);
        }
        catch (TargetInvocationException ex)
        {
            var inner = ex.InnerException ?? ex;
            _logger.LogError(inner, "反射调用任务方法失败: {Type}.{Method}", type.FullName, methodName);
            return JobResult.Failure($"任务方法执行失败: {inner.Message}", inner);
        }
    }
}

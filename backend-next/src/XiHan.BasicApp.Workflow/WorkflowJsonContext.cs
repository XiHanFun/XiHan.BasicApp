// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Workflow.Operations.Approvals;
using XiHan.BasicApp.Workflow.Operations.Tasks;
using XiHan.BasicApp.Workflow.Operations.WorkflowAssignees;
using XiHan.BasicApp.Workflow.Operations.WorkflowDefinitions;
using XiHan.BasicApp.Workflow.Operations.WorkflowInstances;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流包的序列化上下文。
/// </summary>
/// <remarks>
/// 本包全部请求与响应；枚举按名称字符串（框架的定义、实例与节点状态也是）。
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(GetWorkflowDefinitionRequest))]
[JsonSerializable(typeof(CreateWorkflowDefinitionRequest))]
[JsonSerializable(typeof(UpdateWorkflowDefinitionRequest))]
[JsonSerializable(typeof(PublishWorkflowDefinitionRequest))]
[JsonSerializable(typeof(NewWorkflowDefinitionVersionRequest))]
[JsonSerializable(typeof(WorkflowDefinitionIdsRequest))]
[JsonSerializable(typeof(ListWorkflowBusinessTypesRequest))]
[JsonSerializable(typeof(ListWorkflowActivityTypesRequest))]
[JsonSerializable(typeof(WorkflowDefinitionResponse))]
[JsonSerializable(typeof(WorkflowDefinitionDetailResponse))]
[JsonSerializable(typeof(PageResult<WorkflowDefinitionResponse>))]
[JsonSerializable(typeof(WorkflowBusinessTypesResponse))]
[JsonSerializable(typeof(WorkflowActivityTypesResponse))]
[JsonSerializable(typeof(GetWorkflowInstanceRequest))]
[JsonSerializable(typeof(StartWorkflowInstanceRequest))]
[JsonSerializable(typeof(WorkflowInstanceControlRequest))]
[JsonSerializable(typeof(WorkflowInstanceIdsRequest))]
[JsonSerializable(typeof(SignalWorkflowInstancesRequest))]
[JsonSerializable(typeof(WorkflowInstanceResponse))]
[JsonSerializable(typeof(WorkflowInstanceDetailResponse))]
[JsonSerializable(typeof(PageResult<WorkflowInstanceResponse>))]
[JsonSerializable(typeof(SignalWorkflowInstancesResponse))]
[JsonSerializable(typeof(ListAssigneeUsersRequest))]
[JsonSerializable(typeof(ListAssigneeOptionsRequest))]
[JsonSerializable(typeof(AssigneeUsersResponse))]
[JsonSerializable(typeof(AssigneeOptionsResponse))]
[JsonSerializable(typeof(AssigneeDepartmentsResponse))]
[JsonSerializable(typeof(PageResult<WorkflowTaskResponse>))]
[JsonSerializable(typeof(CompleteTaskRequest))]
[JsonSerializable(typeof(TransferTaskRequest))]
[JsonSerializable(typeof(AddTaskAssigneesRequest))]
[JsonSerializable(typeof(WorkflowTaskActionResponse))]
[JsonSerializable(typeof(PageResult<WorkflowApprovalRecordResponse>))]
[JsonSerializable(typeof(PageResult<WorkflowApprovalInstanceResponse>))]
[JsonSerializable(typeof(GetApprovalRequest))]
[JsonSerializable(typeof(WorkflowApprovalDetailResponse))]
[JsonSerializable(typeof(WithdrawApprovalRequest))]
[JsonSerializable(typeof(WithdrawApprovalResponse))]
public sealed partial class WorkflowJsonContext : JsonSerializerContext;

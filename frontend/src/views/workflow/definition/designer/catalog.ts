/**
 * 活动类型目录：驱动节点面板与属性表单（与后端内置活动 WorkflowActivityTypes 一一对应）
 */

export type ActivityPropInput = 'text' | 'textarea' | 'number' | 'boolean' | 'select' | 'tags' | 'json'

export interface ActivityPropDescriptor {
  /** 节点 properties 键名（与后端活动属性键一致） */
  key: string
  /** i18n 键（workflow.designer.prop 下） */
  labelKey: string
  input: ActivityPropInput
  options?: { label: string, value: string }[]
  placeholder?: string
}

export interface ActivityTypeMeta {
  /** 活动类型编码（后端 WorkflowActivityTypes） */
  type: string
  /** i18n 键（workflow.designer.activity 下） */
  labelKey: string
  icon: string
  /** i18n 键（workflow.designer.category 下） */
  categoryKey: string
  props: ActivityPropDescriptor[]
  /** 常见 outcome 提示（辅助连线条件填写） */
  outcomes?: string[]
}

const control = 'control'
const human = 'human'
const data = 'data'
const integration = 'integration'
const event = 'event'
const tool = 'tool'

export const ACTIVITY_CATALOG: ActivityTypeMeta[] = [
  { type: 'Start', labelKey: 'start', icon: 'lucide:play', categoryKey: control, props: [] },
  { type: 'End', labelKey: 'end', icon: 'lucide:square', categoryKey: control, props: [] },
  {
    type: 'Decision',
    labelKey: 'decision',
    icon: 'lucide:git-fork',
    categoryKey: control,
    props: [],
  },
  { type: 'Parallel', labelKey: 'parallel', icon: 'lucide:split', categoryKey: control, props: [] },
  {
    type: 'Join',
    labelKey: 'join',
    icon: 'lucide:merge',
    categoryKey: control,
    props: [
      { key: 'Mode', labelKey: 'join_mode', input: 'select', options: [
        { label: 'WaitAll', value: 'WaitAll' },
        { label: 'WaitAny', value: 'WaitAny' },
      ] },
    ],
  },
  {
    type: 'Delay',
    labelKey: 'delay',
    icon: 'lucide:timer',
    categoryKey: control,
    props: [
      { key: 'DurationSeconds', labelKey: 'duration_seconds', input: 'text', placeholder: '300 或 waitSeconds * 60' },
    ],
  },
  {
    type: 'SubWorkflow',
    labelKey: 'sub_workflow',
    icon: 'lucide:workflow',
    categoryKey: control,
    outcomes: ['completed'],
    props: [
      { key: 'DefinitionCode', labelKey: 'definition_code', input: 'text' },
      { key: 'DefinitionVersion', labelKey: 'definition_version', input: 'number' },
      { key: 'Variables', labelKey: 'variables_literal', input: 'json' },
      { key: 'VariableExpressions', labelKey: 'variable_expressions', input: 'json' },
      { key: 'WaitForCompletion', labelKey: 'wait_for_completion', input: 'boolean' },
      { key: 'ResultVariable', labelKey: 'result_variable', input: 'text' },
      { key: 'FailOnChildFault', labelKey: 'fail_on_child_fault', input: 'boolean' },
    ],
  },
  {
    type: 'ForEach',
    labelKey: 'for_each',
    icon: 'lucide:repeat',
    categoryKey: control,
    props: [
      { key: 'ItemsExpression', labelKey: 'items_expression', input: 'text', placeholder: 'orderLines' },
      { key: 'ItemVariableName', labelKey: 'item_variable_name', input: 'text', placeholder: 'item' },
      { key: 'DefinitionCode', labelKey: 'definition_code', input: 'text' },
      { key: 'DefinitionVersion', labelKey: 'definition_version', input: 'number' },
      { key: 'Parallel', labelKey: 'parallel_mode', input: 'boolean' },
      { key: 'FailFast', labelKey: 'fail_fast', input: 'boolean' },
      { key: 'Variables', labelKey: 'variables_literal', input: 'json' },
      { key: 'ResultVariable', labelKey: 'result_variable', input: 'text' },
    ],
  },
  { type: 'Terminate', labelKey: 'terminate', icon: 'lucide:octagon-x', categoryKey: control, props: [
    { key: 'Reason', labelKey: 'reason', input: 'text' },
  ] },
  { type: 'Fault', labelKey: 'fault', icon: 'lucide:alert-triangle', categoryKey: control, props: [
    { key: 'Message', labelKey: 'message', input: 'text' },
  ] },
  {
    type: 'UserTask',
    labelKey: 'user_task',
    icon: 'lucide:user-check',
    categoryKey: human,
    outcomes: ['approved', 'rejected', 'timeout'],
    props: [
      { key: 'Assignees', labelKey: 'assignees', input: 'tags' },
      { key: 'AssigneesExpression', labelKey: 'assignees_expression', input: 'text', placeholder: 'approverIds' },
      { key: 'CompletionPolicy', labelKey: 'completion_policy', input: 'select', options: [
        { label: 'Any', value: 'Any' },
        { label: 'All', value: 'All' },
        { label: 'Sequential', value: 'Sequential' },
      ] },
      { key: 'Title', labelKey: 'title', input: 'text', placeholder: '单据 {{billNo}} 审批' },
      { key: 'FormData', labelKey: 'form_data', input: 'json' },
      { key: 'CcUserIds', labelKey: 'cc_user_ids', input: 'tags' },
      { key: 'AllowedOutcomes', labelKey: 'allowed_outcomes', input: 'tags' },
    ],
  },
  {
    type: 'SetVariable',
    labelKey: 'set_variable',
    icon: 'lucide:variable',
    categoryKey: data,
    props: [
      { key: 'Values', labelKey: 'values_literal', input: 'json' },
      { key: 'Expressions', labelKey: 'expressions', input: 'json' },
    ],
  },
  {
    type: 'Http',
    labelKey: 'http',
    icon: 'lucide:globe',
    categoryKey: integration,
    props: [
      { key: 'Url', labelKey: 'url', input: 'text', placeholder: 'https://api.example.com/orders/{{orderId}}' },
      { key: 'Method', labelKey: 'method', input: 'select', options: [
        { label: 'GET', value: 'GET' },
        { label: 'POST', value: 'POST' },
        { label: 'PUT', value: 'PUT' },
        { label: 'DELETE', value: 'DELETE' },
        { label: 'PATCH', value: 'PATCH' },
      ] },
      { key: 'Headers', labelKey: 'headers', input: 'json' },
      { key: 'Body', labelKey: 'body', input: 'textarea' },
      { key: 'ContentType', labelKey: 'content_type', input: 'text', placeholder: 'application/json' },
      { key: 'TimeoutSeconds', labelKey: 'timeout_seconds', input: 'number' },
      { key: 'ResultVariable', labelKey: 'result_variable', input: 'text' },
      { key: 'FailOnErrorStatus', labelKey: 'fail_on_error_status', input: 'boolean' },
    ],
  },
  {
    type: 'Script',
    labelKey: 'script',
    icon: 'lucide:code',
    categoryKey: integration,
    props: [
      { key: 'Code', labelKey: 'code', input: 'textarea', placeholder: 'variables["total"] = 1; return null;' },
      { key: 'ResultVariable', labelKey: 'result_variable', input: 'text' },
    ],
  },
  {
    type: 'PublishEvent',
    labelKey: 'publish_event',
    icon: 'lucide:megaphone',
    categoryKey: event,
    props: [
      { key: 'EventName', labelKey: 'event_name', input: 'text' },
      { key: 'Payload', labelKey: 'payload', input: 'json' },
    ],
  },
  {
    type: 'WaitSignal',
    labelKey: 'wait_signal',
    icon: 'lucide:radio',
    categoryKey: event,
    props: [
      { key: 'SignalName', labelKey: 'signal_name', input: 'text', placeholder: 'order-paid' },
      { key: 'AcceptAnyCorrelation', labelKey: 'accept_any_correlation', input: 'boolean' },
    ],
  },
  {
    type: 'Log',
    labelKey: 'log',
    icon: 'lucide:file-text',
    categoryKey: tool,
    props: [
      { key: 'Message', labelKey: 'message', input: 'text', placeholder: '订单 {{orderId}} 已进入审批' },
      { key: 'Level', labelKey: 'level', input: 'select', options: [
        { label: 'Debug', value: 'Debug' },
        { label: 'Information', value: 'Information' },
        { label: 'Warning', value: 'Warning' },
        { label: 'Error', value: 'Error' },
      ] },
    ],
  },
]

export const ACTIVITY_MAP: Record<string, ActivityTypeMeta> = Object.fromEntries(
  ACTIVITY_CATALOG.map(meta => [meta.type, meta]),
)

/** 面板分类顺序 */
export const CATEGORY_ORDER = [control, human, data, integration, event, tool]

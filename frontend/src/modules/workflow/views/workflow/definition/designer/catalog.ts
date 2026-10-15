/**
 * 活动类型目录：驱动节点面板与属性表单（与后端内置活动 WorkflowActivityTypes 一一对应）
 */

export type ActivityPropInput = 'text' | 'textarea' | 'number' | 'boolean' | 'select' | 'tags' | 'json' | 'assignees'

/**
 * 属性面板的分节（i18n 键 workflow.designer.section 下）：basic 与节点名称同一节，
 * participants 放受理人与抄送人，form 放表单与办理结果，props 是未归节属性的缺省节。
 */
export type ActivityPropGroup = 'basic' | 'participants' | 'form' | 'props'

/** 分节在面板里的先后（执行策略不属于活动属性，由面板固定排在最后） */
export const PROP_GROUP_ORDER: readonly ActivityPropGroup[] = ['basic', 'participants', 'form', 'props']

export interface ActivityPropDescriptor {
  /** 节点 properties 键名（与后端活动属性键一致） */
  key: string
  /** i18n 键（workflow.designer.prop 下） */
  labelKey: string
  input: ActivityPropInput
  /** 下拉选项；labelKey（workflow.designer.option 下）存在时按语言显示，否则显示 label */
  options?: { label: string, value: string, labelKey?: string }[]
  placeholder?: string
  /** 所属分节；未写时归 props */
  group?: ActivityPropGroup
  /** 留空时后端取的值（只用作下拉的占位提示，不写进定义） */
  defaultValue?: string
  /** 只在另一属性取某值时才有意义；已经填过值的照样显示，不把配置藏起来 */
  visibleWhen?: { key: string, equals: string }
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
  {
    type: 'End',
    labelKey: 'end',
    icon: 'lucide:square',
    categoryKey: control,
    props: [
      // 结果结束节点：声明后回写审批业务并立即结束整个实例（审批业务流程的每个结束节点都必须声明）
      { key: 'Outcome', labelKey: 'end_outcome', input: 'select', options: [
        { label: 'approved', value: 'approved', labelKey: 'outcome_approved' },
        { label: 'rejected', value: 'rejected', labelKey: 'outcome_rejected' },
      ] },
    ],
  },
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
      // 受理人只按规则配置：旧版 Assignees / AssigneesExpression / CcUserIds 打开时由 transform 并入规则
      { key: 'AssigneeRules', labelKey: 'assignee_rules', input: 'assignees', group: 'participants' },
      { key: 'ExcludeStarter', labelKey: 'exclude_starter', input: 'boolean', group: 'participants' },
      // 两个下拉留空时后端分别取或签、报错（UserTaskActivity 的 TryResolvePolicy / TryResolveEmptyStrategy）
      { key: 'CompletionPolicy', labelKey: 'completion_policy', input: 'select', group: 'participants', defaultValue: 'Any', options: [
        { label: 'Any', value: 'Any', labelKey: 'policy_any' },
        { label: 'All', value: 'All', labelKey: 'policy_all' },
        { label: 'Sequential', value: 'Sequential', labelKey: 'policy_sequential' },
      ] },
      { key: 'EmptyAssigneeStrategy', labelKey: 'empty_assignee_strategy', input: 'select', group: 'participants', defaultValue: 'Fault', options: [
        { label: 'Fault', value: 'Fault', labelKey: 'empty_fault' },
        { label: 'AutoApprove', value: 'AutoApprove', labelKey: 'empty_auto_approve' },
        { label: 'AutoReject', value: 'AutoReject', labelKey: 'empty_auto_reject' },
        { label: 'Fallback', value: 'Fallback', labelKey: 'empty_fallback' },
      ] },
      // 兜底受理人只在「转交兜底受理人」时才用得上
      { key: 'FallbackAssigneeRules', labelKey: 'fallback_assignee_rules', input: 'assignees', group: 'participants', visibleWhen: { key: 'EmptyAssigneeStrategy', equals: 'Fallback' } },
      { key: 'Title', labelKey: 'title', input: 'text', group: 'basic', placeholder: '单据 {{billNo}} 审批' },
      { key: 'FormData', labelKey: 'form_data', input: 'json', group: 'form' },
      { key: 'CcRules', labelKey: 'cc_rules', input: 'assignees', group: 'participants' },
      { key: 'AllowedOutcomes', labelKey: 'allowed_outcomes', input: 'tags', group: 'form' },
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

function isFilled(value: unknown): boolean {
  if (value === null || value === undefined || value === '')
    return false
  return !Array.isArray(value) || value.length > 0
}

/** 属性此刻是否该出现在面板里：条件不满足、但已经填过值的照样显示 */
export function isPropVisible(descriptor: ActivityPropDescriptor, properties: Record<string, unknown>): boolean {
  const when = descriptor.visibleWhen
  if (!when)
    return true
  return properties[when.key] === when.equals || isFilled(properties[descriptor.key])
}

/**
 * 把活动属性按分节排好：分节按 PROP_GROUP_ORDER，节内保持目录里的先后；没有可见属性的节不出现。
 */
export function groupActivityProps(meta: ActivityTypeMeta | undefined, properties: Record<string, unknown>): Array<{ group: ActivityPropGroup, props: ActivityPropDescriptor[] }> {
  const visible = (meta?.props ?? []).filter(descriptor => isPropVisible(descriptor, properties))
  return PROP_GROUP_ORDER
    .map(group => ({ group, props: visible.filter(descriptor => (descriptor.group ?? 'props') === group) }))
    .filter(section => section.props.length > 0)
}

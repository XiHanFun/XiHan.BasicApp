/**
 * 流程设计器上 BasicApp 的设计器扩展：只放应用语义，节点类型与属性都来自工作流引擎预设。
 *
 * - 受理人规则：预设登记的编辑方式由 AssigneeRulesEditor 提供（选人、选角色查应用自己的数据）。
 * - 流程编码：先选关联的审批业务再看编码；编辑已有草稿时只读。
 * - 启动变量：每行变量名与是否必填，换掉设计器内建的四列表格（属性面板里放不下四列）。
 * - 连线条件：条件表达式加源节点常见办理结果的一键填入。
 * - 规则：见 workflow-rules.ts。
 */

import type { AppContext } from 'vue'
import type { WorkflowBusinessTypeDto } from '../../../../api'
import type { FlowDesignerExtension, FlowDocument, FlowEdge, FlowPropertyEditorContext, XhFlowDesigner } from '~/designers/flow'
import { h } from 'vue'
import { createVuePropertyEditor } from '~/designers'
import { XIHAN_WORKFLOW_ASSIGNEE_RULES, XIHAN_WORKFLOW_START_VARIABLES } from '~/designers/flow'
import AssigneeRulesEditor from './AssigneeRulesEditor.vue'
import EdgeConditionEditor from './EdgeConditionEditor.vue'
import FlowCodeEditor from './FlowCodeEditor.vue'
import FlowVariablesEditor from './FlowVariablesEditor.vue'
import { createWorkflowRules, WORKFLOW_EXTENSION_ID, WORKFLOW_ISSUE_KEYS } from './workflow-rules'

/** 源节点类型 → 常见办理结果（连线条件一键填入） */
const OUTCOME_HINTS: Readonly<Record<string, readonly string[]>> = {
  UserTask: ['approved', 'rejected', 'timeout'],
  SubWorkflow: ['completed'],
}

export interface WorkflowExtensionOptions {
  /** 编辑器挂在这个应用上下文下，照常能用 i18n、Pinia 与应用组件 */
  appContext: AppContext | null
  /** 编辑已有草稿：编码与关联业务只读（后端更新草稿沿用原编码） */
  lockCode: boolean
  /** 已登记的审批业务 */
  businessTypes: () => readonly WorkflowBusinessTypeDto[]
  /** 设计器里此刻的流程定义（响应式），编辑器据它给提示 */
  document: () => FlowDocument | undefined
  /** 设计器元素；关联业务时经它一步写入编码、名称与业务变量 */
  designer: () => XhFlowDesigner | null
  /** 问题说明的文案 */
  t: (key: string) => string
}

type Context = FlowPropertyEditorContext

function variableNames(value: unknown): string[] {
  if (!Array.isArray(value))
    return []
  return value.flatMap(item => (typeof item === 'object' && item !== null && typeof (item as { name?: unknown }).name === 'string')
    ? [((item as { name: string }).name).trim()]
    : []).filter(Boolean)
}

/** 表达式可引用的变量：关联业务声明的变量 + 定义里声明的启动变量 */
function knownVariables(options: WorkflowExtensionOptions): string[] {
  const data = options.document()?.data
  const code = typeof data?.code === 'string' ? data.code.trim() : ''
  const business = options.businessTypes().find(item => item.code === code)
  return [...new Set([...(business?.variables.map(variable => variable.name) ?? []), ...variableNames(data?.variables)])]
}

/** 关联审批业务：编码改成业务编码，名称为空时带上业务名，补齐业务声明的变量；一步撤销 */
function bindBusiness(options: WorkflowExtensionOptions, business: WorkflowBusinessTypeDto): void {
  const designer = options.designer()
  if (!designer)
    return
  const data = { ...designer.schema.data }
  const variables = Array.isArray(data.variables) ? [...data.variables] : []
  const declared = new Set(variableNames(variables))
  for (const variable of business.variables) {
    if (!declared.has(variable.name))
      variables.push({ name: variable.name, required: false, defaultValue: null, description: variable.label ?? null })
  }
  const name = typeof data.name === 'string' && data.name.trim() ? data.name : business.name
  designer.execute([{ type: 'updateDocument', patch: { data: { ...data, code: business.code, name, variables } } }])
}

function sourceOutcomes(options: WorkflowExtensionOptions, context: Context): readonly string[] {
  if (context.target.length !== 1)
    return []
  const edge = context.target[0]!.value as unknown as FlowEdge
  const source = options.document()?.nodes.find(node => node.id === edge.source?.node)
  return source ? OUTCOME_HINTS[source.type] ?? [] : []
}

export function createWorkflowExtension(options: WorkflowExtensionOptions): FlowDesignerExtension {
  const editor = (render: (context: Context) => ReturnType<typeof h>) => createVuePropertyEditor(options.appContext, render)

  return {
    id: WORKFLOW_EXTENSION_ID,
    rules: createWorkflowRules({
      businessOutcomes: code => options.businessTypes().find(item => item.code === code)?.outcomes,
    }),
    issueText: (issue) => {
      const key = WORKFLOW_ISSUE_KEYS[issue.code]
      return key ? options.t(`workflow.designer.validate.${key}`) : undefined
    },
    editors: {
      byControl: {
        [XIHAN_WORKFLOW_ASSIGNEE_RULES]: editor(context => h(AssigneeRulesEditor, {
          'value': context.value,
          'label': context.descriptor.label ?? '',
          'variables': knownVariables(options),
          'onUpdate:value': (rules: unknown) => {
            context.setValue((rules ?? undefined) as Context['value'])
            context.commit()
          },
        })),
        [XIHAN_WORKFLOW_START_VARIABLES]: editor(context => h(FlowVariablesEditor, {
          'value': context.value,
          'readonly': context.readonly,
          'onUpdate:value': (variables: unknown) => context.setValue(variables as Context['value']),
          'onCommit': () => context.commit(),
        })),
      },
      byTarget: {
        document: {
          'data.code': editor(context => h(FlowCodeEditor, {
            'code': typeof context.value === 'string' ? context.value : undefined,
            'label': context.descriptor.label,
            'locked': options.lockCode,
            'readonly': context.readonly,
            'businessTypes': options.businessTypes(),
            'onUpdate:code': (code: string) => context.setValue(code),
            'onCommit': () => context.commit(),
            'onBind': (business: WorkflowBusinessTypeDto) => bindBusiness(options, business),
          })),
        },
        edge: {
          'data.condition': editor(context => h(EdgeConditionEditor, {
            'value': typeof context.value === 'string' ? context.value : undefined,
            'label': context.descriptor.label,
            'readonly': context.readonly,
            'outcomes': sourceOutcomes(options, context),
            'onUpdate:value': (condition: string | undefined) => context.setValue(condition),
            'onCommit': () => context.commit(),
          })),
        },
      },
    },
  }
}

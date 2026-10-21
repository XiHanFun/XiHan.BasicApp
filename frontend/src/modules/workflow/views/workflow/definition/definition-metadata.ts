/**
 * 流程定义的元数据：编码、名称、分类、描述。列表页的新建、编辑弹窗读写引擎定义 JSON 里的这几项，别的字段原样留着；
 * 节点、连线、启动变量与执行设置在设计器里改。
 */

import type { WorkflowBusinessTypeDto } from '../../../api'

export interface WorkflowDefinitionMetadata {
  code: string
  name: string
  category: string
  description: string
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function parseDefinition(json: string): Record<string, unknown> {
  const raw: unknown = JSON.parse(json)
  if (!isRecord(raw))
    throw new TypeError('definition must be a JSON object')
  return raw
}

function text(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

export function emptyDefinitionMetadata(): WorkflowDefinitionMetadata {
  return { code: '', name: '', category: '', description: '' }
}

/** 从引擎定义 JSON 读出元数据；JSON 不是对象时抛错 */
export function readDefinitionMetadata(json: string): WorkflowDefinitionMetadata {
  const raw = parseDefinition(json)
  return { code: text(raw.code), name: text(raw.name), category: text(raw.category), description: text(raw.description) }
}

/** 编码与名称都填了 */
export function isDefinitionMetadataComplete(metadata: WorkflowDefinitionMetadata): boolean {
  return Boolean(metadata.code.trim() && metadata.name.trim())
}

/** 选了审批业务：编码换成业务编码（流程编码即业务编码），名称空着时带上业务名 */
export function bindDefinitionBusiness(metadata: WorkflowDefinitionMetadata, business: WorkflowBusinessTypeDto): WorkflowDefinitionMetadata {
  return { ...metadata, code: business.code, name: metadata.name.trim() ? metadata.name : business.name }
}

/** 新建：一份没有节点与连线的空白流程；编码是某个审批业务时带上它声明的变量，与设计器里绑定业务时一样 */
export function createDefinitionJson(metadata: WorkflowDefinitionMetadata, businessTypes: readonly WorkflowBusinessTypeDto[]): string {
  const code = metadata.code.trim()
  const business = businessTypes.find(item => item.code === code)
  const variables = (business?.variables ?? []).map(variable => ({
    name: variable.name,
    required: false,
    defaultValue: null,
    description: variable.label ?? null,
  }))
  return JSON.stringify({
    code,
    name: metadata.name.trim(),
    category: metadata.category.trim() || null,
    description: metadata.description.trim() || null,
    variables,
    nodes: [],
    transitions: [],
  }, null, 2)
}

/** 编辑：只改名称、分类、描述；编码与其它字段原样（后端更新草稿也沿用原编码） */
export function applyDefinitionMetadata(json: string, metadata: WorkflowDefinitionMetadata): string {
  const raw = parseDefinition(json)
  return JSON.stringify({
    ...raw,
    name: metadata.name.trim(),
    category: metadata.category.trim() || null,
    description: metadata.description.trim() || null,
  }, null, 2)
}

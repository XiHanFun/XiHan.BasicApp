/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 流程定义的元数据：编码、名称、分类、描述。列表页的新建、编辑弹窗读写定义文档（框架 的 WorkflowJson）里的这几项，别的字段原样留着；
 * 节点、连线、启动变量与执行设置在设计器里改
 *
 * 界面的叫法：「流程编码」是 WorkflowJson 的 name（定义名，同名各版本共用、关联审批业务时即业务码，创建后不可改），
 * 「流程名称」是 displayName
 */

import type { ResponseOf } from '~/contracts'

/** 流程定义列表的一行 */
export type WorkflowDefinitionRow = ResponseOf<'workflow-definitions.list'>['items'][number]
/** 流程定义详情（带整份 WorkflowJson） */
export type WorkflowDefinitionDetail = ResponseOf<'workflow-definitions.get'>
/** 已登记的审批业务：业务码即流程编码，显示名与变量名是文案键 */
export type WorkflowBusinessType = ResponseOf<'workflow-definitions.business-types'>['items'][number]

export interface WorkflowDefinitionMetadata {
  code: string
  name: string
  category: string
  description: string
}

/** 流程编码（框架的 WorkflowName）：小写字母或数字开头，其后是小写字母、数字、点、下划线与连字符，共 1～128 个字符 */
const WORKFLOW_NAME = /^[a-z0-9][a-z0-9._-]{0,127}$/

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
  return typeof value === 'string'
    ? value
    : ''
}

export function emptyDefinitionMetadata(): WorkflowDefinitionMetadata {
  return {
    code: '',
    name: '',
    category: '',
    description: '',
  }
}

/** 流程编码合不合框架的定义名语法（去掉首尾空白后判） */
export function isWorkflowName(code: string): boolean {
  return WORKFLOW_NAME.test(code.trim())
}

/** 从定义文档读出元数据；JSON 不是对象时抛错 */
export function readDefinitionMetadata(json: string): WorkflowDefinitionMetadata {
  const raw = parseDefinition(json)
  return {
    code: text(raw.name),
    name: text(raw.displayName),
    category: text(raw.category),
    description: text(raw.description),
  }
}

/** 编码与名称都填了，且编码合语法 */
export function isDefinitionMetadataComplete(metadata: WorkflowDefinitionMetadata): boolean {
  return Boolean(isWorkflowName(metadata.code) && metadata.name.trim())
}

/**
 * 绑定审批业务：流程编码即业务编码，名称空着时带上业务名
 * businessName 是已翻译的显示名
 */
export function bindDefinitionBusiness(metadata: WorkflowDefinitionMetadata, business: WorkflowBusinessType, businessName: string): WorkflowDefinitionMetadata {
  return {
    ...metadata,
    code: business.code,
    name: metadata.name.trim()
      ? metadata.name
      : businessName,
  }
}

/**
 * 新建一份没有节点与连线的空白流程，ID 与版本由后端定
 * 编码是某个审批业务时带上它声明的变量，与设计器里绑定业务时一样
 * translate 把业务变量的显示名文案键翻成当前语言，写进变量说明
 */
export function createDefinitionJson(metadata: WorkflowDefinitionMetadata, businessTypes: readonly WorkflowBusinessType[], translate: (key: string) => string): string {
  const code = metadata.code.trim()
  const business = businessTypes.find(item => item.code === code)
  const variables = (business?.variables ?? []).map(variable => ({
    name: variable.name,
    required: false,
    description: translate(variable.labelKey),
  }))
  return JSON.stringify({
    name: code,
    displayName: metadata.name.trim(),
    category: metadata.category.trim() || null,
    description: metadata.description.trim() || null,
    variables,
    nodes: [],
    transitions: [],
  }, null, 2)
}

/** 编辑：只改名称、分类、描述；编码与其它字段原样（后端改草稿也沿用已存的名字） */
export function applyDefinitionMetadata(json: string, metadata: WorkflowDefinitionMetadata): string {
  const raw = parseDefinition(json)
  return JSON.stringify({
    ...raw,
    displayName: metadata.name.trim(),
    category: metadata.category.trim() || null,
    description: metadata.description.trim() || null,
  }, null, 2)
}

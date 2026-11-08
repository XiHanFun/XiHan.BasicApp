/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { XihanWorkflowDefinition } from '@xihan-flow/core/presets/xihan-workflow'

/**
 * 框架 的 WorkflowJson ↔ XiHan 工作流引擎预设（@xihan-flow）的定义
 *
 * 后端收发的是框架 的 WorkflowJson（camelCase），预设有自己的定义格式，两边逐项换算：
 * - 定义：WorkflowJson 的 name（定义名，即预设的编码）↔ code，displayName ↔ name；
 * - 节点与连线：displayName ↔ name；节点的 timeout（TimeSpan 文本 d.hh:mm:ss / hh:mm:ss）↔ timeoutSeconds（秒），
 *   重试策略的 firstDelay ↔ firstDelaySeconds；
 * - 设计器的画面数据：WorkflowJson 的 properties.designerLayout / designerEdges / designerExtensions（对象）
 *   ↔ 预设的 extraProperties 里同名的 JSON 字符串；properties 里别的键原样搬进定义的 properties，预设不认、原样往返；
 * - 节点属性里预设与框架 叫法不同的四个键：DurationSeconds ↔ Duration（延时）、ItemsExpression ↔ Items、
 *   ItemVariableName ↔ ItemVariable（遍历）、DefinitionCode ↔ DefinitionName（子流程与遍历）；
 * - 缺 id 的节点与连线补上合法的元素 id（框架的 [A-Za-z0-9][A-Za-z0-9._-]{0,63}），不交给预设补出含 -> 与 # 的 id；
 * - 回写时去掉预设才有、框架不认的 creationTime、publishTime 与数字形式的 tenantId
 *
 * 没列出的字段原样往返，值为 null 的字段去掉（WorkflowJson 不写 null，二者同义）；往返两次的结果一致
 */

/** WorkflowJson 里的节点重试策略：firstDelay 是 TimeSpan 文本 */
export interface WorkflowJsonRetryPolicy {
  readonly maxAttempts?: number
  readonly firstDelay?: string
  readonly backoffFactor?: number
  readonly [key: string]: unknown
}

/** WorkflowJson 里的节点 */
export interface WorkflowJsonNode {
  readonly id?: string
  readonly activityType?: string
  readonly displayName?: string | null
  readonly properties?: Readonly<Record<string, unknown>>
  readonly retryPolicy?: WorkflowJsonRetryPolicy | null
  /** 挂起超时，TimeSpan 文本（d.hh:mm:ss 或 hh:mm:ss） */
  readonly timeout?: string | null
  readonly continueOnError?: boolean
  readonly [key: string]: unknown
}

/** WorkflowJson 里的连线 */
export interface WorkflowJsonTransition {
  readonly id?: string
  readonly sourceNodeId?: string
  readonly targetNodeId?: string
  readonly condition?: string | null
  readonly priority?: number
  readonly isDefault?: boolean
  readonly displayName?: string | null
  readonly [key: string]: unknown
}

/** 框架 的流程定义（WorkflowJson）；没列出的字段原样往返 */
export interface WorkflowJsonDefinition {
  readonly id?: string
  /** 定义名（同名的各版本共用；关联审批业务时即业务码） */
  readonly name?: string
  readonly version?: number
  readonly displayName?: string | null
  readonly description?: string | null
  readonly category?: string | null
  readonly status?: string
  readonly enableCompensation?: boolean
  readonly variables?: readonly Readonly<Record<string, unknown>>[]
  readonly nodes?: readonly WorkflowJsonNode[]
  readonly transitions?: readonly WorkflowJsonTransition[]
  /** 应用的扩展属性：设计器的画面数据放在 designerLayout / designerEdges / designerExtensions */
  readonly properties?: Readonly<Record<string, unknown>> | null
  readonly [key: string]: unknown
}

type JsonRecord = Record<string, unknown>

/** 设计器画面数据的键：WorkflowJson 的 properties 下是对象，预设的 extraProperties 下是 JSON 字符串 */
const DESIGNER_KEYS: ReadonlySet<string> = new Set(['designerLayout', 'designerEdges', 'designerExtensions'])

/** 预设才有、框架不认的定义字段 */
const LEGACY_ONLY_KEYS: ReadonlySet<string> = new Set(['creationTime', 'publishTime'])

/** 回写 WorkflowJson 时排在前面的字段，其余按原顺序跟在后面 */
const DEFINITION_ORDER = [
  'id',
  'name',
  'version',
  'displayName',
  'description',
  'category',
  'status',
  'enableCompensation',
] as const
const DEFINITION_TAIL = [
  'variables',
  'nodes',
  'transitions',
  'properties',
] as const
const NODE_ORDER = [
  'id',
  'activityType',
  'displayName',
  'properties',
  'retryPolicy',
  'timeout',
  'continueOnError',
] as const
const TRANSITION_ORDER = [
  'id',
  'sourceNodeId',
  'targetNodeId',
  'condition',
  'priority',
  'isDefault',
  'displayName',
] as const
const RETRY_ORDER = ['maxAttempts', 'firstDelay', 'backoffFactor'] as const

/** TimeSpan 的常量格式（c）：[d.]hh:mm[:ss[.fffffff]]，或只有天数 */
const TIME_SPAN = /^(?:(\d+)\.)?(\d{1,2}):(\d{1,2})(?::(\d{1,2})(?:\.(\d{1,7}))?)?$/
const DAYS_ONLY = /^\d+$/
const TICKS_PER_SECOND = 10_000_000

function fail(message: string): never {
  throw new TypeError(`[flow] WorkflowJson: ${message}`)
}

function isRecord(value: unknown): value is JsonRecord {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

function present(value: unknown): boolean {
  return value !== null && value !== undefined
}

/** 按给定顺序排出字段：列出的在前、其余按原顺序在后，值为 null 或缺省的去掉 */
function ordered(source: JsonRecord, head: readonly string[], tail: readonly string[] = []): JsonRecord {
  const result: JsonRecord = {}
  const listed = new Set([
    ...head,
    ...tail,
  ])
  for (const key of head) {
    if (present(source[key]))
      result[key] = source[key]
  }
  for (const [key, value] of Object.entries(source)) {
    if (!listed.has(key) && present(value))
      result[key] = value
  }
  for (const key of tail) {
    if (present(source[key]))
      result[key] = source[key]
  }
  return result
}

/** 取一个没用过的元素 id：前缀加从 1 起的序号，与设计器新建节点、连线的写法一致 */
function freeId(prefix: string, used: Set<string>): string {
  let index = 1
  while (used.has(`${prefix}${index}`))
    index++
  const id = `${prefix}${index}`
  used.add(id)
  return id
}

function hasId(value: unknown): value is JsonRecord & { id: string } {
  return isRecord(value) && typeof value.id === 'string' && value.id !== ''
}

/** 缺 id 的节点与连线补上 id；已有的原样保留 */
function withIds(items: readonly unknown[], prefixOf: (item: JsonRecord) => string): unknown[] {
  const used = new Set(items.filter(hasId).map(item => item.id))
  return items.map((item) => {
    if (!isRecord(item) || hasId(item))
      return item
    return {
      ...item,
      id: freeId(prefixOf(item), used),
    }
  })
}

function nodePrefix(node: JsonRecord): string {
  return typeof node.activityType === 'string' && /^[A-Z0-9][\w.-]*$/i.test(node.activityType)
    ? `${node.activityType}_`
    : 'node_'
}

function listOf(value: unknown, field: string): readonly unknown[] {
  if (value === undefined || value === null)
    return []
  if (!Array.isArray(value))
    fail(`${field} must be an array`)
  return value
}

/**
 * TimeSpan 文本换成秒数（可带小数）
 * @param value TimeSpan 的常量格式，如 01:00:00、1.02:03:04、00:00:00.5000000
 */
export function timeSpanToSeconds(value: string): number {
  const text = value.trim()
  if (DAYS_ONLY.test(text))
    return Number(text) * 86_400
  const match = TIME_SPAN.exec(text)
  if (!match)
    fail(`'${value}' is not a TimeSpan`)
  const [, days = '0', hours, minutes, seconds = '0', fraction = ''] = match
  if (Number(hours) > 23 || Number(minutes) > 59 || Number(seconds) > 59)
    fail(`'${value}' is not a TimeSpan`)
  const whole = Number(days) * 86_400 + Number(hours) * 3_600 + Number(minutes) * 60 + Number(seconds)
  return fraction
    ? whole + Number(fraction.padEnd(7, '0')) / TICKS_PER_SECOND
    : whole
}

/**
 * 秒数换成 TimeSpan 文本，写法同框架序列化
 * 不满一天写 hh:mm:ss，满一天写 d.hh:mm:ss，有小数时带七位小数
 * @param seconds 非负的秒数
 */
export function secondsToTimeSpan(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds < 0)
    fail(`${seconds} seconds is not a TimeSpan`)
  const ticks = Math.round(seconds * TICKS_PER_SECOND)
  const total = Math.floor(ticks / TICKS_PER_SECOND)
  const fraction = ticks % TICKS_PER_SECOND
  const pad = (part: number) => String(part).padStart(2, '0')
  const days = Math.floor(total / 86_400)
  const clock = `${pad(Math.floor(total % 86_400 / 3_600))}:${pad(Math.floor(total % 3_600 / 60))}:${pad(total % 60)}`
  return `${days > 0
    ? `${days}.`
    : ''}${clock}${fraction > 0
    ? `.${String(fraction).padStart(7, '0')}`
    : ''}`
}

// ── WorkflowJson → 预设定义 ─────────────────────────────────

/** 预设与框架 叫法不同的节点属性：前者是预设的键，后者是 WorkflowJson 的键 */
const PROPERTY_RENAMES: readonly (readonly [string, string])[] = [
  ['DurationSeconds', 'Duration'],
  ['ItemsExpression', 'Items'],
  ['ItemVariableName', 'ItemVariable'],
  ['DefinitionCode', 'DefinitionName'],
]

/**
 * 按对照表改节点属性的键，别的键与顺序不变
 * direction 0：预设 → WorkflowJson，1：WorkflowJson → 预设
 */
function renameProperties(properties: unknown, direction: 0 | 1): unknown {
  if (!isRecord(properties))
    return properties
  const renamed: JsonRecord = {}
  for (const [key, value] of Object.entries(properties)) {
    const pair = PROPERTY_RENAMES.find(names => names[direction] === key)
    renamed[pair
      ? pair[direction === 0
        ? 1
        : 0]
      : key] = value
  }
  return renamed
}

function retryFromJson(policy: JsonRecord): JsonRecord {
  const { firstDelay, ...rest } = policy
  return {
    ...rest,
    ...(typeof firstDelay === 'string'
      ? {
          firstDelaySeconds: timeSpanToSeconds(firstDelay),
        }
      : {}),
  }
}

function nodeFromJson(node: unknown): unknown {
  if (!isRecord(node))
    return node
  const { displayName, timeout, retryPolicy, properties, ...rest } = node
  return {
    ...rest,
    ...(properties !== undefined
      ? {
          properties: renameProperties(properties, 1),
        }
      : {}),
    ...(typeof displayName === 'string' && displayName !== ''
      ? { name: displayName }
      : {}),
    ...(isRecord(retryPolicy)
      ? {
          retryPolicy: retryFromJson(retryPolicy),
        }
      : {}),
    ...(typeof timeout === 'string'
      ? {
          timeoutSeconds: timeSpanToSeconds(timeout),
        }
      : {}),
  }
}

function transitionFromJson(transition: unknown): unknown {
  if (!isRecord(transition))
    return transition
  const { displayName, ...rest } = transition
  return {
    ...rest,
    ...(typeof displayName === 'string' && displayName !== ''
      ? { name: displayName }
      : {}),
  }
}

/**
 * WorkflowJson 换成预设的定义，交给 fromWorkflowDefinition 打开
 * 结构不对（不是对象、nodes / transitions 不是数组、TimeSpan 文本不合法）时抛 TypeError
 */
export function fromWorkflowJson(document: WorkflowJsonDefinition): XihanWorkflowDefinition {
  if (!isRecord(document))
    fail('must be an object')
  const { name, displayName, nodes, transitions, properties, ...rest } = document
  const designer: Record<string, string> = {}
  const others: JsonRecord = {}
  if (isRecord(properties)) {
    for (const [key, value] of Object.entries(properties)) {
      if (DESIGNER_KEYS.has(key)) {
        if (present(value)) {
          designer[key] = typeof value === 'string'
            ? value
            : JSON.stringify(value)
        }
      }
      else {
        others[key] = value
      }
    }
  }
  const definition: JsonRecord = {
    ...rest,
    ...(typeof name === 'string'
      ? { code: name }
      : {}),
    ...(typeof displayName === 'string' && displayName !== ''
      ? { name: displayName }
      : {}),
    nodes: withIds(listOf(nodes, 'nodes'), nodePrefix).map(nodeFromJson),
    transitions: withIds(listOf(transitions, 'transitions'), () => 'edge_').map(transitionFromJson),
    ...(Object.keys(others).length > 0
      ? { properties: others }
      : {}),
    ...(Object.keys(designer).length > 0
      ? { extraProperties: designer }
      : {}),
  }
  return definition as XihanWorkflowDefinition
}

// ── 预设定义 → WorkflowJson ─────────────────────────────────

/**
 * extraProperties 里的 JSON 串解析回对象
 * 解析不出时原样留着字符串，交给后端报错，不吞掉
 */
function parseDesignerValue(value: string): unknown {
  try {
    return JSON.parse(value) as unknown
  }
  catch {
    return value
  }
}

function retryToJson(policy: JsonRecord): JsonRecord {
  const { firstDelaySeconds, ...rest } = policy
  return ordered({
    ...rest,
    ...(typeof firstDelaySeconds === 'number'
      ? {
          firstDelay: secondsToTimeSpan(firstDelaySeconds),
        }
      : {}),
  }, RETRY_ORDER)
}

function nodeToJson(node: unknown): unknown {
  if (!isRecord(node))
    return node
  const { name, timeoutSeconds, retryPolicy, properties, ...rest } = node
  return ordered({
    ...rest,
    ...(properties !== undefined
      ? {
          properties: renameProperties(properties, 0),
        }
      : {}),
    ...(typeof name === 'string' && name !== ''
      ? { displayName: name }
      : {}),
    ...(isRecord(retryPolicy)
      ? {
          retryPolicy: retryToJson(retryPolicy),
        }
      : {}),
    ...(typeof timeoutSeconds === 'number'
      ? {
          timeout: secondsToTimeSpan(timeoutSeconds),
        }
      : {}),
  }, NODE_ORDER)
}

function transitionToJson(transition: unknown): unknown {
  if (!isRecord(transition))
    return transition
  const { name, ...rest } = transition
  return ordered({
    ...rest,
    ...(typeof name === 'string' && name !== ''
      ? { displayName: name }
      : {}),
  }, TRANSITION_ORDER)
}

/**
 * 预设写出的定义（toWorkflowDefinition 的结果）换成 WorkflowJson，交给后端保存
 */
export function toWorkflowJson(definition: XihanWorkflowDefinition): WorkflowJsonDefinition {
  if (!isRecord(definition))
    fail('must be an object')
  const { code, name, nodes, transitions, extraProperties, properties, tenantId, ...rest } = definition as JsonRecord
  const merged: JsonRecord = isRecord(properties)
    ? { ...properties }
    : {}
  if (isRecord(extraProperties)) {
    for (const [key, value] of Object.entries(extraProperties)) {
      if (present(value)) {
        merged[key] = DESIGNER_KEYS.has(key) && typeof value === 'string'
          ? parseDesignerValue(value)
          : value
      }
    }
  }
  const kept = Object.fromEntries(Object.entries(rest).filter(([key]) => !LEGACY_ONLY_KEYS.has(key)))
  return ordered({
    ...kept,
    // 框架的租户是字符串；预设的数字租户 id 框架不认
    ...(typeof tenantId === 'string'
      ? { tenantId }
      : {}),
    ...(typeof code === 'string'
      ? { name: code }
      : {}),
    ...(typeof name === 'string' && name !== ''
      ? { displayName: name }
      : {}),
    nodes: withIds(listOf(nodes, 'nodes'), nodePrefix).map(nodeToJson),
    transitions: withIds(listOf(transitions, 'transitions'), () => 'edge_').map(transitionToJson),
    ...(Object.keys(merged).length > 0
      ? { properties: merged }
      : {}),
  }, DEFINITION_ORDER, DEFINITION_TAIL) as WorkflowJsonDefinition
}

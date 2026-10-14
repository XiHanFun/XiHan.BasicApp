import type { LogDetailSchema } from './log-detail.types'
import { describe, expect, it } from 'vitest'
import {
  accessLogDetailSchema,
  apiLogDetailSchema,
  diffLogDetailSchema,
  exceptionLogDetailSchema,
  loginLogDetailSchema,
  migrationHistoryDetailSchema,
  operationLogDetailSchema,
  permissionChangeLogDetailSchema,
} from './log-detail-fields'
import { buildLogDetailView, LOG_DETAIL_GROUP_ORDER } from './log-detail-view'

const t = (key: string) => key
const format = (_: unknown, value: unknown) => `<${String(value)}>`

const SCHEMAS: Record<string, LogDetailSchema> = {
  access: accessLogDetailSchema(t),
  api: apiLogDetailSchema(t),
  diff: diffLogDetailSchema(t),
  exception: exceptionLogDetailSchema(t),
  login: loginLogDetailSchema(t),
  migration: migrationHistoryDetailSchema(t),
  operation: operationLogDetailSchema(t),
  permissionChange: permissionChangeLogDetailSchema(t),
}

describe('日志详情定义', () => {
  it.each(Object.entries(SCHEMAS))('%s：抬头引用的字段都在字段表里，字段键不重复', (_, schema) => {
    const keys = schema.fields.map(field => field.key)
    const header = schema.header
    const headerKeys = [...header.title, ...(header.status ? [header.status] : []), ...(header.badges ?? []), ...header.meta, ...(header.timeKey ? [header.timeKey] : [])]

    expect(new Set(keys).size).toBe(keys.length)
    expect(headerKeys.filter(key => !keys.includes(key))).toEqual([])
  })

  it.each(Object.entries(SCHEMAS))('%s：不再列出重复的审计列', (_, schema) => {
    expect(schema.fields.map(field => field.key).filter(key => ['createdTime', 'createdId', 'createdBy'].includes(key))).toEqual([])
  })

  it.each(Object.entries(SCHEMAS))('%s：结果字段带语气', (_, schema) => {
    const status = schema.fields.find(field => field.key === schema.header.status)
    expect(status?.tone).toBeTypeOf('function')
  })
})

describe('日志详情视图', () => {
  const schema = operationLogDetailSchema(t)
  const record = {
    operationType: 'Create',
    title: 'WorkflowDefinition.NewVersion',
    result: 'Success',
    executionTime: 35,
    userName: 'owner',
    operationTime: '2026-10-14 16:53:30',
    operationIp: '127.0.0.1',
    operationLocation: '',
    module: 'WorkflowDefinition',
    function: 'NewVersion',
    method: 'POST',
    requestUrl: '/api/WorkflowDefinition/NewVersion',
    traceId: '9ef5a8c3',
    basicId: 207795978470952990n.toString(),
    errorMessage: null,
    userAgent: 'Mozilla/5.0',
    createdTime: '2026-10-14 16:53:30',
  }
  const view = buildLogDetailView(schema, record, format)

  it('抬头：标题按顺序拼接，结果带语气，耗时成标签，副信息跳过空值', () => {
    expect(view.title).toBe('<Create> · <WorkflowDefinition.NewVersion>')
    expect(view.status).toMatchObject({ text: '<Success>', tone: 'success' })
    expect(view.badges.map(badge => badge.field.key)).toEqual(['executionTime'])
    expect(view.meta.map(entry => entry.field.key)).toEqual(['userName', 'operationTime', 'operationIp'])
  })

  it('分组按固定顺序、跳过空组，抬头用过的字段不重复出现', () => {
    expect(view.groups.map(group => group.group)).toEqual(['subject', 'request', 'trace'])
    const groupKeys = view.groups.flatMap(group => group.entries.map(entry => entry.field.key))
    expect(groupKeys).toEqual(['module', 'function', 'method', 'requestUrl', 'traceId', 'basicId'])
    expect(LOG_DETAIL_GROUP_ORDER[0]).toBe('subject')
  })

  it('代码块只留有值的，审计列这类定义里没有的键不出现', () => {
    expect(view.blocks.map(block => block.field.key)).toEqual(['userAgent'])
    expect(JSON.stringify(view)).not.toContain('createdTime')
  })

  it('没有语气的结果字段退回成普通标签', () => {
    const plain: LogDetailSchema = { header: { title: ['name'], status: 'state', meta: [] }, fields: [{ key: 'name', label: 'name' }, { key: 'state', label: 'state' }] }
    const result = buildLogDetailView(plain, { name: 'a', state: 'b' }, format)

    expect(result.status).toBeUndefined()
    expect(result.badges.map(badge => badge.text)).toEqual(['<b>'])
  })

  it('对象值的原文按 JSON 保留，复制与追踪拿到的是原值', () => {
    const json: LogDetailSchema = { header: { title: [], meta: [] }, fields: [{ key: 'extendData', label: 'x', type: 'code' }] }
    const result = buildLogDetailView(json, { extendData: { a: 1 } }, format)

    expect(result.blocks[0]?.raw).toBe('{"a":1}')
  })
})

import type { LogDetailSchema } from './log-detail.types'
import { describe, expect, it } from 'vitest'
import {
  accessLogDetailSchema,
  conditionHitDetailSchema,
  diffLogDetailSchema,
  exceptionLogDetailSchema,
  loginLogDetailSchema,
  openApiLogDetailSchema,
  operationLogDetailSchema,
  permissionChangeLogDetailSchema,
} from './log-detail-fields'
import { buildLogDetailView, LOG_DETAIL_GROUP_ORDER } from './log-detail-view'

const t = (key: string) => key
const format = (_: unknown, value: unknown) => `<${String(value)}>`

const SCHEMAS: Record<string, LogDetailSchema> = {
  access: accessLogDetailSchema(t),
  openApi: openApiLogDetailSchema(t),
  diff: diffLogDetailSchema(t),
  exception: exceptionLogDetailSchema(t),
  login: loginLogDetailSchema(t),
  operation: operationLogDetailSchema(t),
  permissionChange: permissionChangeLogDetailSchema(t),
  conditionHit: conditionHitDetailSchema(t),
}

describe('日志详情定义', () => {
  it.each(Object.entries(SCHEMAS))('%s：抬头引用的字段都在字段表里，字段键不重复', (_, schema) => {
    const keys = schema.fields.map(field => field.key)
    const header = schema.header
    const headerKeys = [...header.title, ...(header.status ? [header.status] : []), ...(header.badges ?? []), ...header.meta, ...(header.timeKey ? [header.timeKey] : [])]

    expect(new Set(keys).size).toBe(keys.length)
    expect(headerKeys.filter(key => !keys.includes(key))).toEqual([])
  })

  it.each(Object.entries(SCHEMAS))('%s：发生时间是跳链路追踪的时间点，trace-id 可追踪', (_, schema) => {
    expect(schema.header.timeKey).toBe('occurredAt')
    expect(schema.fields.find(field => field.key === 'traceId')?.trace).toBe('TraceId')
  })

  it.each(Object.entries(SCHEMAS).filter(([, schema]) => schema.header.status))('%s：结果字段带语气', (_, schema) => {
    const status = schema.fields.find(field => field.key === schema.header.status)
    expect(status?.tone).toBeTypeOf('function')
  })

  it('操作日志的详情带描述与写入时记下的客户端信息，客户端地址可追踪', () => {
    const fields = SCHEMAS.operation!.fields
    expect(fields.map(field => field.key)).toEqual(expect.arrayContaining(['description', 'clientIp', 'location', 'browser', 'os', 'device', 'userAgent']))
    expect(fields.find(field => field.key === 'clientIp')?.trace).toBe('ClientIp')
    expect(SCHEMAS.operation!.header.meta).toContain('clientIp')
  })

  it('枚举字段带上契约字面量的选项（6.0 没有枚举元数据接口）', () => {
    const result = SCHEMAS.access!.fields.find(field => field.key === 'result')!
    expect(result.options?.map(option => option.value)).toEqual(['Success', 'Failed', 'Forbidden', 'Unauthorized', 'NotFound', 'ServerError'])
    expect(result.tone?.('Forbidden')).toBe('warning')
  })
})

describe('日志详情视图', () => {
  const schema = accessLogDetailSchema(t)
  const record = {
    id: '0199f2a4-6a3e-7000-8000-000000000001',
    occurredAt: '2026-10-29T08:00:00Z',
    traceId: '9ef5a8c3',
    userId: null,
    userName: 'admin',
    sessionId: null,
    clientIp: '',
    userAgent: 'Mozilla/5.0',
    operationId: 'roles.grant',
    protocol: 'http',
    method: 'POST',
    path: '/api/roles/1:grant',
    result: 'Success',
    statusCode: 200,
    errorCode: null,
    errorMessage: null,
    durationMs: 35,
    browser: 'Chrome',
    os: null,
    device: null,
    referer: null,
  }
  const view = buildLogDetailView(schema, record, format)

  it('抬头：标题按顺序拼接，结果带语气，状态码与耗时成标签，副信息跳过空值', () => {
    expect(view.title).toBe('<POST> · <roles.grant>')
    expect(view.status).toMatchObject({ text: '<Success>', tone: 'success' })
    expect(view.badges.map(badge => badge.field.key)).toEqual(['statusCode', 'durationMs'])
    expect(view.meta.map(entry => entry.field.key)).toEqual(['userName', 'occurredAt'])
  })

  it('分组按固定顺序、跳过空组，抬头用过的字段不重复出现', () => {
    expect(view.groups.map(group => group.group)).toEqual(['request', 'client', 'trace'])
    const groupKeys = view.groups.flatMap(group => group.entries.map(entry => entry.field.key))
    expect(groupKeys).toEqual(['protocol', 'path', 'browser', 'traceId', 'id'])
    expect(LOG_DETAIL_GROUP_ORDER[0]).toBe('subject')
  })

  it('代码块只留有值的', () => {
    expect(view.blocks.map(block => block.field.key)).toEqual(['userAgent'])
  })

  it('没有语气的结果字段退回成普通标签', () => {
    const plain: LogDetailSchema = { header: { title: ['name'], status: 'state', meta: [] }, fields: [{ key: 'name', label: 'name' }, { key: 'state', label: 'state' }] }
    const result = buildLogDetailView(plain, { name: 'a', state: 'b' }, format)

    expect(result.status).toBeUndefined()
    expect(result.badges.map(badge => badge.text)).toEqual(['<b>'])
  })

  it('对象值的原文按 JSON 保留，复制与追踪拿到的是原值', () => {
    const json: LogDetailSchema = { header: { title: [], meta: [] }, fields: [{ key: 'details', label: 'x', type: 'code' }] }
    const result = buildLogDetailView(json, { details: { a: 1 } }, format)

    expect(result.blocks[0]?.raw).toBe('{"a":1}')
  })
})

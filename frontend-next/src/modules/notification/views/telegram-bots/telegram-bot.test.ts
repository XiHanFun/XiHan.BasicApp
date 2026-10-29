import type { TelegramBotForm, TelegramBotRow } from './telegram-bot'
import { describe, expect, it } from 'vitest'
import { toPageRequest } from '~/contracts'
import { configureRequest } from '~/request'
import { emptyTelegramBotForm, findTelegramBotProblem, isCommandList, isIdList, TELEGRAM_BOT_QUERY, telegramBotProblemMessage, telegramBotResource, toCreateBody, toTelegramBotForm, toUpdateBody } from './telegram-bot'

const t = (key: string) => key

function json(status: number, body: object): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

function stub(...responses: object[]): { method: string, url: string, body: unknown }[] {
  const calls: { method: string, url: string, body: unknown }[] = []
  configureRequest({
    baseUrl: '',
    getToken: () => 't',
    fetch: (async (url: RequestInfo | URL, init?: RequestInit) => {
      calls.push({ method: init?.method ?? 'GET', url: String(url), body: init?.body ? JSON.parse(String(init.body)) : undefined })
      return json(200, responses[calls.length - 1] ?? {})
    }) as unknown as typeof fetch,
  })
  return calls
}

const TOKEN = '123456789:AAEhBP0av28cdef_ghij-klmn'

function form(patch: Partial<TelegramBotForm> = {}): TelegramBotForm {
  return { ...emptyTelegramBotForm(), name: 'ops', token: TOKEN, ...patch }
}

const ROW: TelegramBotRow = {
  id: 'g1',
  name: 'ops',
  hasToken: true,
  adminUsers: '123456789',
  allowedGroupChatIds: '-1001234567890',
  allowedCommands: 'start,help',
  enableFallbackReply: true,
  broadcastChatIds: '-1001234567890,-1009876543210',
  isEnabled: true,
  order: 0,
  remark: null,
  createdAt: '2026-10-29T00:00:00Z',
  modifiedAt: null,
}

describe('平台托管的 Telegram 机器人：列表筛选', () => {
  it('是否启用与是否兜底回复都按 true / false 精确匹配', () => {
    const request = toPageRequest({
      page: 1,
      pageSize: 20,
      filters: { keyword: 'ops', isEnabled: 'true', enableFallbackReply: 'false' },
      conditionFilters: [],
      sorts: [{ field: 'name', order: 'asc' }],
    }, TELEGRAM_BOT_QUERY)
    expect(request.filters).toEqual([
      { field: 'isEnabled', operator: 'eq', value: 'true' },
      { field: 'enableFallbackReply', operator: 'eq', value: 'false' },
    ])
    expect(request.keyword).toBe('ops')
    expect(request.sorts).toEqual([{ field: 'name', descending: false }])
  })
})

describe('平台托管的 Telegram 机器人：清单字段', () => {
  it('管理员、群组白名单与广播群：逗号分隔的 64 位整数，可为负、可带空白与空项（与后端 long.TryParse 一致）', () => {
    expect(isIdList('')).toBe(true)
    expect(isIdList('123456789, -1001234567890')).toBe(true)
    expect(isIdList('1,,2, ')).toBe(true)
    expect(isIdList('+5')).toBe(true)
    expect(isIdList('-9223372036854775808,9223372036854775807')).toBe(true)
    expect(isIdList('9223372036854775808')).toBe(false)
    expect(isIdList('12a')).toBe(false)
    expect(isIdList('1.5')).toBe(false)
    expect(isIdList('1 2')).toBe(false)
  })

  it('命令白名单：去掉开头的 / 并转小写后只含小写字母、数字与下划线，最长 32', () => {
    expect(isCommandList('')).toBe(true)
    expect(isCommandList('/start, /help')).toBe(true)
    expect(isCommandList('START,my_cmd2')).toBe(true)
    expect(isCommandList(`/${'a'.repeat(32)}`)).toBe(true)
    expect(isCommandList('a'.repeat(33))).toBe(false)
    expect(isCommandList('my-cmd')).toBe(false)
    expect(isCommandList('/')).toBe(false)
    expect(isCommandList('命令')).toBe(false)
  })
})

describe('平台托管的 Telegram 机器人：表单', () => {
  it('提交前找出第一处问题：名称必填；新建要令牌，填了就要合规；编辑留空即保留', () => {
    expect(findTelegramBotProblem(form())).toBeNull()
    expect(findTelegramBotProblem(form({ name: ' ' }))).toBe('name')
    expect(findTelegramBotProblem(form({ token: ' ' }))).toBe('token')
    expect(findTelegramBotProblem(form({ token: '123:short' }))).toBe('token_invalid')
    expect(findTelegramBotProblem(form({ id: 'g1', token: '' }))).toBeNull()
    expect(findTelegramBotProblem(form({ id: 'g1', token: 'abc' }))).toBe('token_invalid')
    expect(findTelegramBotProblem(form({ adminUsers: 'alice' }))).toBe('admin_users')
    expect(findTelegramBotProblem(form({ allowedGroupChatIds: '-100x' }))).toBe('group_chat_ids')
    expect(findTelegramBotProblem(form({ allowedCommands: '/start,/a-b' }))).toBe('commands')
    expect(findTelegramBotProblem(form({ broadcastChatIds: '@channel' }))).toBe('broadcast_chat_ids')
    expect(telegramBotProblemMessage(t, 'broadcast_chat_ids')).toBe('notification.telegram_bots.message.broadcast_chat_ids_invalid')
    expect(telegramBotProblemMessage(t, 'token_invalid')).toBe('notification.telegram_bots.message.token_invalid')
  })

  it('新建带上令牌、空白写 null；编辑回填不带令牌，修改时令牌留空写 null 即保留', () => {
    expect(toCreateBody(form({ token: ` ${TOKEN} `, allowedCommands: ' /start ', broadcastChatIds: ' ' }))).toEqual({
      name: 'ops',
      token: TOKEN,
      adminUsers: null,
      allowedGroupChatIds: null,
      allowedCommands: '/start',
      enableFallbackReply: false,
      broadcastChatIds: null,
      isEnabled: true,
      order: 100,
      remark: null,
    })
    const filled = toTelegramBotForm(ROW)
    expect(filled).toMatchObject({ id: 'g1', token: '', broadcastChatIds: '-1001234567890,-1009876543210', enableFallbackReply: true, remark: '' })
    expect(toUpdateBody({ ...filled, id: 'g1' })).toMatchObject({ id: 'g1', token: null, allowedCommands: 'start,help', remark: null })
  })

  it('启停取最新一份再整体修改（不带令牌）；删除逐项拒绝时翻成文案', async () => {
    const calls = stub(ROW, ROW, { succeeded: [], failed: [{ id: 'g1', code: 'NOTIFICATION_NOTFOUND_009' }] })
    const resource = telegramBotResource(t)
    await resource.updateStatus!('g1', false)
    await expect(resource.remove!('g1')).rejects.toThrow('notification.messaging.failures.bot_not_found')
    expect(calls[0]).toMatchObject({ method: 'GET', url: '/api/telegram-bots/g1' })
    expect(calls[1]).toMatchObject({ method: 'PUT', url: '/api/telegram-bots/g1', body: { isEnabled: false, token: null, broadcastChatIds: '-1001234567890,-1009876543210' } })
    expect(calls[2]).toMatchObject({ method: 'DELETE', url: '/api/telegram-bots?ids=g1' })
  })
})

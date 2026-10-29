import type { Translate } from '../../labels'
import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, RequestOf, ResponseOf } from '~/contracts'
import { telegramBots as telegramBotsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { assertBatch } from '../../labels'

export type TelegramBotRow = ResponseOf<'telegram-bots.list'>['items'][number]

/** 列表的后端筛选：关键字匹配名称；是否启用、是否兜底回复按 true / false 精确匹配（缺省按排序再按名称） */
export const TELEGRAM_BOT_QUERY: PageQueryMapping = {
  filters: { isEnabled: 'eq', enableFallbackReply: 'eq' },
}

/** 令牌文法（与后端一致）：BotFather 给出的「数字:字符串」 */
export const TELEGRAM_TOKEN_PATTERN = /^\d{3,}:[\w-]{20,}$/

const LONG_MIN = -(2n ** 63n)
const LONG_MAX = 2n ** 63n - 1n

/** 逗号分隔的清单：逐项去空白、去掉空项（与后端 `Split` 一致） */
export function splitList(value: string): string[] {
  return value.split(',').map(item => item.trim()).filter(Boolean)
}

/** 管理员、群组白名单与广播群：每项都是 64 位整数（群组的 chat ID 为负数）；空即不填。后端不合规即 NOTIFICATION_VALIDATION_013 */
export function isIdList(value: string): boolean {
  return splitList(value).every(item => /^[+-]?\d+$/.test(item) && BigInt(item) >= LONG_MIN && BigInt(item) <= LONG_MAX)
}

/** 命令白名单：每项去掉开头的 / 并转小写后只含小写字母、数字与下划线，最长 32；空即不限。后端不合规即 NOTIFICATION_VALIDATION_014 */
export function isCommandList(value: string): boolean {
  return splitList(value).every(item => /^[a-z0-9_]{1,32}$/.test(item.replace(/^\/+/, '').toLowerCase()))
}

/** 编辑弹窗的表单 */
export interface TelegramBotForm {
  id: null | string
  name: string
  /** 新的令牌；编辑时留空即保留原值 */
  token: string
  adminUsers: string
  allowedGroupChatIds: string
  allowedCommands: string
  broadcastChatIds: string
  enableFallbackReply: boolean
  isEnabled: boolean
  order: null | number
  remark: string
}

export function emptyTelegramBotForm(): TelegramBotForm {
  return { id: null, name: '', token: '', adminUsers: '', allowedGroupChatIds: '', allowedCommands: '', broadcastChatIds: '', enableFallbackReply: false, isEnabled: true, order: 100, remark: '' }
}

/** 已保存的机器人回填表单（令牌不回显） */
export function toTelegramBotForm(row: TelegramBotRow): TelegramBotForm {
  return {
    id: row.id,
    name: row.name,
    token: '',
    adminUsers: row.adminUsers ?? '',
    allowedGroupChatIds: row.allowedGroupChatIds ?? '',
    allowedCommands: row.allowedCommands ?? '',
    broadcastChatIds: row.broadcastChatIds ?? '',
    enableFallbackReply: row.enableFallbackReply,
    isEnabled: row.isEnabled,
    order: row.order,
    remark: row.remark ?? '',
  }
}

/** 表单的第一处问题（4.x 的提交前校验，加上令牌文法、广播群与命令白名单）；都好为空 */
export type TelegramBotProblem = 'admin_users' | 'broadcast_chat_ids' | 'commands' | 'group_chat_ids' | 'name' | 'token' | 'token_invalid'

export function findTelegramBotProblem(form: TelegramBotForm): TelegramBotProblem | null {
  if (!form.name.trim())
    return 'name'
  const token = form.token.trim()
  if (!form.id && !token)
    return 'token'
  if (token && !TELEGRAM_TOKEN_PATTERN.test(token))
    return 'token_invalid'
  if (!isIdList(form.adminUsers))
    return 'admin_users'
  if (!isIdList(form.allowedGroupChatIds))
    return 'group_chat_ids'
  if (!isCommandList(form.allowedCommands))
    return 'commands'
  if (!isIdList(form.broadcastChatIds))
    return 'broadcast_chat_ids'
  return null
}

export function telegramBotProblemMessage(t: Translate, problem: TelegramBotProblem): string {
  switch (problem) {
    case 'name': return t('notification.telegram_bots.message.input_bot_name')
    case 'token': return t('notification.telegram_bots.message.input_token')
    case 'token_invalid': return t('notification.telegram_bots.message.token_invalid')
    case 'admin_users': return t('notification.telegram_bots.message.admin_users_invalid')
    case 'group_chat_ids': return t('notification.telegram_bots.message.group_chat_ids_invalid')
    case 'commands': return t('notification.telegram_bots.message.commands_invalid')
    case 'broadcast_chat_ids': return t('notification.telegram_bots.message.broadcast_chat_ids_invalid')
  }
}

function blankToNull(value: string): null | string {
  return value.trim() || null
}

/** 新建与修改共用的内容；清单原样交后端规整（去重、命令去掉 / 并转小写），空白写 null */
function draft(form: TelegramBotForm) {
  return {
    name: form.name.trim(),
    adminUsers: blankToNull(form.adminUsers),
    allowedGroupChatIds: blankToNull(form.allowedGroupChatIds),
    allowedCommands: blankToNull(form.allowedCommands),
    enableFallbackReply: form.enableFallbackReply,
    broadcastChatIds: blankToNull(form.broadcastChatIds),
    isEnabled: form.isEnabled,
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
}

/** 新建：令牌必填 */
export function toCreateBody(form: TelegramBotForm): RequestOf<'telegram-bots.create'> {
  return { ...draft(form), token: form.token.trim() }
}

/** 编辑：令牌留空即保留原值 */
export function toUpdateBody(form: TelegramBotForm & { id: string }): RequestOf<'telegram-bots.update'> {
  return { id: form.id, ...draft(form), token: blankToNull(form.token) }
}

/**
 * SchemaPage 的资源：分页、删除与启停。6.0 没有单独的启停操作，启停是取最新一份再整体修改（令牌不带即保留）。
 */
export function telegramBotResource(t: Translate): SchemaResource<TelegramBotRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await telegramBotsApi.list(toPageRequest(params, TELEGRAM_BOT_QUERY)), params),
    remove: async id => assertBatch(t, await telegramBotsApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => {
      const current = await telegramBotsApi.get({ id: String(id) })
      return telegramBotsApi.update(toUpdateBody({ ...toTelegramBotForm(current), id: current.id, isEnabled: enabled }))
    },
  }
}

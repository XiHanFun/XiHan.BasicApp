import type { Tone } from '@xihan-ui/core'
import type { SchemaResource } from '~/components'
import type { ChatConversationType, ChatMemberRole, ChatMessageType, PageQueryMapping, ResponseOf } from '~/contracts'
import { ref, shallowRef } from 'vue'
import { chatAudit, toPageRequest, toSchemaPageResult } from '~/contracts'

export type ChatAuditRow = ResponseOf<'chat-audit.list'>['items'][number]
export type ChatAuditDetail = ResponseOf<'chat-audit.get'>
export type ChatAuditMembers = ResponseOf<'chat-audit.members'>
export type ChatAuditMember = ChatAuditMembers['items'][number]
export type ChatAuditConversation = ChatAuditMembers['conversation']

type Translate = (key: string, values?: Record<string, unknown>) => string

/** 消息类型（契约字面量；审计里能出现全部六种） */
export const CHAT_MESSAGE_TYPES: readonly ChatMessageType[] = [
  'Text',
  'Image',
  'Voice',
  'File',
  'Assistant',
  'System',
]

/**
 * 列表的后端筛选（与后端 ChatAuditQueries 的白名单一致）：关键字匹配正文、发送人名与附件；
 * 会话与发送人精确匹配，消息类型多选（合成 in），发送时间按区间（拆成 ge / le），已撤回精确匹配；
 * includeRecalled 是伪筛选：后端没给 isRecalled 时缺省不含已撤回的，给 includeRecalled=true 即都看
 */
export const CHAT_AUDIT_QUERY: PageQueryMapping = {
  ranges: ['createdAt'],
  multiples: ['messageType'],
  filters: {
    conversationId: 'eq',
    senderUserId: 'eq',
    isRecalled: 'eq',
    includeRecalled: 'eq',
  },
}

/** SchemaPage 的资源：只读分页（4.x 同，没有导出） */
export function chatAuditResource(): SchemaResource<ChatAuditRow> {
  return {
    page: async params => toSchemaPageResult(await chatAudit.list(toPageRequest(params, CHAT_AUDIT_QUERY)), params),
  }
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function conversationTypeLabel(t: Translate, type: ChatConversationType): string {
  switch (type) {
    case 'Group': return t('chat.type.group')
    case 'Department': return t('chat.type.department')
    case 'Assistant': return t('chat.type.assistant')
    default: return t('chat.type.single')
  }
}

export function messageTypeLabel(t: Translate, type: ChatMessageType): string {
  switch (type) {
    case 'Image': return t('chat.audit.type_image')
    case 'Voice': return t('chat.audit.type_voice')
    case 'File': return t('chat.audit.type_file')
    case 'Assistant': return t('chat.audit.type_assistant')
    case 'System': return t('chat.audit.type_system')
    default: return t('chat.audit.type_text')
  }
}

/** 消息类型的语气：文本中性，图片、语音、文件是附件类，助手与系统另起 */
export function messageTypeTone(type: ChatMessageType): Tone {
  switch (type) {
    case 'Image':
    case 'Voice':
    case 'File':
      return 'info'
    case 'Assistant':
      return 'success'
    case 'System':
      return 'warning'
    default:
      return 'neutral'
  }
}

export function memberRoleLabel(t: Translate, role: ChatMemberRole): string {
  switch (role) {
    case 'Owner': return t('chat.role.owner')
    case 'Admin': return t('chat.role.admin')
    default: return t('chat.role.member')
  }
}

/** 角色语气与聊天面板的成员列表一致：群主 warning、管理员 info、成员中性 */
export function memberRoleTone(role: ChatMemberRole): Tone {
  switch (role) {
    case 'Owner': return 'warning'
    case 'Admin': return 'info'
    default: return 'neutral'
  }
}

/**
 * 一次只看一条的按需加载（详情、成员两个弹窗共用）：打开即清掉上一条，换来的结果只认最后一次打开的那条，
 * 关掉或连点别的行时，慢回来的旧结果不回写。失败抛给调用方提示
 */
export function useAuditLoader<TKey, TValue>(load: (key: TKey) => Promise<TValue>) {
  const open = ref(false)
  const loading = ref(false)
  const value = shallowRef<TValue | null>(null)
  let version = 0

  async function show(key: TKey): Promise<void> {
    const current = ++version
    value.value = null
    open.value = true
    loading.value = true
    try {
      const result = await load(key)
      if (current === version)
        value.value = result
    }
    finally {
      if (current === version)
        loading.value = false
    }
  }

  return { open, loading, value, show }
}

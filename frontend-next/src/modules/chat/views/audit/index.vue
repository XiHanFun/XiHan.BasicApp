<script setup lang="ts">
import type { ChatAuditRow } from './use-chat-audit'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { SelectOption } from '~/types'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { chatAudit } from '~/contracts'
import { chatErrorText } from '../../components/chat-helpers'
import ChatAuditDetailDialog from './components/ChatAuditDetailDialog.vue'
import ChatAuditMembersDialog from './components/ChatAuditMembersDialog.vue'
import {
  CHAT_MESSAGE_TYPES,
  chatAuditResource,
  conversationTypeLabel,
  messageTypeLabel,
  messageTypeTone,
  useAuditLoader,
} from './use-chat-audit'

// 聊天审计（后端页面 chat.audit，路由 /chat/audit）：持有审计权限即看当前一侧全部会话的消息，不要求是成员
defineOptions({ name: 'ChatAuditView' })

const { t } = useI18n()
const resource = chatAuditResource()

// 4.x 只有消息列表；6.0 补上消息详情与会话成员（方案 H4）
const detail = useAuditLoader((id: string) => chatAudit.get({ id }))
const members = useAuditLoader((conversationId: string) => chatAudit.members({ conversationId }))

const asRow = (row: object) => row as ChatAuditRow

const messageTypeOptions = computed<SelectOption[]>(() => CHAT_MESSAGE_TYPES.map(type => ({
  label: messageTypeLabel(t, type),
  value: type,
})))
const yesNoOptions = computed<SelectOption[]>(() => [
  {
    label: t('common.statuses.yes'),
    value: 'true',
  },
  {
    label: t('common.statuses.no'),
    value: 'false',
  },
])

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索（只登记后端 ChatAuditQueries 白名单里的筛选与排序） ──
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  {
    key: 'keyword',
    title: t('chat.audit.keyword'),
    dataType: 'string',
    visible: false,
    searchable: true,
    searchPlaceholder: t('chat.audit.keyword_placeholder'),
    width: 220,
    order: 0,
  },
  {
    key: 'conversationId',
    title: t('chat.audit.conversation_id'),
    dataType: 'string',
    visible: false,
    advancedSearch: true,
    order: 1,
  },
  {
    key: 'senderUserId',
    title: t('chat.audit.sender_user_id'),
    dataType: 'string',
    visible: false,
    advancedSearch: true,
    order: 2,
  },
  // 后端缺省不含已撤回的消息；勾「是」即连同已撤回的一起看
  {
    key: 'includeRecalled',
    title: t('chat.audit.include_recalled'),
    dataType: 'boolean',
    visible: false,
    advancedSearch: true,
    options: yesNoOptions.value,
    order: 3,
  },
  // 列
  {
    key: 'id',
    title: t('chat.audit.message_id'),
    dataType: 'string',
    minWidth: 150,
    ellipsis: true,
    order: 10,
  },
  {
    key: 'conversationName',
    title: t('chat.audit.conversation'),
    dataType: 'string',
    minWidth: 140,
    order: 11,
    render: row => asRow(row).conversationName || '-',
  },
  {
    key: 'conversationType',
    title: t('chat.audit.conversation_type'),
    dataType: 'enum',
    width: 100,
    order: 12,
    render: row => renderTag(conversationTypeLabel(t, asRow(row).conversationType), 'neutral'),
  },
  {
    key: 'senderUserName',
    title: t('chat.audit.sender'),
    dataType: 'string',
    minWidth: 110,
    order: 13,
    render: row => asRow(row).senderUserName || (asRow(row).senderUserId
      ? '-'
      : t('chat.audit.sender_system')),
  },
  {
    key: 'messageType',
    title: t('chat.audit.message_type'),
    dataType: 'enum',
    sortable: true,
    searchable: true,
    searchMultiple: true,
    options: messageTypeOptions.value,
    searchPlaceholder: t('chat.audit.message_type_placeholder'),
    width: 110,
    order: 14,
    render: row => renderTag(messageTypeLabel(t, asRow(row).messageType), messageTypeTone(asRow(row).messageType)),
  },
  {
    key: 'content',
    title: t('chat.audit.content'),
    dataType: 'string',
    minWidth: 280,
    ellipsis: true,
    order: 15,
    render: row => asRow(row).content || '-',
  },
  {
    key: 'fileName',
    title: t('chat.audit.file_name'),
    dataType: 'string',
    minWidth: 140,
    ellipsis: true,
    order: 16,
    render: row => asRow(row).fileName || '-',
  },
  {
    key: 'isRecalled',
    title: t('chat.audit.recalled'),
    dataType: 'boolean',
    advancedSearch: true,
    options: yesNoOptions.value,
    width: 90,
    order: 17,
    render: row => (asRow(row).isRecalled
      ? renderTag(t('chat.audit.recalled_yes'), 'danger')
      : renderTag(t('chat.audit.recalled_no'), 'neutral')),
  },
  {
    key: 'editedAt',
    title: t('chat.audit.edited_time'),
    dataType: 'datetime',
    minWidth: 170,
    order: 18,
  },
  {
    key: 'createdAt',
    title: t('chat.audit.created_time'),
    dataType: 'datetime',
    sortable: true,
    searchable: true,
    searchRange: true,
    minWidth: 170,
    order: 19,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'chat.audit',
  pageName: t('chat.audit.title'),
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    {
      key: 'detail',
      title: t('chat.audit.detail'),
      scope: 'row',
      icon: 'lucide:eye',
      permission: 'chat.chat-audit.read',
    },
    {
      key: 'members',
      title: t('chat.audit.members'),
      scope: 'row',
      icon: 'lucide:users',
      permission: 'chat.chat-audit.read',
    },
  ],
}))

function notifyLoadFailed(error: unknown) {
  toast.danger(chatErrorText(error) || t('chat.audit.load_failed'))
}

function openMembers(conversationId: string) {
  members.show(conversationId).catch((error: unknown) => {
    members.open.value = false
    notifyLoadFailed(error)
  })
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  if (!row)
    return
  if (payload.key === 'detail') {
    detail.show(row.id).catch((error: unknown) => {
      detail.open.value = false
      notifyLoadFailed(error)
    })
  }
  else if (payload.key === 'members') {
    openMembers(row.conversationId)
  }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <ChatAuditDetailDialog
      v-model:show="detail.open.value"
      :loading="detail.loading.value"
      :detail="detail.value.value"
      @members="openMembers"
    />
    <ChatAuditMembersDialog
      v-model:show="members.open.value"
      :loading="members.loading.value"
      :members="members.value.value"
    />
  </SchemaPage>
</template>

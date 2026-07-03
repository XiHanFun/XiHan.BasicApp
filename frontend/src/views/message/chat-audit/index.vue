<script setup lang="ts">
import type { ChatAuditListItemDto, PageResult } from '@/api'
import type { ListFieldSchema, PageSchema, SchemaQueryParams } from '~/components'
import { NTag } from 'naive-ui'
import { computed, h } from 'vue'
import { useI18n } from 'vue-i18n'
import { chatAuditApi, createPageRequest, querySortsFromSchema } from '@/api'
import { SchemaPage } from '~/components'
import { getOptionLabel } from '~/utils'

// 路由名须与后端 PageRegistry 的 RouteName（MessageChatAudit）一致
defineOptions({ name: 'MessageChatAudit' })

const { t } = useI18n()

const conversationTypeOptions = computed(() => [
  { label: t('chat.type.single'), value: 'Single' },
  { label: t('chat.type.group'), value: 'Group' },
  { label: t('chat.type.department'), value: 'Department' },
])

const messageTypeOptions = computed(() => [
  { label: t('chat.audit.type_text'), value: 'Text' },
  { label: t('chat.audit.type_image'), value: 'Image' },
  { label: t('chat.audit.type_file'), value: 'File' },
  { label: t('chat.audit.type_system'), value: 'System' },
])

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索 ─────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  { key: 'keyword', title: t('common.fields.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('chat.audit.keyword_placeholder'), width: 220, order: 0 },
  { key: 'conversationId', title: t('chat.audit.conversation_id'), dataType: 'string', visible: false, advancedSearch: true, order: 1 },
  { key: 'senderUserId', title: t('chat.audit.sender_user_id'), dataType: 'string', visible: false, advancedSearch: true, order: 2 },
  // 列
  { key: 'basicId', title: t('chat.audit.message_id'), dataType: 'string', minWidth: 150, order: 10 },
  { key: 'conversationName', title: t('chat.audit.conversation'), dataType: 'string', minWidth: 140, order: 11, render: row => (row as unknown as ChatAuditListItemDto).conversationName || '-' },
  {
    key: 'conversationType',
    title: t('chat.audit.conversation_type'),
    dataType: 'enum',
    options: conversationTypeOptions.value,
    width: 100,
    order: 12,
    render: (row) => {
      const item = row as unknown as ChatAuditListItemDto
      return getOptionLabel(conversationTypeOptions.value, item.conversationType)
    },
  },
  { key: 'senderUserName', title: t('chat.audit.sender'), dataType: 'string', minWidth: 110, order: 13, render: row => (row as unknown as ChatAuditListItemDto).senderUserName || t('chat.audit.sender_system') },
  {
    key: 'messageType',
    title: t('chat.audit.message_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: messageTypeOptions.value,
    searchPlaceholder: t('chat.audit.message_type_placeholder'),
    width: 100,
    order: 14,
    render: (row) => {
      const item = row as unknown as ChatAuditListItemDto
      return getOptionLabel(messageTypeOptions.value, item.messageType)
    },
  },
  { key: 'content', title: t('chat.audit.content'), dataType: 'string', minWidth: 280, ellipsis: true, order: 15, render: row => (row as unknown as ChatAuditListItemDto).content || '-' },
  { key: 'fileName', title: t('chat.audit.file_name'), dataType: 'string', minWidth: 140, order: 16, render: row => (row as unknown as ChatAuditListItemDto).fileName || '-' },
  {
    key: 'isRecalled',
    title: t('chat.audit.recalled'),
    dataType: 'enum',
    width: 90,
    order: 17,
    render: (row) => {
      const item = row as unknown as ChatAuditListItemDto
      return item.isRecalled
        ? h(NTag, { size: 'small', round: true, bordered: false, type: 'error' }, () => t('chat.audit.recalled_yes'))
        : h(NTag, { size: 'small', round: true, bordered: false }, () => t('chat.audit.recalled_no'))
    },
  },
  { key: 'editedTime', title: t('chat.audit.edited_time'), dataType: 'datetime', minWidth: 170, order: 18 },
  { key: 'createdTime', title: t('chat.audit.created_time'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, minWidth: 170, order: 19 },
])

function toStr(v: unknown): string | undefined {
  return (v as string | undefined)?.trim() || undefined
}

/** 查询构建：排序 + 区间(createdTime)/多选(messageType) 走 conditions；会话/发送人主键走显式字段 */
function buildAuditQuery(params: SchemaQueryParams) {
  const f = params.filters
  return {
    ...createPageRequest({
      page: { pageIndex: params.page, pageSize: params.pageSize },
      conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
    }),
    keyword: toStr(f.keyword),
    conversationId: toStr(f.conversationId),
    senderUserId: toStr(f.senderUserId),
  }
}

const schema = computed<PageSchema>(() => ({
  pageCode: 'message.chat-audit',
  pageName: t('chat.audit.page_name'),
  rowKey: 'basicId',
  scrollX: 1700,
  fields: fields.value,
  resource: {
    page: params => chatAuditApi.page(buildAuditQuery(params)) as unknown as Promise<PageResult<Record<string, unknown>>>,
  },
}))
</script>

<template>
  <SchemaPage :schema="schema" />
</template>

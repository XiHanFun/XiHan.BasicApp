<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { ChatAuditCallRow } from '../use-chat-audit'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { SelectOption } from '~/types'
import { computed, shallowRef } from 'vue'
import { useI18n } from 'vue-i18n'
import { renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { ChatCallStatus as CALL_STATUSES, chatAudit, ChatConversationType as CONVERSATION_TYPES, ChatCallEndReason as END_REASONS, ChatCallMedia as MEDIAS } from '~/contracts'
import { callMediaLabel } from '../../../call/call-helpers'
import { chatErrorText } from '../../../components/chat-helpers'
import {
  callEndReasonLabel,
  callOutcomeLabel,
  callOutcomeTone,
  callStatusLabel,
  chatAuditCallResource,
  conversationTypeLabel,
  useAuditLoader,
} from '../use-chat-audit'
import ChatAuditCallDialog from './ChatAuditCallDialog.vue'
import ChatAuditMembersDialog from './ChatAuditMembersDialog.vue'

// 聊天审计的「通话记录」页签：当前一侧全部会话的通话——谁发起的、谁在里面、结果与时长；媒体不经服务端，没有通话内容可看
defineOptions({ name: 'ChatAuditCallsTab' })

const { t } = useI18n()
const resource = chatAuditCallResource()

// 通话详情就是这一行（参与者随列表带回来了），会话成员另取
const showCall = shallowRef(false)
const call = shallowRef<ChatAuditCallRow | null>(null)
const members = useAuditLoader((conversationId: string) => chatAudit.members({ conversationId }))

const asRow = (row: object) => row as ChatAuditCallRow

const mediaOptions = computed<SelectOption[]>(() => MEDIAS.map(media => ({
  label: callMediaLabel(t, media),
  value: media,
})))
const endReasonOptions = computed<SelectOption[]>(() => END_REASONS.map(reason => ({
  label: callEndReasonLabel(t, reason),
  value: reason,
})))
const statusOptions = computed<SelectOption[]>(() => CALL_STATUSES.map(status => ({
  label: callStatusLabel(t, status),
  value: status,
})))
// 会话类型不在这里筛选，选项只为速览卡与导出把字面量换成文案
const conversationTypeOptions = computed<SelectOption[]>(() => CONVERSATION_TYPES.map(type => ({
  label: conversationTypeLabel(t, type),
  value: type,
})))

// ── 字段单一事实源：列 + 常用搜索 + 高级搜索（只登记后端 ChatAuditQueries 白名单里的筛选与排序） ──
const fields = computed<ListFieldSchema[]>(() => [
  // 仅搜索（不作为列）
  {
    key: 'conversationId',
    title: t('chat.audit.conversation_id'),
    dataType: 'string',
    visible: false,
    advancedSearch: true,
    order: 0,
  },
  {
    key: 'initiatorUserId',
    title: t('chat.audit.initiator_user_id'),
    dataType: 'string',
    visible: false,
    advancedSearch: true,
    order: 1,
  },
  {
    key: 'participantUserId',
    title: t('chat.audit.participant_user_id'),
    dataType: 'string',
    visible: false,
    advancedSearch: true,
    order: 2,
  },
  {
    key: 'status',
    title: t('chat.audit.call_status'),
    dataType: 'enum',
    visible: false,
    advancedSearch: true,
    options: statusOptions.value,
    order: 3,
  },
  // 列
  {
    key: 'id',
    title: t('chat.audit.call_id'),
    dataType: 'string',
    minWidth: 150,
    ellipsis: true,
    order: 10,
  },
  {
    key: 'conversationLabel',
    title: t('chat.audit.conversation'),
    dataType: 'string',
    minWidth: 160,
    ellipsis: true,
    order: 11,
  },
  {
    key: 'conversationType',
    title: t('chat.audit.conversation_type'),
    dataType: 'enum',
    options: conversationTypeOptions.value,
    width: 100,
    order: 12,
    render: row => renderTag(conversationTypeLabel(t, asRow(row).conversationType), 'neutral'),
  },
  {
    key: 'initiatorUserName',
    title: t('chat.audit.initiator'),
    dataType: 'string',
    minWidth: 110,
    order: 13,
    render: row => asRow(row).initiatorUserName || '-',
  },
  {
    key: 'media',
    title: t('chat.audit.call_media'),
    dataType: 'enum',
    searchable: true,
    options: mediaOptions.value,
    searchPlaceholder: t('chat.audit.call_media'),
    width: 110,
    order: 14,
    render: row => renderTag(callMediaLabel(t, asRow(row).media), 'info'),
  },
  {
    key: 'endReason',
    title: t('chat.audit.call_outcome'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    options: endReasonOptions.value,
    searchPlaceholder: t('chat.audit.call_outcome'),
    width: 110,
    order: 15,
    render: row => renderTag(callOutcomeLabel(t, asRow(row)), callOutcomeTone(asRow(row))),
  },
  {
    key: 'participantNames',
    title: t('chat.audit.participants'),
    dataType: 'string',
    minWidth: 200,
    ellipsis: true,
    order: 16,
  },
  {
    key: 'durationText',
    title: t('chat.audit.duration'),
    dataType: 'string',
    width: 100,
    order: 17,
    render: row => asRow(row).durationText || '-',
  },
  {
    key: 'startedAt',
    title: t('chat.audit.started_time'),
    dataType: 'datetime',
    sortable: true,
    searchable: true,
    searchRange: true,
    minWidth: 170,
    order: 18,
  },
  {
    key: 'answeredAt',
    title: t('chat.audit.answered_time'),
    dataType: 'datetime',
    minWidth: 170,
    order: 19,
  },
  {
    key: 'endedAt',
    title: t('chat.audit.ended_time'),
    dataType: 'datetime',
    minWidth: 170,
    order: 20,
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'chat.audit.calls',
  pageName: t('chat.audit.tab_calls'),
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

function openMembers(conversationId: string) {
  members.show(conversationId).catch((error: unknown) => {
    members.open.value = false
    toast.danger(chatErrorText(error) || t('chat.audit.load_failed'))
  })
}

function onAction(payload: SchemaActionPayload) {
  const row = payload.row
    ? asRow(payload.row)
    : undefined
  if (!row)
    return
  if (payload.key === 'detail') {
    call.value = row
    showCall.value = true
  }
  else if (payload.key === 'members') {
    openMembers(row.conversationId)
  }
}
</script>

<template>
  <SchemaPage :schema="schema" @action="onAction">
    <ChatAuditCallDialog v-model:show="showCall" :call="call" @members="openMembers" />
    <ChatAuditMembersDialog
      v-model:show="members.open.value"
      :loading="members.loading.value"
      :members="members.value.value"
    />
  </SchemaPage>
</template>

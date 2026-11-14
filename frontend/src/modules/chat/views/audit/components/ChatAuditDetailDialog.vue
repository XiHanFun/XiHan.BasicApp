<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<!--
  聊天审计的消息详情（方案 H4）：消息摘要、所在会话、附件、提及、回复快照、撤回与置顶时间、表情回应
  撤回清空的正文与编辑前的正文后端不另留，这里也就不显示
-->
<script setup lang="ts">
import type { ChatMessageAttachment, ChatReactionItem } from '../../../types'
import type { ChatAuditDetail } from '../use-chat-audit'
import type { XDataTableColumn } from '~/components'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhSpinner } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, XDataTable } from '~/components'
import { formatDate } from '~/utils'
import { formatFileSize } from '../../../components/chat-helpers'
import { auditContentText, conversationTypeLabel, messageTypeLabel } from '../use-chat-audit'

defineOptions({ name: 'ChatAuditDetailDialog' })

const props = defineProps<{
  loading: boolean
  detail: ChatAuditDetail | null
}>()

const emit = defineEmits<{
  /** 看这条消息所在会话的成员 */
  members: [conversationId: string]
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()

function text(value: null | number | string | undefined): string {
  return value === null || value === undefined || value === ''
    ? '-'
    : String(value)
}

function yesNo(value: boolean, at?: null | string): string {
  if (!value)
    return t('common.statuses.no')
  return at
    ? `${t('common.statuses.yes')}（${formatDate(at)}）`
    : t('common.statuses.yes')
}

/** 提及：提及了所有人的写在最前，后面是点名的用户 */
const mentionsText = computed(() => {
  const detail = props.detail
  if (!detail) {
    return '-'
  }
  const mentioned = [
    ...(detail.mentionAll
      ? [
          t('chat.audit.mention_all'),
        ]
      : []),
    ...detail.mentionedUserIds,
  ]
  return mentioned.length
    ? mentioned.join(', ')
    : '-'
})

const attachmentColumns = computed<XDataTableColumn<ChatMessageAttachment>[]>(() => [
  {
    key: 'fileName',
    title: t('chat.audit.file_name'),
    minWidth: 200,
    ellipsis: true,
  },
  {
    key: 'fileSize',
    title: t('chat.audit.file_size'),
    width: 110,
    render: row => formatFileSize(row.fileSize) || '-',
  },
  {
    key: 'durationSeconds',
    title: t('chat.audit.duration'),
    width: 100,
    render: row => (row.durationSeconds == null
      ? '-'
      : `${row.durationSeconds}"`),
  },
  {
    key: 'fileId',
    title: t('chat.audit.file_id'),
    minWidth: 280,
    ellipsis: true,
  },
])

const reactionColumns = computed<XDataTableColumn<ChatReactionItem>[]>(() => [
  {
    key: 'emoji',
    title: t('chat.audit.reaction_emoji'),
    width: 90,
    align: 'center',
  },
  {
    key: 'userName',
    title: t('chat.audit.reaction_user'),
    minWidth: 140,
    ellipsis: true,
    render: row => text(row.userName),
  },
  {
    key: 'userId',
    title: t('chat.audit.user_id'),
    minWidth: 280,
    ellipsis: true,
  },
])
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: calc(var(--xh-space-8) * 25)">
      <XhDialogHeader>
        <XhDialogTitle>
          <div class="det-hd-entity">
            <div class="det-hd-ico">
              <Icon icon="lucide:message-square-text" width="22" height="22" />
            </div>
            <div class="min-w-0">
              <div class="det-hd-name">
                {{ t('chat.audit.detail_title') }}
              </div>
              <div v-if="detail" class="det-hd-sub">
                {{ detail.message.id }}
              </div>
            </div>
          </div>
        </XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody class="xh-overlay-body">
        <div v-if="loading" class="flex justify-center py-12">
          <XhSpinner />
        </div>
        <div v-else-if="detail" class="flex flex-col gap-4">
          <section class="flex flex-col gap-2">
            <h3 class="section-title">
              {{ t('chat.audit.section_message') }}
            </h3>
            <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.sender') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ detail.message.senderUserName || (detail.message.senderUserId ? '-' : t('chat.audit.sender_system')) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.message_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ messageTypeLabel(t, detail.message.messageType) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.sender_user_id') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ text(detail.message.senderUserId) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.seq') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ detail.seq }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.created_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatDate(detail.message.createdAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.edited_time') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatDate(detail.message.editedAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.recalled') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ yesNo(detail.message.isRecalled, detail.recalledAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.pinned') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ yesNo(detail.isPinned, detail.pinnedAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.client_message_id') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ text(detail.clientMessageId) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.reply_to') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ text(detail.replyToMessageId) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
              <XhDescriptionsItem style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('chat.audit.reply_preview') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ text(detail.replyPreview) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('chat.audit.mentions') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ mentionsText }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem style="grid-column: span 2">
                <XhDescriptionsLabel>{{ t('chat.audit.content') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>
                  <span v-if="detail.message.isRecalled && !detail.message.content" class="text-caption text-fg-muted">{{ t('chat.audit.content_recalled') }}</span>
                  <span v-else class="chat-audit-content">{{ text(auditContentText(t, detail.message.messageType, detail.message.content, detail.conversation.conversationType)) }}</span>
                </XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </section>

          <section class="flex flex-col gap-2">
            <h3 class="section-title">
              {{ t('chat.audit.section_conversation') }}
            </h3>
            <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.conversation') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ text(detail.conversation.conversationName) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.conversation_type') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ conversationTypeLabel(t, detail.conversation.conversationType) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.conversation_id') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ detail.conversation.id }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.member_count') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ detail.conversation.memberCount }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.created_at') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatDate(detail.conversation.createdAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
              <XhDescriptionsItem>
                <XhDescriptionsLabel>{{ t('chat.audit.last_message_at') }}</XhDescriptionsLabel>
                <XhDescriptionsValue>{{ formatDate(detail.conversation.lastMessageAt) }}</XhDescriptionsValue>
              </XhDescriptionsItem>
            </XhDescriptionsRoot>
          </section>

          <section class="flex flex-col gap-2">
            <h3 class="section-title">
              {{ t('chat.audit.section_attachments', { n: detail.attachments.length }) }}
            </h3>
            <XDataTable :columns="attachmentColumns" :data="detail.attachments" row-key="fileId" />
          </section>

          <section class="flex flex-col gap-2">
            <h3 class="section-title">
              {{ t('chat.audit.section_reactions', { n: detail.reactions.length }) }}
            </h3>
            <XDataTable :columns="reactionColumns" :data="detail.reactions" :row-key="(row: ChatReactionItem) => `${row.userId}:${row.emoji}`" />
          </section>
        </div>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton v-if="detail" variant="subtle" size="sm" @click="emit('members', detail.conversation.id)">
          <Icon icon="lucide:users" />
          {{ t('chat.audit.members') }}
        </XhButton>
        <XhButton variant="subtle" size="sm" @click="show = false">
          {{ t('common.actions.close') }}
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.chat-audit-content {
  white-space: pre-wrap;
  word-break: break-word;
}
</style>

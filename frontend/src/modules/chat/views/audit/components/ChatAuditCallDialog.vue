<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<!--
  聊天审计的通话详情：一通电话的摘要 + 参与者表（发起人在前）
  只有状态与时间：媒体在浏览器之间直连、不经服务端，没有通话内容可看；表格与列表页同样式
-->
<script setup lang="ts">
import type { ChatAuditCallParticipant, ChatAuditCallRow } from '../use-chat-audit'
import type { XDataTableColumn } from '~/components'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, renderTag, XDataTable } from '~/components'
import { VNodeRender } from '~/components/common/VNodeRender'
import { formatDate } from '~/utils'
import { callMediaIcon, callMediaLabel } from '../../../call/call-helpers'
import { callOutcomeLabel, callOutcomeTone, conversationTypeLabel, participantStatusLabel, participantStatusTone } from '../use-chat-audit'

defineOptions({ name: 'ChatAuditCallDialog' })

defineProps<{
  call: ChatAuditCallRow | null
}>()

const emit = defineEmits<{
  /** 看这通电话所在会话的成员 */
  members: [conversationId: string]
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()

function text(value: null | number | string | undefined): string {
  return value === null || value === undefined || value === ''
    ? '-'
    : String(value)
}

const columns = computed<XDataTableColumn<ChatAuditCallParticipant>[]>(() => [
  {
    key: 'displayName',
    title: t('chat.audit.display_name'),
    minWidth: 140,
    ellipsis: true,
    render: row => text(row.displayName),
  },
  {
    key: 'userName',
    title: t('chat.audit.user_name'),
    minWidth: 120,
    ellipsis: true,
    render: row => text(row.userName),
  },
  {
    key: 'status',
    title: t('chat.audit.participant_status'),
    width: 110,
    render: row => renderTag(participantStatusLabel(t, row.status), participantStatusTone(row.status)),
  },
  {
    key: 'joinedAt',
    title: t('chat.audit.joined_time'),
    minWidth: 170,
    render: row => formatDate(row.joinedAt),
  },
  {
    key: 'leftAt',
    title: t('chat.audit.left_time'),
    minWidth: 170,
    render: row => formatDate(row.leftAt),
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
              <Icon :icon="callMediaIcon(call?.media)" width="22" height="22" />
            </div>
            <div class="min-w-0">
              <div class="det-hd-name">
                {{ call ? text(call.conversationLabel) : t('chat.audit.call_detail_title') }}
              </div>
              <div v-if="call" class="det-hd-sub">
                {{ callMediaLabel(t, call.media) }} · {{ callOutcomeLabel(t, call) }}
              </div>
            </div>
          </div>
        </XhDialogTitle>
      </XhDialogHeader>
      <XhDialogCloseTrigger />
      <XhDialogBody class="xh-overlay-body">
        <div v-if="call" class="flex flex-col gap-4">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.call_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ call.id }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.conversation_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ call.conversationId }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.conversation_type') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ conversationTypeLabel(t, call.conversationType) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.initiator') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ text(call.initiatorUserName) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.call_outcome') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <VNodeRender :content="renderTag(callOutcomeLabel(t, call), callOutcomeTone(call))" />
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.duration') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ text(call.durationText) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.started_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(call.startedAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.answered_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(call.answeredAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('chat.audit.ended_time') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(call.endedAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <section class="flex flex-col gap-2">
            <h3 class="section-title">
              {{ t('chat.audit.section_participants', { n: call.participants.length }) }}
            </h3>
            <XDataTable :columns="columns" :data="call.participants" row-key="userId" />
          </section>
        </div>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton v-if="call" variant="subtle" size="sm" @click="emit('members', call.conversationId)">
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

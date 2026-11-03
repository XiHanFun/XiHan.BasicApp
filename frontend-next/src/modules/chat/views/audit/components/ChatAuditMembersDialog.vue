<!--
  聊天审计的会话成员（方案 H4，4.x 没有）：会话摘要 + 现有成员表（按角色再按入会时间）
  持有审计权限即可看，不要求是成员；表格与列表页同样式（XDataTable 缺省尺寸 + 列分隔线）
-->
<script setup lang="ts">
import type { ChatAuditMember, ChatAuditMembers } from '../use-chat-audit'
import type { XDataTableColumn } from '~/components'
import { XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhSpinner } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon, renderTag, XDataTable } from '~/components'
import { formatDate } from '~/utils'
import { conversationTypeLabel, memberRoleLabel, memberRoleTone } from '../use-chat-audit'

defineOptions({ name: 'ChatAuditMembersDialog' })

const props = defineProps<{
  loading: boolean
  members: ChatAuditMembers | null
}>()

const show = defineModel<boolean>('show', { default: false })

const { t } = useI18n()

function text(value: null | number | string | undefined): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

/** 群主的展示名：成员表里有群主就用他的名字，否则给主键 */
const ownerName = computed(() => {
  const conversation = props.members?.conversation
  if (!conversation?.ownerUserId)
    return '-'
  const owner = props.members?.items.find(member => member.userId === conversation.ownerUserId)
  return owner ? (owner.displayName || owner.userName || owner.userId) : conversation.ownerUserId
})

const columns = computed<XDataTableColumn<ChatAuditMember>[]>(() => [
  { key: 'displayName', title: t('chat.audit.display_name'), minWidth: 140, ellipsis: true, render: row => text(row.displayName) },
  { key: 'userName', title: t('chat.audit.user_name'), minWidth: 120, ellipsis: true, render: row => text(row.userName) },
  { key: 'memberRole', title: t('chat.audit.member_role'), width: 100, render: row => renderTag(memberRoleLabel(t, row.memberRole), memberRoleTone(row.memberRole)) },
  { key: 'isSilenced', title: t('chat.members.silenced'), width: 90, render: row => (row.isSilenced ? renderTag(t('common.statuses.yes'), 'danger') : renderTag(t('common.statuses.no'), 'neutral')) },
  { key: 'joinedAt', title: t('chat.audit.joined_at'), minWidth: 170, render: row => formatDate(row.joinedAt) },
  { key: 'userId', title: t('chat.audit.user_id'), minWidth: 280, ellipsis: true },
])
</script>

<template>
  <XhDialogRoot v-model:open="show">
    <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: calc(var(--xh-space-8) * 25)">
      <XhDialogHeader>
        <XhDialogTitle>
          <div class="det-hd-entity">
            <div class="det-hd-ico">
              <Icon icon="lucide:users" width="22" height="22" />
            </div>
            <div class="min-w-0">
              <div class="det-hd-name">
                {{ members ? text(members.conversation.conversationName) : t('chat.audit.members_title') }}
              </div>
              <div v-if="members" class="det-hd-sub">
                {{ conversationTypeLabel(t, members.conversation.conversationType) }} · {{ t('chat.members.count', { n: members.conversation.memberCount }) }}
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
        <div v-else-if="members" class="flex flex-col gap-4">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.conversation_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ members.conversation.id }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.owner') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ ownerName }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(members.conversation.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('chat.audit.last_message_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(members.conversation.lastMessageAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('chat.members.announcement_title') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <span class="chat-audit-announcement">{{ text(members.conversation.announcement) }}</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <section class="flex flex-col gap-2">
            <h3 class="section-title">
              {{ t('chat.members.section_members', { n: members.items.length }) }}
            </h3>
            <XDataTable :columns="columns" :data="members.items" row-key="userId" />
          </section>
        </div>
      </XhDialogBody>
      <XhDialogFooter>
        <XhButton variant="subtle" size="sm" @click="show = false">
          {{ t('common.actions.close') }}
        </XhButton>
      </XhDialogFooter>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.chat-audit-announcement {
  white-space: pre-wrap;
  word-break: break-word;
}
</style>

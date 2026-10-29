<script lang="ts" setup>
import type { EmailRecordRow, MessageTab, SmsRecordRow } from './messages'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import {
  XhDescriptionsItem,
  XhDescriptionsLabel,
  XhDescriptionsRoot,
  XhDescriptionsValue,
  XhDrawerBody,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerHeader,
  XhDrawerRoot,
  XhDrawerTitle,
  XhTabsContent,
  XhTabsIndicator,
  XhTabsList,
  XhTabsRoot,
  XhTabsTrigger,
} from '@xihan-ui/vue'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { actionConfirmText, renderTag, SchemaPage } from '~/components'
import { toast } from '~/composables'
import { emails as emailsApi, sms as smsApi, MessageSendStatus as STATUSES } from '~/contracts'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import { errorText, sendStatusLabel, sendStatusTone, smsProviderLabel } from '../../labels'
import { canCancel, cancelRecord, canDelete, canResend, emailRecordResource, formatAttempts, prettyJson, resendRecord, smsRecordResource } from './messages'

defineOptions({ name: 'NotificationMessagesView' })

const { t } = useI18n()
const auth = useAuthStore()
const emailResource = emailRecordResource(t)
const smsResource = smsRecordResource(t)

const activeTab = ref<MessageTab>('email')
const emailPageRef = ref<SchemaPageInstance | null>(null)
const smsPageRef = ref<SchemaPageInstance | null>(null)
/** 页面本身由邮件记录的读权限放行；短信记录另有读权限，没有就只显示邮件 */
const canReadSms = computed(() => auth.can('notification.sms.read'))

function reload(tab: MessageTab) {
  void (tab === 'email' ? emailPageRef : smsPageRef).value?.reload()
}

const asEmail = (row: object) => row as EmailRecordRow
const asSms = (row: object) => row as SmsRecordRow

const statusOptions = computed<SelectOption[]>(() => STATUSES.map(status => ({ label: sendStatusLabel(t, status), value: status })))

function yesNo(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

function statusTag(row: { status: EmailRecordRow['status'] }) {
  return renderTag(sendStatusLabel(t, row.status), sendStatusTone(row.status))
}

// ── 邮件（字段单一事实源；可排序的只放后端白名单里的：发送时间、创建时间） ──
const emailFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.messages.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.messages.email_search_keyword_placeholder'), order: 0 },
  { key: 'toEmail', title: t('notification.messages.col_to_email'), dataType: 'string', minWidth: 200, order: 1 },
  { key: 'subject', title: t('notification.messages.col_subject'), dataType: 'string', minWidth: 220, order: 2 },
  { key: 'status', title: t('notification.messages.col_status'), dataType: 'enum', searchable: true, searchMultiple: true, options: statusOptions.value, searchPlaceholder: t('notification.messages.search_status_placeholder'), width: 100, order: 3, render: row => statusTag(asEmail(row)) },
  { key: 'isHtml', title: t('notification.messages.col_is_html'), dataType: 'boolean', width: 82, order: 4, render: row => renderTag(yesNo(asEmail(row).isHtml), asEmail(row).isHtml ? 'info' : 'neutral') },
  { key: 'businessType', title: t('notification.messages.col_business_type'), dataType: 'string', searchable: true, searchPlaceholder: t('notification.messages.search_business_type_placeholder'), minWidth: 130, order: 5 },
  { key: 'senderId', title: t('notification.messages.col_sender'), dataType: 'string', visible: false, minWidth: 280, order: 6 },
  { key: 'receiverId', title: t('notification.messages.col_receiver'), dataType: 'string', visible: false, minWidth: 280, order: 7 },
  { key: 'templateCode', title: t('notification.messages.col_template_code'), dataType: 'string', searchable: true, searchPlaceholder: t('notification.messages.search_template_code_placeholder'), minWidth: 150, order: 8 },
  { key: 'attempts', title: t('notification.messages.col_attempts'), dataType: 'string', width: 90, order: 9, render: row => formatAttempts(asEmail(row)) },
  { key: 'sentAt', title: t('notification.messages.col_sent_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
  { key: 'createdAt', title: t('notification.messages.col_created_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, minWidth: 170, order: 11 },
])

const emailSchema = computed<PageSchema>(() => ({
  pageCode: 'notification.messages.email',
  pageName: t('notification.messages.email_page_name'),
  exportPermission: 'notification.emails.read',
  rowKey: 'id',
  fields: emailFields.value,
  resource: emailResource as unknown as PageSchema['resource'],
  actions: [
    { key: 'detail', title: t('notification.messages.action_detail'), scope: 'row', tone: 'brand', icon: 'lucide:eye' },
    { key: 'resend', title: t('notification.messages.action_resend'), scope: 'row', tone: 'warning', icon: 'lucide:refresh-cw', confirm: true, confirmText: row => actionConfirmText(t, t('notification.messages.action_resend'), asEmail(row).subject), visible: row => canResend(asEmail(row).status), permission: 'notification.emails.resend' },
    { key: 'cancel', title: t('notification.messages.action_cancel'), scope: 'row', icon: 'lucide:circle-slash', confirm: true, confirmText: row => actionConfirmText(t, t('notification.messages.action_cancel'), asEmail(row).subject), visible: row => canCancel(asEmail(row).status), permission: 'notification.emails.resend' },
    { key: 'delete', title: t('notification.messages.action_delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: t('notification.messages.delete_email_content'), visible: row => canDelete(asEmail(row).status), permission: 'notification.emails.delete' },
  ],
}))

// ── 短信 ─────────────────────────────────────────────────────────
const smsFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('notification.messages.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('notification.messages.sms_search_keyword_placeholder'), order: 0 },
  { key: 'toPhone', title: t('notification.messages.col_to_phone'), dataType: 'string', minWidth: 140, order: 1 },
  { key: 'provider', title: t('notification.messages.col_provider'), dataType: 'enum', width: 110, order: 2, render: row => (asSms(row).provider ? renderTag(smsProviderLabel(t, asSms(row).provider!), 'info') : '-') },
  { key: 'content', title: t('notification.messages.col_content'), dataType: 'string', ellipsis: true, minWidth: 220, order: 3 },
  { key: 'status', title: t('notification.messages.col_status'), dataType: 'enum', searchable: true, searchMultiple: true, options: statusOptions.value, searchPlaceholder: t('notification.messages.search_status_placeholder'), width: 100, order: 4, render: row => statusTag(asSms(row)) },
  { key: 'businessType', title: t('notification.messages.col_business_type'), dataType: 'string', searchable: true, searchPlaceholder: t('notification.messages.search_business_type_placeholder'), minWidth: 130, order: 5 },
  { key: 'senderId', title: t('notification.messages.col_sender'), dataType: 'string', visible: false, minWidth: 280, order: 6 },
  { key: 'receiverId', title: t('notification.messages.col_receiver'), dataType: 'string', visible: false, minWidth: 280, order: 7 },
  { key: 'templateCode', title: t('notification.messages.col_template_code'), dataType: 'string', searchable: true, searchPlaceholder: t('notification.messages.search_template_code_placeholder'), minWidth: 150, order: 8 },
  { key: 'attempts', title: t('notification.messages.col_attempts'), dataType: 'string', width: 90, order: 9, render: row => formatAttempts(asSms(row)) },
  { key: 'sentAt', title: t('notification.messages.col_sent_at'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
  { key: 'createdAt', title: t('notification.messages.col_created_at'), dataType: 'datetime', sortable: true, searchable: true, searchRange: true, minWidth: 170, order: 11 },
])

const smsSchema = computed<PageSchema>(() => ({
  pageCode: 'notification.messages.sms',
  pageName: t('notification.messages.sms_page_name'),
  exportPermission: 'notification.sms.read',
  rowKey: 'id',
  fields: smsFields.value,
  resource: smsResource as unknown as PageSchema['resource'],
  actions: [
    { key: 'detail', title: t('notification.messages.action_detail'), scope: 'row', tone: 'brand', icon: 'lucide:eye' },
    { key: 'resend', title: t('notification.messages.action_resend'), scope: 'row', tone: 'warning', icon: 'lucide:refresh-cw', confirm: true, confirmText: row => actionConfirmText(t, t('notification.messages.action_resend'), asSms(row).toPhone), visible: row => canResend(asSms(row).status), permission: 'notification.sms.resend' },
    { key: 'cancel', title: t('notification.messages.action_cancel'), scope: 'row', icon: 'lucide:circle-slash', confirm: true, confirmText: row => actionConfirmText(t, t('notification.messages.action_cancel'), asSms(row).toPhone), visible: row => canCancel(asSms(row).status), permission: 'notification.sms.resend' },
    { key: 'delete', title: t('notification.messages.action_delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: t('notification.messages.delete_sms_content'), visible: row => canDelete(asSms(row).status), permission: 'notification.sms.delete' },
  ],
}))

function onEmailAction(payload: SchemaActionPayload) {
  if (payload.row)
    void handleAction('email', payload.key, asEmail(payload.row).id)
}

function onSmsAction(payload: SchemaActionPayload) {
  if (payload.row)
    void handleAction('sms', payload.key, asSms(payload.row).id)
}

async function handleAction(tab: MessageTab, key: string, id: string) {
  switch (key) {
    case 'detail':
      await openDetail(tab, id)
      break
    case 'resend':
      await runAction(tab, () => resendRecord(t, tab, id), tab === 'email' ? t('notification.messages.msg_email_requeued') : t('notification.messages.msg_sms_requeued'), t('notification.messages.msg_resend_failed'))
      break
    case 'cancel':
      await runAction(tab, () => cancelRecord(t, tab, id), t('notification.messages.msg_cancelled'), t('notification.messages.msg_cancel_failed'))
      break
    case 'delete':
      await runAction(tab, async () => (tab === 'email' ? emailResource : smsResource).remove?.(id), tab === 'email' ? t('notification.messages.msg_email_deleted') : t('notification.messages.msg_sms_deleted'), t('notification.messages.msg_delete_failed'))
      break
  }
}

async function runAction(tab: MessageTab, action: () => Promise<unknown>, success: string, failure: string) {
  try {
    await action()
    toast.success(success)
  }
  catch (error) {
    toast.danger(errorText(error) || failure)
  }
  finally {
    // 逐项拒绝时状态多半已变（别人刚发出、刚取消），失败也刷新一次
    reload(tab)
  }
}

// ── 详情（邮件 / 短信共用一个抽屉，按 detailTab 切换内容） ─────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailTab = ref<MessageTab>('email')
const currentEmail = ref<EmailRecordRow | null>(null)
const currentSms = ref<SmsRecordRow | null>(null)

async function openDetail(tab: MessageTab, id: string) {
  detailTab.value = tab
  currentEmail.value = null
  currentSms.value = null
  detailVisible.value = true
  detailLoading.value = true
  try {
    if (tab === 'email')
      currentEmail.value = await emailsApi.get({ id })
    else
      currentSms.value = await smsApi.get({ id })
  }
  catch (error) {
    toast.danger(errorText(error) || (tab === 'email' ? t('notification.messages.msg_load_email_detail_failed') : t('notification.messages.msg_load_sms_detail_failed')))
  }
  finally {
    detailLoading.value = false
  }
}

function businessRef(type: null | string | undefined, id: null | string | undefined): string {
  return `${type || '-'} / ${id || '-'}`
}

function userRef(sender: null | string | undefined, receiver: null | string | undefined): string {
  return `${sender || '-'} -> ${receiver || '-'}`
}
</script>

<template>
  <div class="message-page">
    <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-if="canReadSms" v-model:value="activeTab" variant="line">
      <XhTabsList>
        <XhTabsTrigger value="email">
          {{ t('notification.messages.tab_email') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="sms">
          {{ t('notification.messages.tab_sms') }}
        </XhTabsTrigger>
        <XhTabsIndicator />
      </XhTabsList>
      <XhTabsContent value="email">
        <SchemaPage ref="emailPageRef" :schema="emailSchema" @action="onEmailAction" />
      </XhTabsContent>
      <XhTabsContent value="sms">
        <SchemaPage ref="smsPageRef" :schema="smsSchema" @action="onSmsAction" />
      </XhTabsContent>
    </XhTabsRoot>
    <SchemaPage v-else ref="emailPageRef" :schema="emailSchema" @action="onEmailAction" />

    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 38.75rem">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ detailTab === 'email' ? t('notification.messages.detail_email_title') : t('notification.messages.detail_sms_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body">
          <p v-if="detailLoading" class="message-detail__hint">
            {{ t('common.loading') }}
          </p>

          <XhDescriptionsRoot v-else-if="detailTab === 'email' && currentEmail" :columns="1" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_to_email') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentEmail.toEmail }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_subject') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentEmail.subject }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ sendStatusLabel(t, currentEmail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_is_html') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ yesNo(currentEmail.isHtml) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_business_ref') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ businessRef(currentEmail.businessType, currentEmail.businessId) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_user_ref') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ userRef(currentEmail.senderId, currentEmail.receiverId) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_template_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentEmail.templateCode || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_attempts') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatAttempts(currentEmail) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_next_attempt_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentEmail.nextAttemptAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_sent_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentEmail.sentAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentEmail.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_error_message') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentEmail.errorMessage || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_content') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <!-- 正文按源码显示（HTML 正文不在管理端渲染） -->
                <pre v-if="currentEmail.content" class="message-detail__code">{{ currentEmail.content }}</pre>
                <span v-else>-</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <XhDescriptionsRoot v-else-if="detailTab === 'sms' && currentSms" :columns="1" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_to_phone') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentSms.toPhone }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_provider') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentSms.provider ? smsProviderLabel(t, currentSms.provider) : '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_provider_message_id') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentSms.providerMessageId || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ sendStatusLabel(t, currentSms.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_business_ref') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ businessRef(currentSms.businessType, currentSms.businessId) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_user_ref') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ userRef(currentSms.senderId, currentSms.receiverId) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_template_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentSms.templateCode || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_template_params') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <pre v-if="currentSms.templateParams" class="message-detail__code">{{ prettyJson(currentSms.templateParams) }}</pre>
                <span v-else>-</span>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_attempts') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatAttempts(currentSms) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_next_attempt_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentSms.nextAttemptAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_sent_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentSms.sentAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(currentSms.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_error_message') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ currentSms.errorMessage || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('notification.messages.detail_content') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>
                <pre class="message-detail__code">{{ currentSms.content }}</pre>
              </XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>

          <p v-else class="message-detail__hint">
            {{ t('notification.messages.detail_empty') }}
          </p>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>
  </div>
</template>

<style scoped>
.message-page {
  height: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 的表格卡片，分页贴底）。
   包在标签页里时高度链在标签页根上断开，这里补全传递链（4.x 同）。 */
.message-page :deep([data-scope='tabs'][data-part='root']) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.message-page :deep([data-scope='tabs'][data-part='list']) {
  padding: var(--xh-space-2) var(--xh-space-3) 0;
}

.message-page :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  height: 0;
  padding: 0;
}

.message-detail__hint {
  padding: var(--xh-space-6) 0;
  text-align: center;
  color: var(--xh-fg-subtle);
}

/* 正文与模板参数按源码显示：保留换行、限高滚动 */
.message-detail__code {
  max-height: 20rem;
  margin: 0;
  overflow: auto;
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-text-secondary-size);
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>

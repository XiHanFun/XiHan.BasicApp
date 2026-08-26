<script setup lang="ts">
import type {
  ApiId,
  EmailDetailDto,
  EmailListItemDto,
  PageResult,
  SmsDetailDto,
  SmsListItemDto,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFlex, XhTabsContent, XhTabsList, XhTabsRoot, XhTabsTrigger, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, EmailStatus, messageCenterApi, querySortsFromSchema, SmsStatus } from '@/api'
import { EMAIL_STATUS_OPTIONS, EMAIL_TYPE_OPTIONS, SMS_STATUS_OPTIONS, SMS_TYPE_OPTIONS } from '@/constants'
import { SchemaPage } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemMessagePage' })

// 业务枚举本地化选项（挂载拉取后端元数据 + 切语言重取 + 空回退静态常量）
const emailTypeOptions = useEnumOptions('EmailType', EMAIL_TYPE_OPTIONS)
const emailStatusOptions = useEnumOptions('EmailStatus', EMAIL_STATUS_OPTIONS)
const smsTypeOptions = useEnumOptions('SmsType', SMS_TYPE_OPTIONS)
const smsStatusOptions = useEnumOptions('SmsStatus', SMS_STATUS_OPTIONS)

type MessageTab = 'email' | 'sms'
type TagType = 'neutral' | 'danger' | 'info' | 'success' | 'warning'

const { t } = useI18n()

const activeTab = ref<MessageTab>('email')

const emailPageRef = ref<{ reload: () => Promise<void> } | null>(null)
const smsPageRef = ref<{ reload: () => Promise<void> } | null>(null)

// 详情抽屉（邮件/短信共用一个，按 detailTab 切换内容）
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailTab = ref<MessageTab>('email')
const currentEmailDetail = ref<EmailDetailDto | null>(null)
const currentSmsDetail = ref<SmsDetailDto | null>(null)

// 过滤值归一化：空串/空值 → null，与后端「未传即不过滤」语义一致
function normalizeNullable(value: unknown): string | null {
  const normalized = (value as string | undefined)?.trim()
  return normalized || null
}
function normalizeId(value: unknown): ApiId | null {
  return (value as ApiId | null | undefined) || null
}
function normalizeTemplateCode(value: unknown): string | undefined {
  return (value as string | undefined)?.trim() || undefined
}

function getMessageStatusTagType(status: EmailStatus | SmsStatus): TagType {
  if (status === EmailStatus.Success) {
    return 'success'
  }
  if (status === EmailStatus.Failed) {
    return 'danger'
  }
  if (status === EmailStatus.Sending) {
    return 'warning'
  }
  if (status === EmailStatus.Cancelled) {
    return 'neutral'
  }
  return 'info'
}

function formatRetry(row: { maxRetryCount: number, retryCount: number }) {
  return `${row.retryCount}/${row.maxRetryCount}`
}

function formatFlag(value: boolean) {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}

function canResend(status: EmailStatus | SmsStatus) {
  return status === EmailStatus.Failed || status === EmailStatus.Pending || status === EmailStatus.Cancelled
}

// ── 系统邮件 ──────────────────────────────────────────────────
const emailFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.record.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.record.email_search_keyword_placeholder'), order: 0 },
  { key: 'subject', title: t('message.record.col_subject'), dataType: 'string', sortable: true, minWidth: 220, order: 1 },
  {
    key: 'emailType',
    title: t('message.record.col_email_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EmailType',
    options: EMAIL_TYPE_OPTIONS,
    searchPlaceholder: t('message.record.search_email_type_placeholder'),
    minWidth: 110,
    order: 2,
    render: row => getOptionLabel(emailTypeOptions.value, (row as unknown as EmailListItemDto).emailType),
  },
  {
    key: 'emailStatus',
    title: t('message.record.col_email_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EmailStatus',
    options: EMAIL_STATUS_OPTIONS,
    searchPlaceholder: t('message.record.search_status_placeholder'),
    width: 100,
    order: 3,
    render: (row) => {
      const r = row as unknown as EmailListItemDto
      return h(XhTagRoot, { variant: 'outline', tone: getMessageStatusTagType(r.emailStatus) }, () => h(XhTagLabel, () => getOptionLabel(emailStatusOptions.value, r.emailStatus)))
    },
  },
  {
    key: 'isHtml',
    title: t('message.record.col_is_html'),
    dataType: 'boolean',
    width: 82,
    order: 4,
    render: (row) => {
      const r = row as unknown as EmailListItemDto
      return h(XhTagRoot, { variant: 'outline', tone: r.isHtml ? 'info' : 'neutral' }, () => h(XhTagLabel, () => formatFlag(r.isHtml)))
    },
  },
  { key: 'businessType', title: t('message.record.col_business_type'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_business_type_placeholder'), minWidth: 130, order: 5 },
  { key: 'sendUserId', title: t('message.record.col_send_user'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_send_user_placeholder'), minWidth: 110, order: 6 },
  { key: 'receiveUserId', title: t('message.record.col_receive_user'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_receive_user_placeholder'), minWidth: 110, order: 7 },
  { key: 'templateCode', title: t('message.record.col_template_code'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_template_code_placeholder'), minWidth: 130, order: 8 },
  { key: 'retryCount', title: t('message.record.col_retry'), dataType: 'string', sortable: true, minWidth: 90, order: 9, render: row => formatRetry(row as unknown as EmailListItemDto) },
  { key: 'sendTime', title: t('message.record.col_send_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
  { key: 'createdTime', title: t('message.record.col_created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
])

const emailSchema = computed<PageSchema>(() => ({
  pageCode: 'message.email',
  exportPermission: 'saas:message:export',
  pageName: t('message.record.email_page_name'),
  rowKey: 'basicId',
  fields: emailFields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return messageCenterApi.emailPage({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 区间/多选(emailType/emailStatus)统一走 conditions.filters（Between/In）
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        businessId: null,
        businessType: normalizeNullable(f.businessType),
        keyword: normalizeNullable(f.keyword),
        receiveUserId: normalizeId(f.receiveUserId),
        sendUserId: normalizeId(f.sendUserId),
        templateCode: normalizeTemplateCode(f.templateCode),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => messageCenterApi.deleteEmail(id),
  },
  actions: [
    { key: 'detail', title: t('message.record.action_detail'), scope: 'row', type: 'primary', icon: 'lucide:eye' },
    { key: 'resend', title: t('message.record.action_resend'), scope: 'row', type: 'warning', icon: 'lucide:refresh-cw', visible: row => canResend((row as unknown as EmailListItemDto).emailStatus) },
    { key: 'delete', title: t('message.record.action_delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onEmailAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as EmailListItemDto | undefined
  if (!row) {
    return
  }
  if (payload.key === 'detail') {
    void openEmailDetail(row)
  }
  else if (payload.key === 'resend') {
    void resendEmail(row)
  }
  else if (payload.key === 'delete') {
    confirmDeleteEmail(row)
  }
}

async function openEmailDetail(row: EmailListItemDto) {
  detailTab.value = 'email'
  detailVisible.value = true
  detailLoading.value = true
  currentSmsDetail.value = null
  try {
    currentEmailDetail.value = await messageCenterApi.emailDetail(row.basicId)
  }
  catch (error) {
    currentEmailDetail.value = null
    toast.error((error as Error)?.message || t('message.record.msg_load_email_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

async function resendEmail(row: EmailListItemDto) {
  try {
    await messageCenterApi.updateEmailStatus({ basicId: row.basicId, emailStatus: EmailStatus.Pending })
    toast.success(t('message.record.msg_email_requeued'))
    void emailPageRef.value?.reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('message.record.msg_email_resend_failed'))
  }
}

function confirmDeleteEmail(row: EmailListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('message.record.delete_email_title'),
    content: t('message.record.delete_email_content'),
    okText: t('message.record.confirm'),
    cancelText: t('message.record.cancel'),
    onOk: () => deleteEmail(row),
  })
}

async function deleteEmail(row: EmailListItemDto) {
  try {
    await messageCenterApi.deleteEmail(row.basicId)
    toast.success(t('message.record.msg_email_deleted'))
    void emailPageRef.value?.reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('message.record.msg_email_delete_failed'))
  }
}

// ── 系统短信 ──────────────────────────────────────────────────
const smsFields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('message.record.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('message.record.sms_search_keyword_placeholder'), order: 0 },
  { key: 'provider', title: t('message.record.col_provider'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_provider_placeholder'), minWidth: 140, order: 1 },
  {
    key: 'smsType',
    title: t('message.record.col_sms_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'SmsType',
    options: SMS_TYPE_OPTIONS,
    searchPlaceholder: t('message.record.search_sms_type_placeholder'),
    minWidth: 110,
    order: 2,
    render: row => getOptionLabel(smsTypeOptions.value, (row as unknown as SmsListItemDto).smsType),
  },
  {
    key: 'smsStatus',
    title: t('message.record.col_email_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'SmsStatus',
    options: SMS_STATUS_OPTIONS,
    searchPlaceholder: t('message.record.search_status_placeholder'),
    width: 100,
    order: 3,
    render: (row) => {
      const r = row as unknown as SmsListItemDto
      return h(XhTagRoot, { variant: 'outline', tone: getMessageStatusTagType(r.smsStatus) }, () => h(XhTagLabel, () => getOptionLabel(smsStatusOptions.value, r.smsStatus)))
    },
  },
  { key: 'businessType', title: t('message.record.col_business_type'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_business_type_placeholder'), minWidth: 130, order: 4 },
  { key: 'senderId', title: t('message.record.col_send_user'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_send_user_placeholder'), minWidth: 110, order: 5 },
  { key: 'receiverId', title: t('message.record.col_receive_user'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_receive_user_placeholder'), minWidth: 110, order: 6 },
  { key: 'templateCode', title: t('message.record.col_template_code'), dataType: 'string', searchable: true, sortable: true, searchPlaceholder: t('message.record.search_template_code_placeholder'), minWidth: 130, order: 7 },
  { key: 'cost', title: t('message.record.col_cost'), dataType: 'string', sortable: true, minWidth: 90, order: 8 },
  { key: 'retryCount', title: t('message.record.col_retry'), dataType: 'string', sortable: true, minWidth: 90, order: 9, render: row => formatRetry(row as unknown as SmsListItemDto) },
  { key: 'sendTime', title: t('message.record.col_send_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
  { key: 'createdTime', title: t('message.record.col_created_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
])

const smsSchema = computed<PageSchema>(() => ({
  pageCode: 'message.sms',
  exportPermission: 'saas:message:export',
  pageName: t('message.record.sms_page_name'),
  rowKey: 'basicId',
  fields: smsFields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return messageCenterApi.smsPage({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 区间/多选(smsType/smsStatus)统一走 conditions.filters（Between/In）
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        businessId: null,
        businessType: normalizeNullable(f.businessType),
        keyword: normalizeNullable(f.keyword),
        provider: normalizeNullable(f.provider),
        receiverId: normalizeId(f.receiverId),
        senderId: normalizeId(f.senderId),
        templateCode: normalizeTemplateCode(f.templateCode),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => messageCenterApi.deleteSms(id),
  },
  actions: [
    { key: 'detail', title: t('message.record.action_detail'), scope: 'row', type: 'primary', icon: 'lucide:eye' },
    { key: 'resend', title: t('message.record.action_resend'), scope: 'row', type: 'warning', icon: 'lucide:refresh-cw', visible: row => canResend((row as unknown as SmsListItemDto).smsStatus) },
    { key: 'delete', title: t('message.record.action_delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}))

function onSmsAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as SmsListItemDto | undefined
  if (!row) {
    return
  }
  if (payload.key === 'detail') {
    void openSmsDetail(row)
  }
  else if (payload.key === 'resend') {
    void resendSms(row)
  }
  else if (payload.key === 'delete') {
    confirmDeleteSms(row)
  }
}

async function openSmsDetail(row: SmsListItemDto) {
  detailTab.value = 'sms'
  detailVisible.value = true
  detailLoading.value = true
  currentEmailDetail.value = null
  try {
    currentSmsDetail.value = await messageCenterApi.smsDetail(row.basicId)
  }
  catch (error) {
    currentSmsDetail.value = null
    toast.error((error as Error)?.message || t('message.record.msg_load_sms_detail_failed'))
  }
  finally {
    detailLoading.value = false
  }
}

async function resendSms(row: SmsListItemDto) {
  try {
    await messageCenterApi.updateSmsStatus({ basicId: row.basicId, smsStatus: SmsStatus.Pending })
    toast.success(t('message.record.msg_sms_requeued'))
    void smsPageRef.value?.reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('message.record.msg_sms_resend_failed'))
  }
}

function confirmDeleteSms(row: SmsListItemDto) {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('message.record.delete_sms_title'),
    content: t('message.record.delete_sms_content'),
    okText: t('message.record.confirm'),
    cancelText: t('message.record.cancel'),
    onOk: () => deleteSms(row),
  })
}

async function deleteSms(row: SmsListItemDto) {
  try {
    await messageCenterApi.deleteSms(row.basicId)
    toast.success(t('message.record.msg_sms_deleted'))
    void smsPageRef.value?.reload()
  }
  catch (error) {
    toast.error((error as Error)?.message || t('message.record.msg_sms_delete_failed'))
  }
}
</script>

<template>
  <div class="message-page">
    <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-model:value="activeTab" variant="line">
      <XhTabsList>
        <XhTabsTrigger value="email">
          {{ t('message.record.tab_email') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="sms">
          {{ t('message.record.tab_sms') }}
        </XhTabsTrigger>
      </XhTabsList>
      <XhTabsContent value="email">
        <SchemaPage ref="emailPageRef" :schema="emailSchema" @action="onEmailAction" />
      </XhTabsContent>
      <XhTabsContent value="sms">
        <SchemaPage ref="smsPageRef" :schema="smsSchema" @action="onSmsAction" />
      </XhTabsContent>
    </XhTabsRoot>

    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 620px">
        <XhDrawerTitle>{{ detailTab === 'email' ? t('message.record.detail_email_title') : t('message.record.detail_sms_title') }}</XhDrawerTitle>
        <XhDrawerCloseTrigger />
        <XhFlex v-if="detailLoading" justify="center">
          {{ t('message.record.detail_loading') }}
        </XhFlex>

        <XhDescriptionsRoot v-else-if="detailTab === 'email' && currentEmailDetail" :columns="1" bordered size="sm">
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_subject') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentEmailDetail.subject }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_email_type') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ getOptionLabel(emailTypeOptions, currentEmailDetail.emailType) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_send_status') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ getOptionLabel(emailStatusOptions, currentEmailDetail.emailStatus) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_business_ref') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentEmailDetail.businessType || '-' }} / {{ currentEmailDetail.businessId || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_user_ref') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentEmailDetail.sendUserId || '-' }} -> {{ currentEmailDetail.receiveUserId || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_template_code') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentEmailDetail.templateCode || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_retry') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatRetry(currentEmailDetail) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_scheduled_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentEmailDetail.scheduledTime ? formatDate(currentEmailDetail.scheduledTime) : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_actual_send_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentEmailDetail.sendTime ? formatDate(currentEmailDetail.sendTime) : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_created_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatDate(currentEmailDetail.createdTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_modified_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentEmailDetail.modifiedTime ? formatDate(currentEmailDetail.modifiedTime) : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
        </XhDescriptionsRoot>

        <XhDescriptionsRoot v-else-if="detailTab === 'sms' && currentSmsDetail" :columns="1" bordered size="sm">
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_provider') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.provider || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_sms_type') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ getOptionLabel(smsTypeOptions, currentSmsDetail.smsType) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_send_status') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ getOptionLabel(smsStatusOptions, currentSmsDetail.smsStatus) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_business_ref') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.businessType || '-' }} / {{ currentSmsDetail.businessId || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_user_ref') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.senderId || '-' }} -> {{ currentSmsDetail.receiverId || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_template_code') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.templateCode || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_cost') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.cost ?? '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_retry') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatRetry(currentSmsDetail) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_scheduled_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.scheduledTime ? formatDate(currentSmsDetail.scheduledTime) : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_actual_send_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.sendTime ? formatDate(currentSmsDetail.sendTime) : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_created_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ formatDate(currentSmsDetail.createdTime) }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('message.record.detail_modified_time') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ currentSmsDetail.modifiedTime ? formatDate(currentSmsDetail.modifiedTime) : '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
        </XhDescriptionsRoot>

        <div v-else class="py-8 text-center text-gray-400">
          {{ t('message.record.detail_empty') }}
        </div>
      </XhDrawerContent>
    </XhDrawerRoot>
  </div>
</template>

<style scoped>
.message-page {
  height: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 的表格卡片，分页贴底）。
   被 NTabs 包裹时高度链在 tab pane 处断裂，这里补全传递链，
   与授权申请等以 NTabs 包裹 SchemaPage 的页面保持一致。 */
.message-page :deep([data-scope='tabs'][data-part='root']) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.message-page :deep([data-scope='tabs'][data-part='list']) {
  padding: 8px 12px 0;
}

.message-page :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  height: 0;
}

.message-page :deep([data-scope='tabs'][data-part='content']) {
  padding: 0;
}
</style>

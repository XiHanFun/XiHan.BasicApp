<script setup lang="ts">
import type {
  ApiId,
  EmailDetailDto,
  EmailListItemDto,
  EmailType,
  PageResult,
  SmsDetailDto,
  SmsListItemDto,
  SmsType,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NSpace,
  NTabPane,
  NTabs,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { h, ref } from 'vue'
import { createPageRequest, EmailStatus, messageCenterApi, SmsStatus } from '@/api'
import { SchemaPage } from '~/components'
import { EMAIL_STATUS_OPTIONS, EMAIL_TYPE_OPTIONS, SMS_STATUS_OPTIONS, SMS_TYPE_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemMessagePage' })

type MessageTab = 'email' | 'sms'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()
const dialog = useDialog()

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
    return 'error'
  }
  if (status === EmailStatus.Sending) {
    return 'warning'
  }
  if (status === EmailStatus.Cancelled) {
    return 'default'
  }
  return 'info'
}

function formatRetry(row: { maxRetryCount: number, retryCount: number }) {
  return `${row.retryCount}/${row.maxRetryCount}`
}

function formatFlag(value: boolean) {
  return value ? '是' : '否'
}

function canResend(status: EmailStatus | SmsStatus) {
  return status === EmailStatus.Failed || status === EmailStatus.Pending || status === EmailStatus.Cancelled
}

// ── 系统邮件 ──────────────────────────────────────────────────
const emailFields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索主题/业务类型', order: 0 },
  { key: 'subject', title: '邮件主题', dataType: 'string', minWidth: 220, order: 1 },
  {
    key: 'emailType',
    title: '邮件类型',
    dataType: 'enum',
    searchable: true,
    options: EMAIL_TYPE_OPTIONS,
    searchPlaceholder: '邮件类型',
    minWidth: 110,
    order: 2,
    render: row => getOptionLabel(EMAIL_TYPE_OPTIONS, (row as unknown as EmailListItemDto).emailType),
  },
  {
    key: 'emailStatus',
    title: '发送状态',
    dataType: 'enum',
    searchable: true,
    options: EMAIL_STATUS_OPTIONS,
    searchPlaceholder: '发送状态',
    width: 100,
    order: 3,
    render: (row) => {
      const r = row as unknown as EmailListItemDto
      return h(NTag, { type: getMessageStatusTagType(r.emailStatus), round: true, size: 'small' }, () => getOptionLabel(EMAIL_STATUS_OPTIONS, r.emailStatus))
    },
  },
  {
    key: 'isHtml',
    title: 'HTML',
    dataType: 'boolean',
    width: 82,
    order: 4,
    render: (row) => {
      const r = row as unknown as EmailListItemDto
      return h(NTag, { type: r.isHtml ? 'info' : 'default', round: true, size: 'small' }, () => formatFlag(r.isHtml))
    },
  },
  { key: 'businessType', title: '业务类型', dataType: 'string', searchable: true, searchPlaceholder: '业务类型', minWidth: 130, order: 5 },
  { key: 'sendUserId', title: '发送用户', dataType: 'string', searchable: true, searchPlaceholder: '发送用户', minWidth: 110, order: 6 },
  { key: 'receiveUserId', title: '接收用户', dataType: 'string', searchable: true, searchPlaceholder: '接收用户', minWidth: 110, order: 7 },
  { key: 'templateCode', title: '模板编码', dataType: 'string', searchable: true, searchPlaceholder: '模板编码', minWidth: 130, order: 8 },
  { key: 'retryCount', title: '重试', dataType: 'string', minWidth: 90, order: 9, render: row => formatRetry(row as unknown as EmailListItemDto) },
  { key: 'sendTime', title: '发送时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
]

const emailSchema: PageSchema = {
  pageCode: 'message.email',
  pageName: '系统邮件',
  rowKey: 'basicId',
  scrollX: 1600,
  fields: emailFields,
  resource: {
    page: (params) => {
      const f = params.filters
      return messageCenterApi.emailPage({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        businessId: null,
        businessType: normalizeNullable(f.businessType),
        emailStatus: (f.emailStatus ?? null) as EmailStatus | null,
        emailType: (f.emailType ?? null) as EmailType | null,
        keyword: normalizeNullable(f.keyword),
        receiveUserId: normalizeId(f.receiveUserId),
        sendUserId: normalizeId(f.sendUserId),
        templateCode: normalizeTemplateCode(f.templateCode),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => messageCenterApi.deleteEmail(id),
  },
  actions: [
    { key: 'detail', title: '详情', scope: 'row', type: 'primary', icon: 'lucide:eye' },
    { key: 'resend', title: '重发', scope: 'row', type: 'warning', icon: 'lucide:refresh-cw', visible: row => canResend((row as unknown as EmailListItemDto).emailStatus) },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}

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
  catch {
    currentEmailDetail.value = null
    message.error('加载系统邮件详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function resendEmail(row: EmailListItemDto) {
  try {
    await messageCenterApi.updateEmailStatus({ basicId: row.basicId, emailStatus: EmailStatus.Pending })
    message.success('邮件已重新加入发送队列')
    void emailPageRef.value?.reload()
  }
  catch {
    message.error('重发邮件失败')
  }
}

function confirmDeleteEmail(row: EmailListItemDto) {
  dialog.warning({
    title: '删除邮件',
    content: '确定删除该邮件？',
    positiveText: '确定',
    negativeText: '取消',
    onPositiveClick: () => deleteEmail(row),
  })
}

async function deleteEmail(row: EmailListItemDto) {
  try {
    await messageCenterApi.deleteEmail(row.basicId)
    message.success('邮件已删除')
    void emailPageRef.value?.reload()
  }
  catch {
    message.error('删除邮件失败')
  }
}

// ── 系统短信 ──────────────────────────────────────────────────
const smsFields: ListFieldSchema[] = [
  { key: 'keyword', title: '关键词', dataType: 'string', visible: false, searchable: true, searchPlaceholder: '搜索服务商/业务类型', order: 0 },
  { key: 'provider', title: '服务商', dataType: 'string', searchable: true, searchPlaceholder: '服务商', minWidth: 140, order: 1 },
  {
    key: 'smsType',
    title: '短信类型',
    dataType: 'enum',
    searchable: true,
    options: SMS_TYPE_OPTIONS,
    searchPlaceholder: '短信类型',
    minWidth: 110,
    order: 2,
    render: row => getOptionLabel(SMS_TYPE_OPTIONS, (row as unknown as SmsListItemDto).smsType),
  },
  {
    key: 'smsStatus',
    title: '发送状态',
    dataType: 'enum',
    searchable: true,
    options: SMS_STATUS_OPTIONS,
    searchPlaceholder: '发送状态',
    width: 100,
    order: 3,
    render: (row) => {
      const r = row as unknown as SmsListItemDto
      return h(NTag, { type: getMessageStatusTagType(r.smsStatus), round: true, size: 'small' }, () => getOptionLabel(SMS_STATUS_OPTIONS, r.smsStatus))
    },
  },
  { key: 'businessType', title: '业务类型', dataType: 'string', searchable: true, searchPlaceholder: '业务类型', minWidth: 130, order: 4 },
  { key: 'senderId', title: '发送用户', dataType: 'string', searchable: true, searchPlaceholder: '发送用户', minWidth: 110, order: 5 },
  { key: 'receiverId', title: '接收用户', dataType: 'string', searchable: true, searchPlaceholder: '接收用户', minWidth: 110, order: 6 },
  { key: 'templateCode', title: '模板编码', dataType: 'string', searchable: true, searchPlaceholder: '模板编码', minWidth: 130, order: 7 },
  { key: 'cost', title: '费用', dataType: 'string', minWidth: 90, order: 8 },
  { key: 'retryCount', title: '重试', dataType: 'string', minWidth: 90, order: 9, render: row => formatRetry(row as unknown as SmsListItemDto) },
  { key: 'sendTime', title: '发送时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 10 },
  { key: 'createdTime', title: '创建时间', dataType: 'datetime', sortable: true, minWidth: 170, order: 11 },
]

const smsSchema: PageSchema = {
  pageCode: 'message.sms',
  pageName: '系统短信',
  rowKey: 'basicId',
  scrollX: 1600,
  fields: smsFields,
  resource: {
    page: (params) => {
      const f = params.filters
      return messageCenterApi.smsPage({
        ...createPageRequest({ page: { pageIndex: params.page, pageSize: params.pageSize } }),
        businessId: null,
        businessType: normalizeNullable(f.businessType),
        keyword: normalizeNullable(f.keyword),
        provider: normalizeNullable(f.provider),
        receiverId: normalizeId(f.receiverId),
        senderId: normalizeId(f.senderId),
        smsStatus: (f.smsStatus ?? null) as SmsStatus | null,
        smsType: (f.smsType ?? null) as SmsType | null,
        templateCode: normalizeTemplateCode(f.templateCode),
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => messageCenterApi.deleteSms(id),
  },
  actions: [
    { key: 'detail', title: '详情', scope: 'row', type: 'primary', icon: 'lucide:eye' },
    { key: 'resend', title: '重发', scope: 'row', type: 'warning', icon: 'lucide:refresh-cw', visible: row => canResend((row as unknown as SmsListItemDto).smsStatus) },
    { key: 'delete', title: '删除', scope: 'row', type: 'error', icon: 'lucide:trash-2' },
  ],
}

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
  catch {
    currentSmsDetail.value = null
    message.error('加载系统短信详情失败')
  }
  finally {
    detailLoading.value = false
  }
}

async function resendSms(row: SmsListItemDto) {
  try {
    await messageCenterApi.updateSmsStatus({ basicId: row.basicId, smsStatus: SmsStatus.Pending })
    message.success('短信已重新加入发送队列')
    void smsPageRef.value?.reload()
  }
  catch {
    message.error('重发短信失败')
  }
}

function confirmDeleteSms(row: SmsListItemDto) {
  dialog.warning({
    title: '删除短信',
    content: '确定删除该短信？',
    positiveText: '确定',
    negativeText: '取消',
    onPositiveClick: () => deleteSms(row),
  })
}

async function deleteSms(row: SmsListItemDto) {
  try {
    await messageCenterApi.deleteSms(row.basicId)
    message.success('短信已删除')
    void smsPageRef.value?.reload()
  }
  catch {
    message.error('删除短信失败')
  }
}
</script>

<template>
  <div class="message-page">
    <NTabs v-model:value="activeTab" animated type="line">
      <NTabPane name="email" tab="系统邮件" display-directive="show:lazy">
        <SchemaPage ref="emailPageRef" :schema="emailSchema" @action="onEmailAction" />
      </NTabPane>
      <NTabPane name="sms" tab="系统短信" display-directive="show:lazy">
        <SchemaPage ref="smsPageRef" :schema="smsSchema" @action="onSmsAction" />
      </NTabPane>
    </NTabs>

    <NDrawer v-model:show="detailVisible" :width="620">
      <NDrawerContent closable :title="detailTab === 'email' ? '系统邮件详情' : '系统短信详情'">
        <NSpace v-if="detailLoading" justify="center">
          加载中...
        </NSpace>

        <NDescriptions v-else-if="detailTab === 'email' && currentEmailDetail" :column="1" bordered size="small">
          <NDescriptionsItem label="邮件主题">
            {{ currentEmailDetail.subject }}
          </NDescriptionsItem>
          <NDescriptionsItem label="邮件类型">
            {{ getOptionLabel(EMAIL_TYPE_OPTIONS, currentEmailDetail.emailType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="发送状态">
            {{ getOptionLabel(EMAIL_STATUS_OPTIONS, currentEmailDetail.emailStatus) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="业务引用">
            {{ currentEmailDetail.businessType || '-' }} / {{ currentEmailDetail.businessId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="用户引用">
            {{ currentEmailDetail.sendUserId || '-' }} -> {{ currentEmailDetail.receiveUserId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="模板编码">
            {{ currentEmailDetail.templateCode || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="内容标记">
            正文 {{ formatFlag(currentEmailDetail.hasBody) }}，附件 {{ formatFlag(currentEmailDetail.hasAttachment) }}，模板数据 {{ formatFlag(currentEmailDetail.hasTemplateData) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="收件标记">
            发件地址 {{ formatFlag(currentEmailDetail.hasSenderAddress) }}，收件地址 {{ formatFlag(currentEmailDetail.hasRecipientAddress) }}，抄送 {{ formatFlag(currentEmailDetail.hasCopyRecipient) }}，密送 {{ formatFlag(currentEmailDetail.hasBlindRecipient) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="失败明细">
            {{ formatFlag(currentEmailDetail.hasFailureDetail) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ formatFlag(currentEmailDetail.hasNote) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="重试">
            {{ formatRetry(currentEmailDetail) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="预定发送时间">
            {{ currentEmailDetail.scheduledTime ? formatDate(currentEmailDetail.scheduledTime) : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="实际发送时间">
            {{ currentEmailDetail.sendTime ? formatDate(currentEmailDetail.sendTime) : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDate(currentEmailDetail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ currentEmailDetail.modifiedTime ? formatDate(currentEmailDetail.modifiedTime) : '-' }}
          </NDescriptionsItem>
        </NDescriptions>

        <NDescriptions v-else-if="detailTab === 'sms' && currentSmsDetail" :column="1" bordered size="small">
          <NDescriptionsItem label="服务商">
            {{ currentSmsDetail.provider || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="短信类型">
            {{ getOptionLabel(SMS_TYPE_OPTIONS, currentSmsDetail.smsType) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="发送状态">
            {{ getOptionLabel(SMS_STATUS_OPTIONS, currentSmsDetail.smsStatus) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="业务引用">
            {{ currentSmsDetail.businessType || '-' }} / {{ currentSmsDetail.businessId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="用户引用">
            {{ currentSmsDetail.senderId || '-' }} -> {{ currentSmsDetail.receiverId || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="模板编码">
            {{ currentSmsDetail.templateCode || '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="费用">
            {{ currentSmsDetail.cost ?? '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="内容标记">
            正文 {{ formatFlag(currentSmsDetail.hasBody) }}，模板数据 {{ formatFlag(currentSmsDetail.hasTemplateData) }}，回执 {{ formatFlag(currentSmsDetail.hasProviderReceipt) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="接收号码">
            {{ formatFlag(currentSmsDetail.hasRecipientPhone) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="失败明细">
            {{ formatFlag(currentSmsDetail.hasFailureDetail) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="备注">
            {{ formatFlag(currentSmsDetail.hasNote) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="重试">
            {{ formatRetry(currentSmsDetail) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="预定发送时间">
            {{ currentSmsDetail.scheduledTime ? formatDate(currentSmsDetail.scheduledTime) : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="实际发送时间">
            {{ currentSmsDetail.sendTime ? formatDate(currentSmsDetail.sendTime) : '-' }}
          </NDescriptionsItem>
          <NDescriptionsItem label="创建时间">
            {{ formatDate(currentSmsDetail.createdTime) }}
          </NDescriptionsItem>
          <NDescriptionsItem label="修改时间">
            {{ currentSmsDetail.modifiedTime ? formatDate(currentSmsDetail.modifiedTime) : '-' }}
          </NDescriptionsItem>
        </NDescriptions>

        <div v-else class="py-8 text-center text-gray-400">
          暂无详情数据
        </div>
      </NDrawerContent>
    </NDrawer>
  </div>
</template>

<style scoped>
.message-page {
  height: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 的表格卡片，分页贴底）。
   被 NTabs 包裹时高度链在 tab pane 处断裂，这里补全传递链，
   与授权申请等以 NTabs 包裹 SchemaPage 的页面保持一致。 */
.message-page :deep(.n-tabs) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

.message-page :deep(.n-tabs-nav) {
  padding: 8px 12px 0;
}

.message-page :deep(.n-tabs-pane-wrapper) {
  flex: 1;
  height: 0;
}

.message-page :deep(.n-tab-pane) {
  height: 100%;
  padding: 0;
}
</style>

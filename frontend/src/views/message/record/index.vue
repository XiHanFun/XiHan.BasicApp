<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { ApiId, EmailDetailDto, EmailListItemDto, EmailType, SmsDetailDto, SmsListItemDto, SmsType } from '@/api'
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInput,
  NPagination,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, h, onMounted, reactive, ref } from 'vue'
import { createPageRequest, EmailStatus, messageCenterApi, SmsStatus } from '@/api'
import { Icon } from '~/components'
import { EMAIL_STATUS_OPTIONS, EMAIL_TYPE_OPTIONS, SMS_STATUS_OPTIONS, SMS_TYPE_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemMessagePage' })

type MessageTab = 'email' | 'sms'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

const message = useMessage()
const activeTab = ref<MessageTab>('email')
const emailLoaded = ref(false)
const smsLoaded = ref(false)
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailTab = ref<MessageTab>('email')
const currentEmailDetail = ref<EmailDetailDto | null>(null)
const currentSmsDetail = ref<SmsDetailDto | null>(null)
const actionLoading = ref(false)

const emailLoading = ref(false)
const emailList = ref<EmailListItemDto[]>([])
const emailTotal = ref(0)
const emailPage = ref(1)
const emailPageSize = ref(20)

const smsLoading = ref(false)
const smsList = ref<SmsListItemDto[]>([])
const smsTotal = ref(0)
const smsPage = ref(1)
const smsPageSize = ref(20)

const emailQuery = reactive({
  businessId: null as ApiId | null,
  businessType: '',
  emailStatus: null as EmailStatus | null,
  emailType: null as EmailType | null,
  keyword: '',
  receiveUserId: null as ApiId | null,
  sendUserId: null as ApiId | null,
  templateCode: null as string | null,
})

const smsQuery = reactive({
  businessId: null as ApiId | null,
  businessType: '',
  keyword: '',
  provider: '',
  receiverId: null as ApiId | null,
  senderId: null as ApiId | null,
  smsStatus: null as SmsStatus | null,
  smsType: null as SmsType | null,
  templateCode: null as string | null,
})

const messageTabOptions = [
  { label: '系统邮件', value: 'email' },
  { label: '系统短信', value: 'sms' },
]

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function normalizeId(value: ApiId | null) {
  return value || null
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

async function fetchEmailData() {
  emailLoading.value = true
  try {
    const result = await messageCenterApi.emailPage({
      ...createPageRequest({
        page: {
          pageIndex: emailPage.value,
          pageSize: emailPageSize.value,
        },
      }),
      businessId: normalizeId(emailQuery.businessId),
      businessType: normalizeNullable(emailQuery.businessType),
      emailStatus: emailQuery.emailStatus,
      emailType: emailQuery.emailType,
      keyword: normalizeNullable(emailQuery.keyword),
      receiveUserId: normalizeId(emailQuery.receiveUserId),
      sendUserId: normalizeId(emailQuery.sendUserId),
      templateCode: emailQuery.templateCode?.trim() || undefined,
    })
    emailList.value = result.items
    emailTotal.value = result.page.totalCount
    emailLoaded.value = true
  }
  catch {
    message.error('查询系统邮件失败')
    emailList.value = []
    emailTotal.value = 0
  }
  finally {
    emailLoading.value = false
  }
}

async function fetchSmsData() {
  smsLoading.value = true
  try {
    const result = await messageCenterApi.smsPage({
      ...createPageRequest({
        page: {
          pageIndex: smsPage.value,
          pageSize: smsPageSize.value,
        },
      }),
      businessId: normalizeId(smsQuery.businessId),
      businessType: normalizeNullable(smsQuery.businessType),
      keyword: normalizeNullable(smsQuery.keyword),
      provider: normalizeNullable(smsQuery.provider),
      receiverId: normalizeId(smsQuery.receiverId),
      senderId: normalizeId(smsQuery.senderId),
      smsStatus: smsQuery.smsStatus,
      smsType: smsQuery.smsType,
      templateCode: smsQuery.templateCode?.trim() || undefined,
    })
    smsList.value = result.items
    smsTotal.value = result.page.totalCount
    smsLoaded.value = true
  }
  catch {
    message.error('查询系统短信失败')
    smsList.value = []
    smsTotal.value = 0
  }
  finally {
    smsLoading.value = false
  }
}

const emailColumns = computed<DataTableColumns<EmailListItemDto>>(() => [
  { key: 'subject', title: '邮件主题', minWidth: 220, ellipsis: { tooltip: true } },
  {
    key: 'emailType',
    title: '邮件类型',
    minWidth: 110,
    render: row => getOptionLabel(EMAIL_TYPE_OPTIONS, row.emailType),
  },
  {
    key: 'emailStatus',
    title: '发送状态',
    width: 100,
    render: row =>
      h(NTag, { type: getMessageStatusTagType(row.emailStatus), round: true, size: 'small' }, () => getOptionLabel(EMAIL_STATUS_OPTIONS, row.emailStatus)),
  },
  {
    key: 'isHtml',
    title: 'HTML',
    width: 82,
    render: row =>
      h(NTag, { type: row.isHtml ? 'info' : 'default', round: true, size: 'small' }, () => row.isHtml ? '是' : '否'),
  },
  { key: 'businessType', title: '业务类型', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'sendUserId', title: '发送用户', minWidth: 110 },
  { key: 'receiveUserId', title: '接收用户', minWidth: 110 },
  { key: 'templateCode', title: '模板编码', minWidth: 130 },
  {
    key: 'retryCount',
    title: '重试',
    minWidth: 90,
    render: row => formatRetry(row),
  },
  {
    key: 'sendTime',
    title: '发送时间',
    minWidth: 170,
    sorter: true,
    render: row => row.sendTime ? formatDate(row.sendTime) : '-',
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render: row => formatDate(row.createdTime),
  },
  {
    key: 'actions',
    title: '操作',
    width: 160,
    fixed: 'right',
    render: row =>
      h(NSpace, { size: 4 }, () => [
        h(NTooltip, {}, {
          trigger: () => h(NButton, { ariaLabel: '详情', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleEmailDetail(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '详情',
        }),
        canResend(row.emailStatus)
          ? h(NTooltip, {}, {
              trigger: () => h(NButton, { ariaLabel: '重发', circle: true, quaternary: true, size: 'small', type: 'warning', loading: actionLoading.value, onClick: () => handleResendEmail(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:refresh-cw' })) }),
              default: () => '重发',
            })
          : null,
        h(NPopconfirm, { onPositiveClick: () => handleDeleteEmail(row) }, {
          trigger: () => h(NButton, { ariaLabel: '删除', circle: true, quaternary: true, size: 'small', type: 'error', loading: actionLoading.value }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => '确定删除该邮件？',
        }),
      ]),
  },
])

const smsColumns = computed<DataTableColumns<SmsListItemDto>>(() => [
  { key: 'provider', title: '服务商', minWidth: 140, ellipsis: { tooltip: true } },
  {
    key: 'smsType',
    title: '短信类型',
    minWidth: 110,
    render: row => getOptionLabel(SMS_TYPE_OPTIONS, row.smsType),
  },
  {
    key: 'smsStatus',
    title: '发送状态',
    width: 100,
    render: row =>
      h(NTag, { type: getMessageStatusTagType(row.smsStatus), round: true, size: 'small' }, () => getOptionLabel(SMS_STATUS_OPTIONS, row.smsStatus)),
  },
  { key: 'businessType', title: '业务类型', minWidth: 130, ellipsis: { tooltip: true } },
  { key: 'senderId', title: '发送用户', minWidth: 110 },
  { key: 'receiverId', title: '接收用户', minWidth: 110 },
  { key: 'templateCode', title: '模板编码', minWidth: 130 },
  { key: 'cost', title: '费用', minWidth: 90 },
  {
    key: 'retryCount',
    title: '重试',
    minWidth: 90,
    render: row => formatRetry(row),
  },
  {
    key: 'sendTime',
    title: '发送时间',
    minWidth: 170,
    sorter: true,
    render: row => row.sendTime ? formatDate(row.sendTime) : '-',
  },
  {
    key: 'createdTime',
    title: '创建时间',
    minWidth: 170,
    sorter: true,
    render: row => formatDate(row.createdTime),
  },
  {
    key: 'actions',
    title: '操作',
    width: 160,
    fixed: 'right',
    render: row =>
      h(NSpace, { size: 4 }, () => [
        h(NTooltip, {}, {
          trigger: () => h(NButton, { ariaLabel: '详情', circle: true, quaternary: true, size: 'small', type: 'primary', onClick: () => handleSmsDetail(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:eye' })) }),
          default: () => '详情',
        }),
        canResend(row.smsStatus)
          ? h(NTooltip, {}, {
              trigger: () => h(NButton, { ariaLabel: '重发', circle: true, quaternary: true, size: 'small', type: 'warning', loading: actionLoading.value, onClick: () => handleResendSms(row) }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:refresh-cw' })) }),
              default: () => '重发',
            })
          : null,
        h(NPopconfirm, { onPositiveClick: () => handleDeleteSms(row) }, {
          trigger: () => h(NButton, { ariaLabel: '删除', circle: true, quaternary: true, size: 'small', type: 'error', loading: actionLoading.value }, { icon: () => h(NIcon, null, () => h(Icon, { icon: 'lucide:trash-2' })) }),
          default: () => '确定删除该短信？',
        }),
      ]),
  },
])

const emailTotalPages = computed(() => Math.max(1, Math.ceil(emailTotal.value / emailPageSize.value)))
const smsTotalPages = computed(() => Math.max(1, Math.ceil(smsTotal.value / smsPageSize.value)))

function reloadActiveGrid() {
  if (activeTab.value === 'email') {
    fetchEmailData()
    return
  }

  fetchSmsData()
}

function handleSearch() {
  if (activeTab.value === 'email') {
    emailPage.value = 1
    fetchEmailData()
  }
  else {
    smsPage.value = 1
    fetchSmsData()
  }
}

function handleTabChanged() {
  if (activeTab.value === 'email' && !emailLoaded.value) {
    fetchEmailData()
  }
  else if (activeTab.value === 'sms' && !smsLoaded.value) {
    fetchSmsData()
  }
}

function handleReset() {
  if (activeTab.value === 'email') {
    emailQuery.businessId = null
    emailQuery.businessType = ''
    emailQuery.emailStatus = null
    emailQuery.emailType = null
    emailQuery.keyword = ''
    emailQuery.receiveUserId = null
    emailQuery.sendUserId = null
    emailQuery.templateCode = null
    emailPage.value = 1
    fetchEmailData()
  }
  else {
    smsQuery.businessId = null
    smsQuery.businessType = ''
    smsQuery.keyword = ''
    smsQuery.provider = ''
    smsQuery.receiverId = null
    smsQuery.senderId = null
    smsQuery.smsStatus = null
    smsQuery.smsType = null
    smsQuery.templateCode = null
    smsPage.value = 1
    fetchSmsData()
  }
}

function handleEmailPageChange(page: number) {
  emailPage.value = page
  fetchEmailData()
}

function handleEmailPageSizeChange(size: number) {
  emailPageSize.value = size
  emailPage.value = 1
  fetchEmailData()
}

function handleSmsPageChange(page: number) {
  smsPage.value = page
  fetchSmsData()
}

function handleSmsPageSizeChange(size: number) {
  smsPageSize.value = size
  smsPage.value = 1
  fetchSmsData()
}

async function handleResendEmail(row: EmailListItemDto) {
  actionLoading.value = true
  try {
    await messageCenterApi.updateEmailStatus({
      basicId: row.basicId,
      emailStatus: EmailStatus.Pending,
    })
    message.success('邮件已重新加入发送队列')
    reloadActiveGrid()
  }
  catch {
    message.error('重发邮件失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleResendSms(row: SmsListItemDto) {
  actionLoading.value = true
  try {
    await messageCenterApi.updateSmsStatus({
      basicId: row.basicId,
      smsStatus: SmsStatus.Pending,
    })
    message.success('短信已重新加入发送队列')
    reloadActiveGrid()
  }
  catch {
    message.error('重发短信失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleDeleteEmail(row: EmailListItemDto) {
  actionLoading.value = true
  try {
    await messageCenterApi.deleteEmail(row.basicId)
    message.success('邮件已删除')
    reloadActiveGrid()
  }
  catch {
    message.error('删除邮件失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleDeleteSms(row: SmsListItemDto) {
  actionLoading.value = true
  try {
    await messageCenterApi.deleteSms(row.basicId)
    message.success('短信已删除')
    reloadActiveGrid()
  }
  catch {
    message.error('删除短信失败')
  }
  finally {
    actionLoading.value = false
  }
}

async function handleEmailDetail(row: EmailListItemDto) {
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

async function handleSmsDetail(row: SmsListItemDto) {
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

onMounted(() => fetchEmailData())
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <div class="xh-query-panel mb-2" style="padding:10px 16px;background:var(--n-card-color);border-radius:var(--n-border-radius);">
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="activeTab"
          :options="messageTabOptions"
          style="width: 120px"
          @update:value="handleTabChanged"
        />

        <template v-if="activeTab === 'email'">
          <NInput
            v-model:value="emailQuery.keyword"
            clearable
            placeholder="搜索主题/业务类型"
            style="width: 220px"
            @keyup.enter="handleSearch"
          />
          <NSelect
            v-model:value="emailQuery.emailType"
            :options="EMAIL_TYPE_OPTIONS"
            clearable
            placeholder="邮件类型"
            style="width: 120px"
          />
          <NSelect
            v-model:value="emailQuery.emailStatus"
            :options="EMAIL_STATUS_OPTIONS"
            clearable
            placeholder="发送状态"
            style="width: 120px"
          />
          <NInput
            v-model:value="emailQuery.businessType"
            clearable
            placeholder="业务类型"
            style="width: 130px"
            @keyup.enter="handleSearch"
          />
          <NInput v-model:value="emailQuery.templateCode" clearable placeholder="模板编码" style="width: 130px" />
          <NInput v-model:value="emailQuery.sendUserId" clearable placeholder="发送用户" style="width: 120px" />
          <NInput
            v-model:value="emailQuery.receiveUserId"
            clearable
            placeholder="接收用户"
            style="width: 120px"
          />
        </template>

        <template v-else>
          <NInput
            v-model:value="smsQuery.keyword"
            clearable
            placeholder="搜索服务商/业务类型"
            style="width: 220px"
            @keyup.enter="handleSearch"
          />
          <NSelect
            v-model:value="smsQuery.smsType"
            :options="SMS_TYPE_OPTIONS"
            clearable
            placeholder="短信类型"
            style="width: 120px"
          />
          <NSelect
            v-model:value="smsQuery.smsStatus"
            :options="SMS_STATUS_OPTIONS"
            clearable
            placeholder="发送状态"
            style="width: 120px"
          />
          <NInput
            v-model:value="smsQuery.provider"
            clearable
            placeholder="服务商"
            style="width: 120px"
            @keyup.enter="handleSearch"
          />
          <NInput
            v-model:value="smsQuery.businessType"
            clearable
            placeholder="业务类型"
            style="width: 130px"
            @keyup.enter="handleSearch"
          />
          <NInput v-model:value="smsQuery.templateCode" clearable placeholder="模板编码" style="width: 130px" />
          <NInput v-model:value="smsQuery.senderId" clearable placeholder="发送用户" style="width: 120px" />
          <NInput v-model:value="smsQuery.receiverId" clearable placeholder="接收用户" style="width: 120px" />
        </template>

        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </div>

    <NCard v-show="activeTab === 'email'" content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="emailColumns"
        :data="emailList"
        :loading="emailLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row: EmailListItemDto) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">
          共 <strong>{{ emailTotal }}</strong> 条，第 <strong>{{ emailPage }}</strong> / {{ emailTotalPages }} 页
        </div>
        <NPagination :page="emailPage" :page-count="emailTotalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="emailPageSize" show-size-picker @update:page="handleEmailPageChange" @update:page-size="handleEmailPageSizeChange" />
      </div>
    </NCard>

    <NCard v-show="activeTab === 'sms'" content-style="padding:0;display:flex;flex-direction:column;height:100%;" :bordered="false" class="flex-1" style="height:0;">
      <NDataTable
        :columns="smsColumns"
        :data="smsList"
        :loading="smsLoading"
        :bordered="false"
        :single-line="false"
        :row-key="(row: SmsListItemDto) => row.basicId"
        :scroll-x="2000"
        size="small"
        striped
        flex-height
        style="flex:1;"
      />
      <div style="display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-top:1px solid var(--n-border-color);flex-shrink:0;">
        <div style="font-size:13px;color:var(--n-text-color-3);">
          共 <strong>{{ smsTotal }}</strong> 条，第 <strong>{{ smsPage }}</strong> / {{ smsTotalPages }} 页
        </div>
        <NPagination :page="smsPage" :page-count="smsTotalPages" :page-slot="7" :page-sizes="[10,20,50,100]" :page-size="smsPageSize" show-size-picker @update:page="handleSmsPageChange" @update:page-size="handleSmsPageSizeChange" />
      </div>
    </NCard>

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

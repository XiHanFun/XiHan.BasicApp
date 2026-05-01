<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type { ApiId, EmailDetailDto, EmailListItemDto, EmailType, SmsDetailDto, SmsListItemDto, SmsStatus, SmsType } from '@/api'
import {
  NButton,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NIcon,
  NInputNumber,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { reactive, ref } from 'vue'
import { createPageRequest, EmailStatus, messageApi } from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { EMAIL_STATUS_OPTIONS, EMAIL_TYPE_OPTIONS, SMS_STATUS_OPTIONS, SMS_TYPE_OPTIONS } from '~/constants'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemMessagePage' })

type MessageTab = 'email' | 'sms'
type TagType = 'default' | 'error' | 'info' | 'success' | 'warning'

interface EmailGridResult {
  items: EmailListItemDto[]
  total: number
}

interface SmsGridResult {
  items: SmsListItemDto[]
  total: number
}

const message = useMessage()
const activeTab = ref<MessageTab>('email')
const emailGrid = ref<VxeGridInstance<EmailListItemDto>>()
const smsGrid = ref<VxeGridInstance<SmsListItemDto>>()
const detailVisible = ref(false)
const detailLoading = ref(false)
const detailTab = ref<MessageTab>('email')
const currentEmailDetail = ref<EmailDetailDto | null>(null)
const currentSmsDetail = ref<SmsDetailDto | null>(null)

const emailQuery = reactive({
  businessId: null as ApiId | null,
  businessType: '',
  emailStatus: null as EmailStatus | null,
  emailType: null as EmailType | null,
  keyword: '',
  receiveUserId: null as ApiId | null,
  sendUserId: null as ApiId | null,
  templateId: null as ApiId | null,
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
  templateId: null as ApiId | null,
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
  return value && value > 0 ? value : null
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

function handleEmailQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<EmailGridResult> {
  return messageApi
    .emailPage({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
        },
      }),
      businessId: normalizeId(emailQuery.businessId),
      businessType: normalizeNullable(emailQuery.businessType),
      emailStatus: emailQuery.emailStatus,
      emailType: emailQuery.emailType,
      keyword: normalizeNullable(emailQuery.keyword),
      receiveUserId: normalizeId(emailQuery.receiveUserId),
      sendUserId: normalizeId(emailQuery.sendUserId),
      templateId: normalizeId(emailQuery.templateId),
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询系统邮件失败')
      return {
        items: [],
        total: 0,
      }
    })
}

function handleSmsQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<SmsGridResult> {
  return messageApi
    .smsPage({
      ...createPageRequest({
        page: {
          pageIndex: page.currentPage,
          pageSize: page.pageSize,
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
      templateId: normalizeId(smsQuery.templateId),
    })
    .then(result => ({
      items: result.items,
      total: result.page.totalCount,
    }))
    .catch(() => {
      message.error('查询系统短信失败')
      return {
        items: [],
        total: 0,
      }
    })
}

const emailTableOptions = useVxeTable<EmailListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'subject', minWidth: 220, showOverflow: 'tooltip', title: '邮件主题' },
      {
        field: 'emailType',
        formatter: ({ cellValue }) => getOptionLabel(EMAIL_TYPE_OPTIONS, cellValue),
        minWidth: 110,
        title: '邮件类型',
      },
      {
        field: 'emailStatus',
        slots: { default: 'col_email_status' },
        title: '发送状态',
        width: 100,
      },
      {
        field: 'isHtml',
        slots: { default: 'col_email_html' },
        title: 'HTML',
        width: 82,
      },
      { field: 'businessType', minWidth: 130, showOverflow: 'tooltip', title: '业务类型' },
      { field: 'sendUserId', minWidth: 110, title: '发送用户' },
      { field: 'receiveUserId', minWidth: 110, title: '接收用户' },
      { field: 'templateId', minWidth: 110, title: '模板' },
      {
        field: 'retryCount',
        formatter: ({ row }) => formatRetry(row),
        minWidth: 90,
        title: '重试',
      },
      {
        field: 'sendTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        sortable: true,
        title: '发送时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_email_actions' },
        title: '操作',
        width: 90,
      },
    ],
    id: 'sys_email_message',
    name: '系统邮件',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleEmailQueryApi(page),
      },
    },
  },
)

const smsTableOptions = useVxeTable<SmsListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'provider', minWidth: 140, showOverflow: 'tooltip', title: '服务商' },
      {
        field: 'smsType',
        formatter: ({ cellValue }) => getOptionLabel(SMS_TYPE_OPTIONS, cellValue),
        minWidth: 110,
        title: '短信类型',
      },
      {
        field: 'smsStatus',
        slots: { default: 'col_sms_status' },
        title: '发送状态',
        width: 100,
      },
      { field: 'businessType', minWidth: 130, showOverflow: 'tooltip', title: '业务类型' },
      { field: 'senderId', minWidth: 110, title: '发送用户' },
      { field: 'receiverId', minWidth: 110, title: '接收用户' },
      { field: 'templateId', minWidth: 110, title: '模板' },
      { field: 'cost', minWidth: 90, title: '费用' },
      {
        field: 'retryCount',
        formatter: ({ row }) => formatRetry(row),
        minWidth: 90,
        title: '重试',
      },
      {
        field: 'sendTime',
        formatter: ({ cellValue }) => (cellValue ? formatDate(cellValue) : '-'),
        minWidth: 170,
        sortable: true,
        title: '发送时间',
      },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        sortable: true,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_sms_actions' },
        title: '操作',
        width: 90,
      },
    ],
    id: 'sys_sms_message',
    name: '系统短信',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleSmsQueryApi(page),
      },
    },
  },
)

function reloadActiveGrid() {
  if (activeTab.value === 'email') {
    emailGrid.value?.commitProxy('reload')
    return
  }

  smsGrid.value?.commitProxy('reload')
}

function handleSearch() {
  reloadActiveGrid()
}

function handleTabChanged() {
  reloadActiveGrid()
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
    emailQuery.templateId = null
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
    smsQuery.templateId = null
  }

  reloadActiveGrid()
}

async function handleEmailDetail(row: EmailListItemDto) {
  detailTab.value = 'email'
  detailVisible.value = true
  detailLoading.value = true
  currentSmsDetail.value = null

  try {
    currentEmailDetail.value = await messageApi.emailDetail(row.basicId)
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
    currentSmsDetail.value = await messageApi.smsDetail(row.basicId)
  }
  catch {
    currentSmsDetail.value = null
    message.error('加载系统短信详情失败')
  }
  finally {
    detailLoading.value = false
  }
}
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="activeTab"
          :options="messageTabOptions"
          style="width: 120px"
          @update:value="handleTabChanged"
        />

        <template v-if="activeTab === 'email'">
          <vxe-input
            v-model="emailQuery.keyword"
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
          <vxe-input
            v-model="emailQuery.businessType"
            clearable
            placeholder="业务类型"
            style="width: 130px"
            @keyup.enter="handleSearch"
          />
          <NInputNumber v-model:value="emailQuery.templateId" clearable placeholder="模板 ID" style="width: 120px" />
          <NInputNumber v-model:value="emailQuery.sendUserId" clearable placeholder="发送用户" style="width: 120px" />
          <NInputNumber
            v-model:value="emailQuery.receiveUserId"
            clearable
            placeholder="接收用户"
            style="width: 120px"
          />
        </template>

        <template v-else>
          <vxe-input
            v-model="smsQuery.keyword"
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
          <vxe-input
            v-model="smsQuery.provider"
            clearable
            placeholder="服务商"
            style="width: 120px"
            @keyup.enter="handleSearch"
          />
          <vxe-input
            v-model="smsQuery.businessType"
            clearable
            placeholder="业务类型"
            style="width: 130px"
            @keyup.enter="handleSearch"
          />
          <NInputNumber v-model:value="smsQuery.templateId" clearable placeholder="模板 ID" style="width: 120px" />
          <NInputNumber v-model:value="smsQuery.senderId" clearable placeholder="发送用户" style="width: 120px" />
          <NInputNumber v-model:value="smsQuery.receiverId" clearable placeholder="接收用户" style="width: 120px" />
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
    </XSystemQueryPanel>

    <vxe-card v-show="activeTab === 'email'" class="flex-1" style="height: 0">
      <vxe-grid ref="emailGrid" v-bind="emailTableOptions">
        <template #toolbar_buttons />

        <template #col_email_status="{ row }">
          <NTag :type="getMessageStatusTagType(row.emailStatus)" round size="small">
            {{ getOptionLabel(EMAIL_STATUS_OPTIONS, row.emailStatus) }}
          </NTag>
        </template>

        <template #col_email_html="{ row }">
          <NTag :type="row.isHtml ? 'info' : 'default'" round size="small">
            {{ row.isHtml ? '是' : '否' }}
          </NTag>
        </template>

        <template #col_email_actions="{ row }">
          <NButton size="small" text type="primary" @click="handleEmailDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
            详情
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

    <vxe-card v-show="activeTab === 'sms'" class="flex-1" style="height: 0">
      <vxe-grid ref="smsGrid" v-bind="smsTableOptions">
        <template #toolbar_buttons />

        <template #col_sms_status="{ row }">
          <NTag :type="getMessageStatusTagType(row.smsStatus)" round size="small">
            {{ getOptionLabel(SMS_STATUS_OPTIONS, row.smsStatus) }}
          </NTag>
        </template>

        <template #col_sms_actions="{ row }">
          <NButton size="small" text type="primary" @click="handleSmsDetail(row)">
            <template #icon>
              <NIcon><Icon icon="lucide:eye" /></NIcon>
            </template>
            详情
          </NButton>
        </template>
      </vxe-grid>
    </vxe-card>

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
          <NDescriptionsItem label="模板">
            {{ currentEmailDetail.templateId || '-' }}
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
          <NDescriptionsItem label="模板">
            {{ currentSmsDetail.templateId || '-' }}
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
      </NDrawerContent>
    </NDrawer>
  </div>
</template>

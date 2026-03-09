<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { SysEmail, SysSms } from '~/types'
import {
  NButton,
  NCard,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSpace,
  useMessage,
} from 'naive-ui'
import { ref } from 'vue'
import { getPendingEmailsByMessageApi, getPendingSmsByMessageApi, sendMessageApi } from '@/api'
import { CrudProTable, XJsonViewer } from '~/components'

defineOptions({ name: 'SystemMessagePage' })

const message = useMessage()
const loadingEmail = ref(false)
const loadingSms = ref(false)
const sending = ref(false)
const tenantId = ref<number | null>(null)
const maxCount = ref(20)
const pendingEmails = ref<SysEmail[]>([])
const pendingSms = ref<SysSms[]>([])
const sendResult = ref('')
const sendPayload = ref(
  JSON.stringify(
    {
      recipientUserIds: [],
      channels: ['notification'],
      title: '系统通知',
      content: '这是一条统一消息测试内容',
      tenantId: null,
    },
    null,
    2,
  ),
)

const emailColumns: DataTableColumns<SysEmail> = [
  { title: 'ID', key: 'basicId', width: 100 },
  { title: '收件人', key: 'toEmail', width: 180 },
  { title: '主题', key: 'subject', minWidth: 180 },
  { title: '状态', key: 'emailStatus', width: 90 },
  { title: '计划时间', key: 'scheduledTime', width: 170 },
]

const smsColumns: DataTableColumns<SysSms> = [
  { title: 'ID', key: 'basicId', width: 100 },
  { title: '手机号', key: 'toPhone', width: 160 },
  { title: '内容', key: 'content', minWidth: 220, ellipsis: { tooltip: true } },
  { title: '状态', key: 'smsStatus', width: 90 },
  { title: '计划时间', key: 'scheduledTime', width: 170 },
]

async function loadPendingEmails() {
  try {
    loadingEmail.value = true
    pendingEmails.value = await getPendingEmailsByMessageApi(maxCount.value, tenantId.value ?? undefined)
  }
  catch {
    message.error('加载待分发邮件失败')
  }
  finally {
    loadingEmail.value = false
  }
}

async function loadPendingSms() {
  try {
    loadingSms.value = true
    pendingSms.value = await getPendingSmsByMessageApi(maxCount.value, tenantId.value ?? undefined)
  }
  catch {
    message.error('加载待分发短信失败')
  }
  finally {
    loadingSms.value = false
  }
}

async function handleSend() {
  try {
    sending.value = true
    const payload = JSON.parse(sendPayload.value)
    const result = await sendMessageApi(payload)
    message.success('发送成功')
    sendResult.value = JSON.stringify(result, null, 2)
  }
  catch {
    message.error('发送失败，请检查 JSON 格式与字段')
  }
  finally {
    sending.value = false
  }
}
</script>

<template>
  <div class="space-y-3">
    <NCard title="统一消息发送" :bordered="false" size="small">
      <NForm label-placement="left" label-width="90px">
        <NFormItem label="消息命令">
          <NInput
            v-model:value="sendPayload"
            type="textarea"
            :rows="8"
            placeholder="请输入 SendMessageCommand JSON"
          />
        </NFormItem>
      </NForm>
      <NButton type="primary" size="small" :loading="sending" @click="handleSend">
        发送消息
      </NButton>
    </NCard>

    <XJsonViewer title="发送结果" :raw-text="sendResult || '暂无发送结果'" :max-height="220" />

    <NCard title="待分发队列" :bordered="false" size="small">
      <NSpace align="center" class="mb-3">
        <span class="text-sm text-gray-500">租户ID</span>
        <NInputNumber v-model:value="tenantId" clearable />
        <span class="text-sm text-gray-500">最大数量</span>
        <NInputNumber v-model:value="maxCount" :min="1" :max="200" />
        <NButton size="small" @click="loadPendingEmails">
          刷新邮件
        </NButton>
        <NButton size="small" @click="loadPendingSms">
          刷新短信
        </NButton>
      </NSpace>

      <div class="space-y-3">
        <CrudProTable
          :columns="emailColumns"
          :data="pendingEmails"
          :loading="loadingEmail"
          :row-key="(row) => row.basicId"
          :show-toolbar="false"
          :show-pagination="false"
        />
        <CrudProTable
          :columns="smsColumns"
          :data="pendingSms"
          :loading="loadingSms"
          :row-key="(row) => row.basicId"
          :show-toolbar="false"
          :show-pagination="false"
        />
      </div>
    </NCard>
  </div>
</template>

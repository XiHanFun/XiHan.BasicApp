<script lang="ts" setup>
import {
  NButton,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSelect,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { ref } from 'vue'
import { messageApi } from '@/api'
import { formatDate } from '~/utils'

defineOptions({ name: 'SystemMessagePage' })

const message = useMessage()
const activeTab = ref('send')
const sendLoading = ref(false)
const pendingLoading = ref(false)

const CHANNEL_OPTIONS = [
  { label: '邮件', value: 'Email' },
  { label: '短信', value: 'Sms' },
]

const sendForm = ref({
  channel: 'Email',
  to: '',
  subject: '',
  content: '',
  isHtml: false,
})

const pendingTenantId = ref(0)
const pendingMaxCount = ref(10)
const pendingEmails = ref<any[]>([])
const pendingSms = ref<any[]>([])

async function handleSend() {
  if (!sendForm.value.to || !sendForm.value.content) {
    message.warning('请填写完整信息')
    return
  }
  try {
    sendLoading.value = true
    await messageApi.send({
      channel: sendForm.value.channel,
      to: sendForm.value.to,
      subject: sendForm.value.subject,
      content: sendForm.value.content,
      isHtml: sendForm.value.isHtml,
    })
    message.success('发送成功')
    sendForm.value = { channel: 'Email', to: '', subject: '', content: '', isHtml: false }
  }
  catch {
    message.error('发送失败')
  }
  finally {
    sendLoading.value = false
  }
}

async function fetchPending() {
  try {
    pendingLoading.value = true
    const [emails, sms] = await Promise.all([
      messageApi.getPendingEmails(pendingMaxCount.value, pendingTenantId.value),
      messageApi.getPendingSms(pendingMaxCount.value, pendingTenantId.value),
    ])
    pendingEmails.value = Array.isArray(emails) ? emails : []
    pendingSms.value = Array.isArray(sms) ? sms : []
  }
  catch {
    message.error('获取待处理消息失败')
  }
  finally {
    pendingLoading.value = false
  }
}
</script>

<template>
  <div class="h-full flex flex-col">
    <vxe-card>
      <NTabs v-model:value="activeTab" type="line">
        <NTabPane name="send" tab="发送消息">
          <NForm :model="sendForm" label-placement="left" label-width="80px" style="max-width: 600px" class="mt-4">
            <NFormItem label="发送渠道">
              <NSelect v-model:value="sendForm.channel" :options="CHANNEL_OPTIONS" />
            </NFormItem>
            <NFormItem label="收件人">
              <NInput v-model:value="sendForm.to" :placeholder="sendForm.channel === 'Email' ? '邮箱地址' : '手机号'" />
            </NFormItem>
            <NFormItem v-if="sendForm.channel === 'Email'" label="主题">
              <NInput v-model:value="sendForm.subject" placeholder="邮件主题" />
            </NFormItem>
            <NFormItem label="内容">
              <NInput v-model:value="sendForm.content" type="textarea" :rows="5" placeholder="消息内容" />
            </NFormItem>
            <NFormItem>
              <NButton type="primary" :loading="sendLoading" @click="handleSend">
                发送
              </NButton>
            </NFormItem>
          </NForm>
        </NTabPane>
        <NTabPane name="pending" tab="待处理队列">
          <div class="mt-4">
            <div class="flex items-center gap-3 mb-4">
              <NInputNumber v-model:value="pendingTenantId" :min="0" placeholder="租户ID" style="width: 120px" />
              <NInputNumber v-model:value="pendingMaxCount" :min="1" :max="100" placeholder="数量" style="width: 120px" />
              <NButton type="primary" :loading="pendingLoading" @click="fetchPending">
                查询
              </NButton>
            </div>
            <div v-if="pendingEmails.length" class="mb-4">
              <h4 class="mb-2 font-medium">
                待发送邮件 ({{ pendingEmails.length }})
              </h4>
              <div v-for="(item, i) in pendingEmails" :key="i" class="rounded border p-3 mb-2">
                <div class="flex items-center gap-2">
                  <NTag type="info" size="small">
                    邮件
                  </NTag>
                  <span class="font-medium">{{ item.subject || item.toEmail }}</span>
                  <span class="ml-auto text-xs text-gray-400">{{ formatDate(item.createTime) }}</span>
                </div>
              </div>
            </div>
            <div v-if="pendingSms.length">
              <h4 class="mb-2 font-medium">
                待发送短信 ({{ pendingSms.length }})
              </h4>
              <div v-for="(item, i) in pendingSms" :key="i" class="rounded border p-3 mb-2">
                <div class="flex items-center gap-2">
                  <NTag type="warning" size="small">
                    短信
                  </NTag>
                  <span class="font-medium">{{ item.toPhone }}</span>
                  <span class="ml-auto text-xs text-gray-400">{{ formatDate(item.createTime) }}</span>
                </div>
              </div>
            </div>
            <div v-if="!pendingEmails.length && !pendingSms.length && !pendingLoading" class="py-8 text-center text-gray-400">
              暂无待处理消息
            </div>
          </div>
        </NTabPane>
      </NTabs>
    </vxe-card>
  </div>
</template>

<script lang="ts" setup>
import {
  NAlert,
  NButton,
  NCard,
  NDivider,
  NEmpty,
  NGrid,
  NGridItem,
  NIcon,
  NInput,
  NInputGroup,
  NSelect,
  NSpace,
  NTooltip,
  useDialog,
  useMessage,
} from 'naive-ui'
import { ref } from 'vue'
import { Icon } from '~/iconify'
import { copyToClipboard, formatDate } from '~/utils'

interface ApiCredential { appId: string, appKey: string, createdAt: string, lastUsedAt?: string }

const message = useMessage()
const dialog = useDialog()

const apiCredentials = ref<ApiCredential[]>([])
const newSecret = ref('')
const ipWhitelist = ref('')
const signAlgorithm = ref('HmacSHA256')
const signAlgorithmOptions = [
  { label: 'HmacSHA256', value: 'HmacSHA256' },
  { label: 'HmacSHA512', value: 'HmacSHA512' },
  { label: 'RSA-SHA256', value: 'RSA-SHA256' },
]

function handleCreateCredential() {
  message.info('需要后端实现 OpenAPI 凭证创建接口')
}
function handleRotateSecret(appId: string) {
  dialog.warning({
    title: '滚动密钥',
    content: '生成新密钥后旧密钥将立即失效，确定继续？',
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: () => {
      message.info(`[${appId}] 需要后端实现密钥滚动接口`)
    },
  })
}
function handleDeleteCredential(appId: string) {
  dialog.error({
    title: '删除凭证',
    content: '删除后使用此凭证的所有集成将立即失效，此操作不可恢复。',
    positiveText: '确认删除',
    negativeText: '取消',
    onPositiveClick: () => {
      message.info(`[${appId}] 需要后端实现凭证删除接口`)
    },
  })
}
function handleSaveIpWhitelist() {
  message.info('需要后端实现 IP 白名单接口')
}
</script>

<template>
  <div class="pf-tab-body">
    <NAlert type="warning" :bordered="false">
      API 凭证功能需配合后端实现，当前为界面预览。Secret 仅在创建时显示一次。
    </NAlert>

    <NCard :bordered="false" size="small" class="pf-card">
      <template #header>
        <div class="pf-card-header">
          <Icon icon="lucide:key" width="16" /><span>API 凭证</span>
        </div>
      </template>
      <template #header-extra>
        <NButton size="small" type="primary" @click="handleCreateCredential">
          <template #icon>
            <NIcon><Icon icon="lucide:plus" /></NIcon>
          </template>创建凭证
        </NButton>
      </template>
      <NEmpty v-if="apiCredentials.length === 0" description="暂无 API 凭证">
        <template #extra>
          <NButton size="small" @click="handleCreateCredential">
            创建第一个凭证
          </NButton>
        </template>
      </NEmpty>
      <div v-else class="pf-list">
        <div v-for="cred in apiCredentials" :key="cred.appId" class="pf-list-item" style="flex-wrap: wrap;">
          <div class="pf-list-body" style="width: 100%;">
            <div class="pf-list-title" style="font-family: monospace">
              {{ cred.appId }}
            </div>
            <div class="pf-list-desc">
              创建于 {{ formatDate(cred.createdAt) }}<template v-if="cred.lastUsedAt">
                · 最后使用 {{ formatDate(cred.lastUsedAt) }}
              </template>
            </div>
          </div>
          <NInputGroup style="margin-top: 8px">
            <NInput :value="cred.appKey" readonly size="small" />
            <NButton size="small" @click="copyToClipboard(cred.appKey).then(() => message.success('已复制'))">
              <template #icon>
                <NIcon><Icon icon="lucide:copy" /></NIcon>
              </template>
            </NButton>
          </NInputGroup>
          <NSpace :size="4" style="margin-left: auto; margin-top: 8px">
            <NTooltip>
              <template #trigger>
                <NButton size="tiny" quaternary @click="handleRotateSecret(cred.appId)">
                  <template #icon>
                    <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
                  </template>
                </NButton>
              </template>滚动密钥
            </NTooltip>
            <NTooltip>
              <template #trigger>
                <NButton size="tiny" quaternary type="error" @click="handleDeleteCredential(cred.appId)">
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                </NButton>
              </template>删除凭证
            </NTooltip>
          </NSpace>
        </div>
      </div>
      <template v-if="newSecret">
        <NDivider />
        <NAlert type="warning" title="请立即保存 API Secret" :bordered="false">
          <p style="margin-bottom: 8px; font-size: 13px">
            此密钥仅显示一次：
          </p>
          <NInputGroup>
            <NInput :value="newSecret" readonly type="password" show-password-on="click" />
            <NButton @click="copyToClipboard(newSecret).then(() => message.success('已复制'))">
              <template #icon>
                <NIcon><Icon icon="lucide:copy" /></NIcon>
              </template>
            </NButton>
          </NInputGroup>
        </NAlert>
      </template>
    </NCard>

    <NGrid cols="1 m:2" responsive="screen" :x-gap="12" :y-gap="12">
      <NGridItem>
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:file-signature" width="16" /><span>签名算法</span>
            </div>
          </template>
          <span class="pf-hint" style="display: block; margin-bottom: 8px">修改后需同步更新客户端配置</span>
          <NSelect v-model:value="signAlgorithm" :options="signAlgorithmOptions" />
        </NCard>
      </NGridItem>
      <NGridItem>
        <NCard :bordered="false" size="small" class="pf-card">
          <template #header>
            <div class="pf-card-header">
              <Icon icon="lucide:shield-ban" width="16" /><span>IP 白名单</span>
            </div>
          </template>
          <span class="pf-hint" style="display: block; margin-bottom: 8px">每行一个 IP，留空则不限制</span>
          <NInput v-model:value="ipWhitelist" type="textarea" placeholder="192.168.1.1&#10;10.0.0.0/24" :autosize="{ minRows: 3, maxRows: 6 }" />
          <div style="display: flex; justify-content: flex-end; margin-top: 8px">
            <NButton type="primary" size="small" @click="handleSaveIpWhitelist">
              保存
            </NButton>
          </div>
        </NCard>
      </NGridItem>
    </NGrid>
  </div>
</template>

<style src="./profile-shared.css" />

<script lang="ts" setup>
import type { DropdownOption } from 'naive-ui'
import type { ExternalLoginItem, OAuthProviderItem } from '~/types'
import {
  NButton,
  NDropdown,
  NEmpty,
  NPopconfirm,
  NSpace,
  NSpin,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabBinding' })

const { apis } = useAppContext()
const message = useMessage()

// ==================== 第三方账号 ====================

const linkedAccounts = ref<ExternalLoginItem[]>([])
const linkedLoading = ref(false)
const linkedLoaded = ref(false)
const oauthProviders = ref<OAuthProviderItem[]>([])

const providerOptions = computed<DropdownOption[]>(() =>
  oauthProviders.value.map(item => ({ key: item.name, label: item.displayName })),
)

async function loadLinkedAccounts() {
  linkedLoading.value = true
  try {
    linkedAccounts.value = await apis.getLinkedAccountsApi()
    linkedLoaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载失败')
  }
  finally {
    linkedLoading.value = false
  }
}

async function loadProviders() {
  try {
    const config = await apis.getLoginConfigApi()
    oauthProviders.value = config.oAuthProviders ?? []
  }
  catch {
    oauthProviders.value = []
  }
}

async function handleUnlinkAccount(provider: string) {
  try {
    await apis.unlinkAccountApi(provider)
    message.success('已解除绑定')
    await loadLinkedAccounts()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
  }
}

function providerIcon(name: string) {
  const map: Record<string, string> = {
    github: 'simple-icons:github',
    google: 'simple-icons:google',
    microsoft: 'simple-icons:microsoft',
    qq: 'simple-icons:tencentqq',
    wechat: 'simple-icons:wechat',
    weibo: 'simple-icons:sinaweibo',
  }
  return map[name.toLowerCase()] || 'lucide:link'
}

async function handleStartBind(provider: string) {
  try {
    // 浏览器跳转不带 JWT 头，先换取一次性票据以携带当前用户身份
    const ticket = await apis.createOAuthBindTicketApi()
    const baseUrl = import.meta.env.VITE_API_BASE_URL || ''
    const apiPrefix = import.meta.env.VITE_API_PREFIX || '/api'
    window.location.href = `${baseUrl}${apiPrefix}/OAuth/ExternalLogin?provider=${encodeURIComponent(provider)}&bindTicket=${encodeURIComponent(ticket)}`
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '发起绑定失败')
  }
}

function handleProviderSelect(key: string) {
  void handleStartBind(key)
}

onMounted(() => {
  loadLinkedAccounts()
  loadProviders()
})
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:link" width="16" />
            <span>关联第三方账号</span>
          </div>
          <div class="pf-section__desc">
            绑定第三方账号后，可使用其快速登录
          </div>
        </div>
        <div class="pf-section__extra">
          <NSpace :size="8">
            <NButton size="tiny" quaternary @click="loadLinkedAccounts">
              <template #icon>
                <Icon icon="lucide:refresh-cw" />
              </template>
            </NButton>
            <NDropdown
              v-if="providerOptions.length > 0"
              trigger="click"
              :options="providerOptions"
              @select="handleProviderSelect"
            >
              <NButton size="tiny">
                <template #icon>
                  <Icon icon="lucide:plus" />
                </template>
                绑定新账号
              </NButton>
            </NDropdown>
            <NButton v-else size="tiny" disabled>
              <template #icon>
                <Icon icon="lucide:plus" />
              </template>
              暂无可绑定渠道
            </NButton>
          </NSpace>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="linkedLoading">
          <NEmpty v-if="linkedAccounts.length === 0 && linkedLoaded" description="暂无绑定的第三方账号" />
          <div v-else class="pf-list">
            <div v-for="item in linkedAccounts" :key="item.provider" class="pf-list-item">
              <div class="pf-list-icon pf-list-icon--active">
                <Icon :icon="providerIcon(item.provider)" width="18" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ item.providerDisplayName || item.provider }}
                </div>
                <div class="pf-list-desc">
                  {{ item.email || '未关联邮箱' }}
                  <template v-if="item.lastLoginTime">
                    · 最后登录 {{ formatDate(item.lastLoginTime) }}
                  </template>
                </div>
              </div>
              <NPopconfirm @positive-click="handleUnlinkAccount(item.provider)">
                <template #trigger>
                  <NButton size="tiny" type="warning" text>
                    解除绑定
                  </NButton>
                </template>
                确定解除与 {{ item.providerDisplayName || item.provider }} 的绑定？
              </NPopconfirm>
            </div>
          </div>
        </NSpin>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />

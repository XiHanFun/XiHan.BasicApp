<script lang="ts" setup>
import type { ExternalLoginItem, OAuthProviderItem } from '~/types'
import {
  NButton,
  NEmpty,
  NPopconfirm,
  NSpin,
  useMessage,
} from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabBinding' })

const { apis } = useAppContext()
const message = useMessage()
const { t } = useI18n()

// ==================== 第三方账号 ====================

const linkedAccounts = ref<ExternalLoginItem[]>([])
const oauthProviders = ref<OAuthProviderItem[]>([])
const loading = ref(false)
const loaded = ref(false)
// 头像加载失败的渠道（回退到品牌图标）
const avatarErrors = ref<Set<string>>(new Set())

// 支持的渠道全部罗列（与登录页一致，支持什么显示什么）；每个渠道合并其绑定状态：
// 已绑定 → 展示账号信息 + 解除绑定；未绑定 → 展示绑定按钮。
const providers = computed(() =>
  oauthProviders.value.map((provider) => {
    const linked = linkedAccounts.value.find(
      item => item.provider.toLowerCase() === provider.name.toLowerCase(),
    )
    return {
      name: provider.name,
      displayName: provider.displayName,
      linked,
    }
  }),
)

async function loadData() {
  loading.value = true
  try {
    const [accounts, config] = await Promise.all([
      apis.getLinkedAccountsApi(),
      apis.getLoginConfigApi(),
    ])
    linkedAccounts.value = accounts
    oauthProviders.value = config.oAuthProviders ?? []
    avatarErrors.value = new Set()
    loaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.binding.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

async function handleUnlinkAccount(provider: string) {
  try {
    await apis.unlinkAccountApi(provider)
    message.success(t('component.profile.binding.msg_unbound'))
    await loadData()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.binding.err_operation_failed'))
  }
}

// 品牌图标用离线已预加载的图标集（offline.ts 预加载 lucide/tabler/mdi/simple-icons）：mdi/simple-icons 品牌图标，lucide 兜底
function providerIcon(name: string) {
  const map: Record<string, string> = {
    github: 'mdi:github',
    gitee: 'simple-icons:gitee',
    google: 'mdi:google',
    microsoft: 'mdi:microsoft',
    qq: 'mdi:qqchat',
    wechat: 'mdi:wechat',
    weibo: 'mdi:sina-weibo',
  }
  return map[name.toLowerCase()] || 'lucide:link-2'
}

function handleAvatarError(name: string) {
  const next = new Set(avatarErrors.value)
  next.add(name)
  avatarErrors.value = next
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
    message.error((e as Error)?.message || t('component.profile.binding.err_bind_start_failed'))
  }
}

onMounted(() => {
  loadData()
})
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:link" width="16" />
            <span>{{ t('component.profile.binding.section_title') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.binding.section_desc') }}
          </div>
        </div>
        <div class="pf-section__extra">
          <NButton size="tiny" quaternary @click="loadData">
            <template #icon>
              <Icon icon="lucide:refresh-cw" />
            </template>
          </NButton>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="loading">
          <NEmpty v-if="providers.length === 0 && loaded" :description="t('component.profile.binding.empty')" />
          <div v-else class="pf-list">
            <div v-for="provider in providers" :key="provider.name" class="pf-list-item">
              <div class="pf-list-icon" :class="{ 'pf-list-icon--active': provider.linked }">
                <img
                  v-if="provider.linked?.avatarUrl && !avatarErrors.has(provider.name)"
                  :src="provider.linked.avatarUrl"
                  alt=""
                  class="pf-list-avatar"
                  referrerpolicy="no-referrer"
                  @error="handleAvatarError(provider.name)"
                >
                <Icon v-else :icon="providerIcon(provider.name)" width="18" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ provider.displayName }}
                </div>
                <div class="pf-list-desc">
                  <template v-if="provider.linked">
                    {{ provider.linked.email || t('component.profile.binding.no_email') }}
                    <template v-if="provider.linked.lastLoginTime">
                      · {{ t('component.profile.binding.last_login', { time: formatDate(provider.linked.lastLoginTime) }) }}
                    </template>
                  </template>
                  <template v-else>
                    {{ t('component.profile.binding.unbound') }}
                  </template>
                </div>
              </div>
              <NPopconfirm v-if="provider.linked" @positive-click="handleUnlinkAccount(provider.name)">
                <template #trigger>
                  <NButton size="tiny" type="warning" text>
                    {{ t('component.profile.binding.btn_unbind') }}
                  </NButton>
                </template>
                {{ t('component.profile.binding.confirm_unbind', { name: provider.displayName }) }}
              </NPopconfirm>
              <NButton v-else size="tiny" type="primary" text @click="handleStartBind(provider.name)">
                {{ t('component.profile.binding.btn_bind') }}
              </NButton>
            </div>
          </div>
        </NSpin>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-list-avatar {
  width: 100%;
  height: 100%;
  border-radius: inherit;
  object-fit: cover;
}
</style>

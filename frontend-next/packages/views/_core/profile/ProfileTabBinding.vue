<script lang="ts" setup>
import type { ExternalLoginItem, OAuthProviderItem } from '~/types'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhPopconfirmCancelTrigger, XhPopconfirmConfirmTrigger, XhPopconfirmContent, XhPopconfirmPositioner, XhPopconfirmRoot, XhPopconfirmTitle, XhPopconfirmTrigger, XhSpinner } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAppContext, useAuthStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabBinding' })

const { apis } = useAppContext()
const authStore = useAuthStore()
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
// 绑定记的是规范类型、登录配置给的是名称（可能是别名或自定义名），按登录配置带的类型对齐；
// 对不上任何已配置渠道的绑定（渠道已下线）也列出来，只能解除绑定
const providers = computed(() => {
  const rows = oauthProviders.value.map((provider) => {
    const linked = linkedAccounts.value.find(
      item => item.provider.toLowerCase() === provider.type,
    )
    return {
      name: provider.name,
      displayName: provider.displayName,
      type: provider.type,
      linked,
    }
  })
  for (const linked of linkedAccounts.value) {
    if (!rows.some(row => row.linked === linked)) {
      rows.push({ name: linked.provider, displayName: linked.provider, type: linked.provider, linked })
    }
  }
  return rows
})

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余取 message */
function messageOf(error: unknown): string | undefined {
  return error instanceof ApiError ? error.detail : (error as Error | undefined)?.message
}

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
    toast.danger(messageOf(e) || t('component.profile.binding.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 解绑按绑定记录的规范类型（不是登录配置的名称） */
async function handleUnlinkAccount(provider: string) {
  try {
    await apis.unlinkAccountApi(provider)
    toast.success(t('component.profile.binding.msg_unbound'))
    await loadData()
  }
  catch (e: unknown) {
    toast.danger(messageOf(e) || t('component.profile.binding.err_operation_failed'))
  }
}

// 品牌图标用离线已预加载的图标集（offline.ts 预加载 lucide/tabler/mdi/simple-icons）：mdi/simple-icons 品牌图标，lucide 兜底。
// 企业微信、飞书在这四个集里没有品牌 logo，用语义相近的通用图标占位（与登录页保持一致）。
function providerIcon(type: string) {
  const map: Record<string, string> = {
    github: 'mdi:github',
    gitee: 'simple-icons:gitee',
    google: 'mdi:google',
    microsoft: 'mdi:microsoft',
    qq: 'mdi:qqchat',
    wechat: 'mdi:wechat',
    weibo: 'mdi:sina-weibo',
    dingtalk: 'tabler:brand-dingtalk',
    wecom: 'mdi:briefcase-account',
    lark: 'lucide:send',
  }
  return map[type] || 'lucide:link-2'
}

function handleAvatarError(name: string) {
  const next = new Set(avatarErrors.value)
  next.add(name)
  avatarErrors.value = next
}

/** 发起绑定（4.x 同为先换一次性凭据再整页跳去第三方）：领本人的绑定意图后跳去后端给的地址，回到回调页由本人确认 */
async function handleStartBind(provider: string) {
  try {
    await authStore.startOAuthBind(provider)
  }
  catch (e: unknown) {
    toast.danger(messageOf(e) || t('component.profile.binding.err_bind_start_failed'))
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
          <XhButton size="sm" variant="ghost" icon-only :aria-label="t('common.actions.refresh')" @click="loadData">
            <Icon icon="lucide:refresh-cw" />
          </XhButton>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="providers.length === 0 && loaded">
            <XhEmptyStateIndicator>
              <Icon icon="lucide:inbox" width="28" height="28" />
            </XhEmptyStateIndicator>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('component.profile.binding.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
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
                <Icon v-else :icon="providerIcon(provider.type)" width="18" />
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
              <XhPopconfirmRoot v-if="provider.linked" @confirm="handleUnlinkAccount(provider.linked.provider)">
                <!-- 触发器本身就是那颗按钮：浮层触发器渲染成 button，不能再往里套一颗 -->
                <XhPopconfirmTrigger class="xh-linklike-trigger">
                  {{ t('component.profile.binding.btn_unbind') }}
                </XhPopconfirmTrigger>
                <XhPopconfirmPositioner>
                  <XhPopconfirmContent>
                    <XhPopconfirmTitle>
                      {{ t('component.profile.binding.confirm_unbind', { name: provider.displayName }) }}
                    </XhPopconfirmTitle>
                    <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                    <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                  </XhPopconfirmContent>
                </XhPopconfirmPositioner>
              </XhPopconfirmRoot>
              <XhButton v-else variant="subtle" size="sm" tone="brand" @click="handleStartBind(provider.name)">
                {{ t('component.profile.binding.btn_bind') }}
              </XhButton>
            </div>
          </div>
        </div>
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

<script lang="ts" setup>
import type { AppTenantSwitcherItem } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhEmptyStateAction, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XUserAvatar } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext, useAppStore, useAuthStore, useUserStore } from '~/stores'

defineOptions({ name: 'ControlCenter' })

const { t } = useI18n()
const { apis } = useAppContext()
const appStore = useAppStore()
const authStore = useAuthStore()
const userStore = useUserStore()

const loading = ref(false)
const loaded = ref(false)
const switching = ref(false)
const tenants = ref<AppTenantSwitcherItem[]>([])

/** 是否可进入平台（平台账号），决定是否展示平台分组 */
const canAccessPlatform = computed(() => userStore.userInfo?.canAccessPlatform ?? false)
/** 平台是否为当前上下文（切换上下文会整页重建，会话里的租户就是当前） */
const platformIsCurrent = computed(() => userStore.userInfo?.isPlatform ?? false)
const displayName = computed(() => userStore.nickname || userStore.username)
const avatar = computed(() => userStore.avatar)
// 与侧栏品牌同一套回退：偏好里的品牌 → 构建期 VITE_APP_TITLE / VITE_APP_LOGO → 缺省
const brandTitle = computed(() => appStore.brandTitle || import.meta.env.VITE_APP_TITLE || 'XiHan.BasicApp')
const brandLogo = computed(() => appStore.brandLogo || import.meta.env.VITE_APP_LOGO || '/favicon.png')

async function loadTenants() {
  loading.value = true
  try {
    tenants.value = await apis.tenantApi.myAvailableTenants()
    loaded.value = true
  }
  catch (e: unknown) {
    toast.danger((e as Error)?.message || t('page.control_center.load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 进入租户：后端在目标租户续接会话，本地按新上下文整页重建（允许再次进入当前租户以刷新） */
async function enterTenant(tenant: AppTenantSwitcherItem) {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    await authStore.switchTenant(tenant.tenantId)
  }
  catch (e: unknown) {
    toast.danger((e as Error)?.message || t('page.control_center.switch_failed'))
    switching.value = false
  }
}

/** 进入平台（仅平台账号）：与进入租户同构 */
async function enterPlatform() {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    await authStore.switchTenant(null)
  }
  catch (e: unknown) {
    toast.danger((e as Error)?.message || t('page.control_center.switch_failed'))
    switching.value = false
  }
}

async function handleLogout() {
  await authStore.logout()
}

/**
 * 身份标签：所有者（租户的 AdminUserId，先于注册地判定）、注册地、运维支持、普通成员
 * （文案键静态写出以便 i18n 门禁核对；与个人中心「我的租户」同口径）
 */
function membershipLabel(tenant: AppTenantSwitcherItem): string {
  if (tenant.isOwner) {
    return t('page.control_center.member_type.owner')
  }
  if (tenant.isHome) {
    return t('page.control_center.home_tenant')
  }
  return tenant.memberType === 'Support'
    ? t('page.control_center.member_type.support')
    : t('page.control_center.member_type.member')
}

function membershipTone(tenant: AppTenantSwitcherItem) {
  if (tenant.isOwner) {
    return 'warning'
  }
  if (tenant.isHome) {
    return 'brand'
  }
  return tenant.memberType === 'Support'
    ? 'info'
    : 'neutral'
}

onMounted(loadTenants)
</script>

<template>
  <div class="cc-page">
    <header class="cc-topbar">
      <div class="cc-brand">
        <img v-if="brandLogo" :src="brandLogo" alt="" class="cc-brand__logo">
        <span class="cc-brand__title">{{ brandTitle }}</span>
      </div>
      <div class="cc-user">
        <XhButton size="sm" variant="ghost" @click="handleLogout">
          <Icon icon="lucide:log-out" />
          {{ t('header.user.logout') }}
        </XhButton>
      </div>
    </header>

    <main class="cc-main">
      <div class="cc-container">
        <div class="cc-header">
          <XUserAvatar :avatar="avatar" :name="displayName" :size="64" class="cc-avatar" />
          <h1 class="cc-title">
            {{ t('page.control_center.welcome', { name: displayName }) }}
          </h1>
          <p class="cc-subtitle">
            {{ t('page.control_center.subtitle') }}
          </p>
        </div>

        <section class="cc-card">
          <div class="cc-card__head">
            <div class="cc-card__title">
              <Icon icon="lucide:layout-grid" width="16" />
              <span>{{ t('page.control_center.title') }}</span>
            </div>
            <XhButton size="sm" variant="ghost" :loading="loading" @click="loadTenants">
              <XhButtonIndicator />
              <Icon icon="lucide:refresh-cw" />
              <XhButtonLabel>{{ t('page.control_center.refresh') }}</XhButtonLabel>
            </XhButton>
          </div>
          <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
            <div class="xh-loading-stage__veil">
              <XhSpinner />
            </div>
            <!-- 平台分组：与租户同构的可选中项，选中态表示当前处于平台运维态 -->
            <template v-if="canAccessPlatform">
              <div class="cc-group-label">
                {{ t('page.control_center.platform_group') }}
              </div>
              <div class="cc-tenant-grid">
                <button
                  type="button"
                  class="cc-tenant"
                  :class="{ 'cc-tenant--current': platformIsCurrent }"
                  :disabled="switching"
                  @click="enterPlatform"
                >
                  <div class="cc-tenant__logo cc-tenant__logo--platform">
                    <Icon icon="lucide:shield-check" width="20" />
                  </div>
                  <div class="cc-tenant__body">
                    <div class="cc-tenant__name">
                      {{ t('page.control_center.platform_panel') }}
                      <XhTagRoot v-if="platformIsCurrent" variant="subtle" tone="success" size="sm">
                        <XhTagLabel>
                          {{ t('page.control_center.current') }}
                        </XhTagLabel>
                      </XhTagRoot>
                    </div>
                    <div class="cc-tenant__meta">
                      <span class="cc-tenant__code">{{ t('page.control_center.platform_desc') }}</span>
                    </div>
                  </div>
                  <Icon v-if="!platformIsCurrent" icon="lucide:arrow-right" width="16" class="cc-tenant__arrow" />
                </button>
              </div>
            </template>

            <!-- 我的租户分组：注册地租户 + 成员租户 -->
            <div class="cc-group-label" :class="{ 'cc-group-label--spaced': canAccessPlatform }">
              {{ t('page.control_center.my_tenants') }}
            </div>
            <XhEmptyStateRoot v-if="tenants.length === 0 && loaded" class="cc-empty">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:inbox" width="28" height="28" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('page.control_center.no_tenants') }}</XhEmptyStateDescription>
              <XhEmptyStateAction>
                <span class="cc-empty__hint">{{ t('page.control_center.no_tenants_hint') }}</span>
              </XhEmptyStateAction>
            </XhEmptyStateRoot>
            <div v-else class="cc-tenant-grid">
              <button
                v-for="tenant in tenants"
                :key="tenant.tenantId"
                type="button"
                class="cc-tenant"
                :class="{ 'cc-tenant--current': tenant.isCurrent }"
                :disabled="switching"
                @click="enterTenant(tenant)"
              >
                <div class="cc-tenant__logo">
                  <XUserAvatar :name="tenant.tenantName" :size="40" :round="false" />
                </div>
                <div class="cc-tenant__body">
                  <div class="cc-tenant__name">
                    {{ tenant.tenantName }}
                    <XhTagRoot v-if="tenant.isCurrent" variant="subtle" tone="success" size="sm">
                      <XhTagLabel>
                        {{ t('page.control_center.current') }}
                      </XhTagLabel>
                    </XhTagRoot>
                  </div>
                  <div class="cc-tenant__meta">
                    <XhTagRoot variant="subtle" :tone="membershipTone(tenant)" size="sm">
                      <XhTagLabel>
                        {{ membershipLabel(tenant) }}
                      </XhTagLabel>
                    </XhTagRoot>
                    <span class="cc-tenant__code">{{ tenant.tenantId }}</span>
                  </div>
                </div>
                <Icon v-if="!tenant.isCurrent" icon="lucide:arrow-right" width="16" class="cc-tenant__arrow" />
              </button>
            </div>
          </div>
        </section>
      </div>
    </main>
  </div>
</template>

<style scoped>
/* 全部取色走设计系统令牌（packages/design/variables.css，.dark 自动切换），禁止硬编码颜色 */
.cc-page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  color: var(--xh-fg-default);
  background:
    radial-gradient(1200px 500px at 80% -10%, color-mix(in srgb, var(--xh-fg-brand) 10%, transparent), transparent 60%),
    radial-gradient(900px 420px at -10% 110%, color-mix(in srgb, var(--xh-fg-brand) 6%, transparent), transparent 55%),
    var(--xh-bg-surface);
}

.cc-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--xh-space-3) var(--xh-space-7);
}

.cc-brand {
  display: flex;
  gap: var(--xh-space-2_5);
  align-items: center;
}

/* 与其他页面侧边栏品牌（AppBrand）保持一致：32px、圆角 6px、object-fit cover、标题 600 */
.cc-brand__logo {
  width: 32px;
  height: 32px;
  flex-shrink: 0;
  border-radius: var(--xh-shape-control);
  object-fit: cover;
}

.cc-brand__title {
  font-size: var(--xh-font-size-lg);
  font-weight: 600;
  color: var(--xh-fg-default);
}

.cc-user {
  display: flex;
  gap: var(--xh-space-2_5);
  align-items: center;
}

.cc-main {
  display: flex;
  flex: 1;
  justify-content: center;
  padding: var(--xh-space-8) var(--xh-space-6) calc(var(--xh-space-8) * 2);
}

.cc-container {
  width: 100%;
  max-width: 720px;
}

.cc-header {
  margin-bottom: 24px;
  text-align: center;
}

.cc-avatar {
  margin-bottom: 12px;
  box-shadow: 0 4px 16px color-mix(in srgb, var(--xh-fg-brand) 18%, transparent);
}

.cc-title {
  margin: 0 0 6px;
  font-size: var(--xh-font-size-2xl);
  font-weight: 700;
  color: var(--xh-fg-default);
}

.cc-subtitle {
  margin: 0;
  font-size: var(--xh-font-size-md);
  color: var(--xh-fg-muted);
}

.cc-card {
  padding: var(--xh-space-5);
  margin-bottom: 16px;
  color: var(--xh-fg-default);
  background: color-mix(in srgb, var(--xh-bg-surface) 88%, transparent);
  backdrop-filter: blur(8px);
  border: 1px solid var(--xh-border-default);
  border-radius: calc(var(--xh-shape-control) * 2);
  box-shadow: 0 8px 28px color-mix(in srgb, var(--xh-fg-default) 5%, transparent);
}

.cc-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.cc-card__title {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  font-size: var(--xh-font-size-md);
  font-weight: 600;
  color: var(--xh-fg-default);
}

.cc-group-label {
  margin-bottom: 8px;
  font-size: var(--xh-font-size-xs);
  font-weight: 600;
  color: var(--xh-fg-muted);
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.cc-group-label--spaced {
  margin-top: 18px;
}

.cc-tenant__logo--platform {
  color: var(--xh-fg-brand);
  background: color-mix(in srgb, var(--xh-fg-brand) 12%, transparent);
}

.cc-tenant-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: var(--xh-space-2_5);
}

.cc-tenant {
  display: flex;
  gap: var(--xh-space-3);
  align-items: center;
  width: 100%;
  padding: var(--xh-space-3);
  color: var(--xh-fg-default);
  text-align: left;
  cursor: pointer;
  background: transparent;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-sheet);
  transition:
    border-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-nudge) var(--xh-motion-ease-enter);
}

.cc-tenant:hover:not(:disabled) {
  background: var(--xh-bg-subtle-hover);
  border-color: var(--xh-fg-brand);
  transform: translateY(-1px);
}

.cc-tenant:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.cc-tenant--current {
  border-color: var(--xh-fg-success);
}

.cc-tenant__logo {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  overflow: hidden;
  font-size: var(--xh-font-size-md);
  font-weight: 600;
  color: var(--xh-fg-brand);
  background: color-mix(in srgb, var(--xh-fg-brand) 12%, transparent);
  border-radius: var(--xh-shape-sheet);
}

.cc-tenant__body {
  flex: 1;
  min-width: 0;
}

.cc-tenant__name {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  font-size: var(--xh-font-size-md);
  font-weight: 600;
  color: var(--xh-fg-default);
}

.cc-tenant__meta {
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  margin-top: 4px;
}

.cc-tenant__code {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

.cc-tenant__arrow {
  flex-shrink: 0;
  color: var(--xh-fg-muted);
}

.cc-empty {
  padding: var(--xh-space-6) 0;
}

.cc-empty__hint {
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
}

/* 小屏适配：收紧间距 */
@media (max-width: 480px) {
  .cc-topbar {
    padding: var(--xh-space-2_5) var(--xh-space-4);
  }

  .cc-brand__title {
    font-size: var(--xh-font-size-md);
  }

  .cc-main {
    padding: var(--xh-space-5) var(--xh-space-4) calc(var(--xh-space-6) * 2);
  }

  .cc-title {
    font-size: var(--xh-font-size-xl);
  }

  .cc-subtitle {
    font-size: var(--xh-font-size-sm);
  }

  .cc-card {
    padding: var(--xh-space-4);
  }
}
</style>

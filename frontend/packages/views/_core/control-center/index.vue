<script lang="ts" setup>
import type { TenantSwitcherDto } from '@/api'
import { NAvatar, NButton, NEmpty, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { tenantApi, TenantMemberType } from '@/api'
import { XUserAvatar } from '~/components'
import { MEMBER_TYPE_OPTIONS } from '~/constants'
import { useEnumOptions } from '~/hooks'
import { Icon } from '~/iconify'
import { useAccessStore, useAppStore, useAuthStore, useUserStore } from '~/stores'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'ControlCenter' })

const { t } = useI18n()
const message = useMessage()
const accessStore = useAccessStore()
const appStore = useAppStore()
const authStore = useAuthStore()
const userStore = useUserStore()

// 成员类型走后端枚举元数据（本地化、切语言响应式重取），未加载/未部署时回退静态 MEMBER_TYPE_OPTIONS
const memberTypeOptions = useEnumOptions('TenantMemberType', MEMBER_TYPE_OPTIONS)
function memberTypeLabel(value: TenantSwitcherDto['memberType']) {
  return getOptionLabel(memberTypeOptions.value, value)
}

const loading = ref(false)
const loaded = ref(false)
const switching = ref(false)
const tenants = ref<TenantSwitcherDto[]>([])

/** 是否可进入平台管理（超管 / 平台管理员）——角色派生，稳定，仅用于是否展示平台分组 */
const canAccessPlatform = computed(() => userStore.userInfo?.canAccessPlatform ?? false)
/**
 * 当前上下文以租户列表的 isCurrent 为唯一事实源：后端按令牌 tenantid 实时计算，始终新鲜。
 * 不用 userInfo.isPlatform（整页重载后可能陈旧，会导致切换后前端当前态不同步）。
 */
const hasCurrentTenant = computed(() => tenants.value.some(item => item.isCurrent))
/** 平台管理是否为当前上下文：已加载且没有任何租户处于当前态 */
const platformIsCurrent = computed(() => loaded.value && !hasCurrentTenant.value)
const displayName = computed(() => userStore.nickname || userStore.username)
const avatar = computed(() => userStore.avatar)
const brandTitle = computed(() => appStore.brandTitle)
const brandLogo = computed(() => appStore.brandLogo)

async function loadTenants() {
  loading.value = true
  try {
    tenants.value = await tenantApi.myAvailableTenants()
    loaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('page.control_center.load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 进入租户：重签发令牌后整页重载，让新上下文的权限/菜单重新引导（允许再次进入当前租户） */
async function enterTenant(tenant: TenantSwitcherDto) {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    const token = await tenantApi.switchTenant({ tenantId: String(tenant.tenantId) })
    accessStore.setAccessToken(token.accessToken)
    accessStore.setRefreshToken(token.refreshToken)
    message.success(t('page.control_center.switched', { name: tenant.tenantName }))
    window.location.href = import.meta.env.VITE_ROUTER_HISTORY === 'history' ? '/' : './'
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('page.control_center.switch_failed'))
    switching.value = false
  }
}

/** 进入平台管理：与进入租户同构——重签发平台态令牌（无 TenantId）后整页重载（允许在平台态再次进入以刷新） */
async function enterPlatform() {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    // tenantId 传 null → 后端归一为平台运维态（无租户上下文）
    const token = await tenantApi.switchTenant({ tenantId: null })
    accessStore.setAccessToken(token.accessToken)
    accessStore.setRefreshToken(token.refreshToken)
    message.success(t('page.control_center.switched_platform'))
    window.location.href = import.meta.env.VITE_ROUTER_HISTORY === 'history' ? '/' : './'
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('page.control_center.switch_failed'))
    switching.value = false
  }
}

async function handleLogout() {
  await authStore.logout()
}

function memberTagType(type: TenantMemberType) {
  if (type === TenantMemberType.Owner) {
    return 'warning'
  }
  if (type === TenantMemberType.Admin || type === TenantMemberType.PlatformAdmin) {
    return 'primary'
  }
  return 'default'
}

function tenantInitial(tenant: TenantSwitcherDto): string {
  const name = tenant.tenantShortName || tenant.tenantName || ''
  return name.trim().charAt(0) || '租'
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
        <NButton size="small" quaternary @click="handleLogout">
          <template #icon>
            <Icon icon="lucide:log-out" />
          </template>
          {{ t('header.user.logout') }}
        </NButton>
      </div>
    </header>

    <main class="cc-main">
      <div class="cc-container">
        <div class="cc-header">
          <XUserAvatar :avatar="avatar" :name="displayName" :size="64" class="cc-avatar" />
          <h1 class="cc-title">
            {{ t('page.control_center.welcome') }}，{{ displayName }}
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
            <NButton size="tiny" quaternary :loading="loading" @click="loadTenants">
              <template #icon>
                <Icon icon="lucide:refresh-cw" />
              </template>
              {{ t('page.control_center.refresh') }}
            </NButton>
          </div>
          <NSpin :show="loading">
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
                      <NTag v-if="platformIsCurrent" type="success" size="tiny" :bordered="false">
                        {{ t('page.control_center.current') }}
                      </NTag>
                    </div>
                    <div class="cc-tenant__meta">
                      <span class="cc-tenant__code">{{ t('page.control_center.platform_desc') }}</span>
                    </div>
                  </div>
                  <Icon v-if="!platformIsCurrent" icon="lucide:arrow-right" width="16" class="cc-tenant__arrow" />
                </button>
              </div>
            </template>

            <!-- 我的租户分组 -->
            <div class="cc-group-label" :class="{ 'cc-group-label--spaced': canAccessPlatform }">
              {{ t('page.control_center.my_tenants') }}
            </div>
            <NEmpty
              v-if="tenants.length === 0 && loaded"
              :description="t('page.control_center.no_tenants')"
              class="cc-empty"
            >
              <template #extra>
                <span class="cc-empty__hint">{{ t('page.control_center.no_tenants_hint') }}</span>
              </template>
            </NEmpty>
            <div v-else class="cc-tenant-grid">
              <button
                v-for="tenant in tenants"
                :key="tenant.membershipId"
                type="button"
                class="cc-tenant"
                :class="{ 'cc-tenant--current': tenant.isCurrent }"
                :disabled="switching"
                @click="enterTenant(tenant)"
              >
                <div class="cc-tenant__logo">
                  <NAvatar
                    v-if="tenant.logo"
                    :src="tenant.logo"
                    :size="40"
                    object-fit="cover"
                    class="cc-tenant__avatar"
                  />
                  <template v-else>
                    {{ tenantInitial(tenant) }}
                  </template>
                </div>
                <div class="cc-tenant__body">
                  <div class="cc-tenant__name">
                    {{ tenant.tenantName }}
                    <NTag v-if="tenant.isCurrent" type="success" size="tiny" :bordered="false">
                      {{ t('page.control_center.current') }}
                    </NTag>
                  </div>
                  <div class="cc-tenant__meta">
                    <NTag :type="memberTagType(tenant.memberType)" size="tiny" round :bordered="false">
                      {{ memberTypeLabel(tenant.memberType) }}
                    </NTag>
                    <span class="cc-tenant__code">{{ tenant.tenantCode }}</span>
                  </div>
                </div>
                <Icon v-if="!tenant.isCurrent" icon="lucide:arrow-right" width="16" class="cc-tenant__arrow" />
              </button>
            </div>
          </NSpin>
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
  color: hsl(var(--foreground));
  background:
    radial-gradient(1200px 500px at 80% -10%, hsl(var(--primary) / 10%), transparent 60%),
    radial-gradient(900px 420px at -10% 110%, hsl(var(--primary) / 6%), transparent 55%), hsl(var(--background));
}

.cc-topbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 28px;
}

.cc-brand {
  display: flex;
  gap: 10px;
  align-items: center;
}

.cc-brand__logo {
  width: 28px;
  height: 28px;
  border-radius: 6px;
}

.cc-brand__title {
  font-size: 16px;
  font-weight: 700;
  color: hsl(var(--foreground));
}

.cc-user {
  display: flex;
  gap: 10px;
  align-items: center;
}

.cc-main {
  display: flex;
  flex: 1;
  justify-content: center;
  padding: 32px 24px 64px;
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
  box-shadow: 0 4px 16px hsl(var(--primary) / 18%);
}

.cc-title {
  margin: 0 0 6px;
  font-size: 26px;
  font-weight: 700;
  color: hsl(var(--foreground));
}

.cc-subtitle {
  margin: 0;
  font-size: 14px;
  color: hsl(var(--muted-foreground));
}

.cc-card {
  padding: 20px;
  margin-bottom: 16px;
  color: hsl(var(--card-foreground));
  background: hsl(var(--card) / 88%);
  backdrop-filter: blur(8px);
  border: 1px solid hsl(var(--border));
  border-radius: 16px;
  box-shadow: 0 8px 28px hsl(var(--foreground) / 5%);
}

.cc-card__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.cc-card__title {
  display: flex;
  gap: 8px;
  align-items: center;
  font-size: 15px;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.cc-group-label {
  margin-bottom: 8px;
  font-size: 12px;
  font-weight: 600;
  color: hsl(var(--muted-foreground));
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.cc-group-label--spaced {
  margin-top: 18px;
}

.cc-tenant__logo--platform {
  color: hsl(var(--primary));
  background: hsl(var(--primary) / 12%);
}

.cc-tenant-grid {
  display: grid;
  grid-template-columns: 1fr;
  gap: 10px;
}

.cc-tenant {
  display: flex;
  gap: 12px;
  align-items: center;
  width: 100%;
  padding: 14px;
  color: hsl(var(--foreground));
  text-align: left;
  cursor: pointer;
  background: transparent;
  border: 1px solid hsl(var(--border));
  border-radius: 12px;
  transition:
    border-color 0.15s ease,
    background 0.15s ease,
    transform 0.15s ease;
}

.cc-tenant:hover:not(:disabled) {
  background: hsl(var(--accent));
  border-color: hsl(var(--primary));
  transform: translateY(-1px);
}

.cc-tenant:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.cc-tenant--current {
  border-color: hsl(var(--success));
}

.cc-tenant__logo {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  overflow: hidden;
  font-size: 15px;
  font-weight: 600;
  color: hsl(var(--primary));
  background: hsl(var(--primary) / 12%);
  border-radius: 10px;
}

.cc-tenant__avatar {
  background: transparent;
  border-radius: 10px;
}

.cc-tenant__body {
  flex: 1;
  min-width: 0;
}

.cc-tenant__name {
  display: flex;
  gap: 8px;
  align-items: center;
  font-size: 14px;
  font-weight: 600;
  color: hsl(var(--foreground));
}

.cc-tenant__meta {
  display: flex;
  gap: 8px;
  align-items: center;
  margin-top: 4px;
}

.cc-tenant__code {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
}

.cc-tenant__arrow {
  flex-shrink: 0;
  color: hsl(var(--muted-foreground));
}

.cc-empty {
  padding: 24px 0;
}

.cc-empty__hint {
  font-size: 12px;
  color: hsl(var(--muted-foreground));
}

/* 小屏适配：收紧间距，平台管理卡片纵向堆叠、按钮整行 */
@media (max-width: 480px) {
  .cc-topbar {
    padding: 10px 16px;
  }

  .cc-brand__title {
    font-size: 14px;
  }

  .cc-main {
    padding: 20px 16px 48px;
  }

  .cc-title {
    font-size: 20px;
  }

  .cc-subtitle {
    font-size: 13px;
  }

  .cc-card {
    padding: 16px;
  }
}
</style>

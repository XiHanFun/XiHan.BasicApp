<script lang="ts" setup>
import type { TenantSwitcherDto } from '@/api'
import { NAvatar, NButton, NEmpty, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { tenantApi, TenantMemberType } from '@/api'
import { MEMBER_TYPE_OPTIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useAccessStore, useUserStore } from '~/stores'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'ControlCenter' })

const { t } = useI18n()
const message = useMessage()
const accessStore = useAccessStore()
const userStore = useUserStore()

const loading = ref(false)
const loaded = ref(false)
const switching = ref(false)
const tenants = ref<TenantSwitcherDto[]>([])

/** 是否可进入平台管理（超管 / 平台管理员） */
const canAccessPlatform = computed(() => userStore.userInfo?.canAccessPlatform ?? false)
/** 当前是否处于平台态（未进入任何租户） */
const isPlatform = computed(() => userStore.userInfo?.isPlatform ?? false)
const displayName = computed(() => userStore.nickname || userStore.username)

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

/** 进入租户：重签发令牌后整页重载，让新上下文的权限/菜单重新引导 */
async function enterTenant(tenant: TenantSwitcherDto) {
  if (switching.value || tenant.isCurrent) {
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

/** 进入平台管理：平台态本身已具备平台菜单，直接回首页（首个可导航菜单） */
function enterPlatform() {
  window.location.href = import.meta.env.VITE_ROUTER_HISTORY === 'history' ? '/' : './'
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
    <div class="cc-container">
      <header class="cc-header">
        <h1 class="cc-title">
          {{ t('page.control_center.welcome') }}，{{ displayName }}
        </h1>
        <p class="cc-subtitle">
          {{ t('page.control_center.subtitle') }}
        </p>
      </header>

      <section v-if="canAccessPlatform && isPlatform" class="cc-card cc-platform">
        <div class="cc-platform__icon">
          <Icon icon="lucide:shield-check" width="22" />
        </div>
        <div class="cc-platform__body">
          <div class="cc-platform__title">
            {{ t('page.control_center.platform_panel') }}
          </div>
          <div class="cc-platform__desc">
            {{ t('page.control_center.platform_desc') }}
          </div>
        </div>
        <NButton type="primary" @click="enterPlatform">
          {{ t('page.control_center.enter_platform') }}
          <template #icon>
            <Icon icon="lucide:arrow-right" />
          </template>
        </NButton>
      </section>

      <section class="cc-card">
        <div class="cc-card__head">
          <div class="cc-card__title">
            <Icon icon="lucide:building-2" width="16" />
            <span>{{ t('page.control_center.my_tenants') }}</span>
          </div>
          <NButton size="tiny" quaternary :loading="loading" @click="loadTenants">
            <template #icon>
              <Icon icon="lucide:refresh-cw" />
            </template>
            {{ t('page.control_center.refresh') }}
          </NButton>
        </div>
        <NSpin :show="loading">
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
                    {{ getOptionLabel(MEMBER_TYPE_OPTIONS, tenant.memberType) }}
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
  </div>
</template>

<style scoped>
.cc-page {
  display: flex;
  justify-content: center;
  min-height: 100%;
  padding: 48px 24px;
}

.cc-container {
  width: 100%;
  max-width: 720px;
}

.cc-header {
  margin-bottom: 24px;
}

.cc-title {
  margin: 0 0 4px;
  font-size: 24px;
  font-weight: 600;
}

.cc-subtitle {
  margin: 0;
  font-size: 14px;
  color: var(--text-secondary, rgb(107 114 128));
}

.cc-card {
  padding: 20px;
  margin-bottom: 16px;
  background: var(--card-bg, rgb(255 255 255 / 60%));
  border: 1px solid var(--border-color, rgb(0 0 0 / 8%));
  border-radius: 14px;
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
}

.cc-platform {
  display: flex;
  gap: 14px;
  align-items: center;
}

.cc-platform__icon {
  display: flex;
  flex-shrink: 0;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  color: var(--primary-color, rgb(79 124 255));
  background: var(--primary-color-suppl, rgb(79 124 255 / 12%));
  border-radius: 12px;
}

.cc-platform__body {
  flex: 1;
  min-width: 0;
}

.cc-platform__title {
  font-size: 15px;
  font-weight: 600;
}

.cc-platform__desc {
  font-size: 13px;
  color: var(--text-secondary, rgb(107 114 128));
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
  text-align: left;
  cursor: pointer;
  background: transparent;
  border: 1px solid var(--border-color, rgb(0 0 0 / 8%));
  border-radius: 12px;
  transition: border-color 0.15s ease, background 0.15s ease, transform 0.15s ease;
}

.cc-tenant:hover:not(:disabled) {
  background: var(--hover-bg, rgb(0 0 0 / 3%));
  border-color: var(--primary-color, rgb(79 124 255));
  transform: translateY(-1px);
}

.cc-tenant:disabled {
  cursor: not-allowed;
  opacity: 0.6;
}

.cc-tenant--current {
  border-color: var(--success-color, rgb(34 197 94));
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
  background: var(--primary-color-suppl, rgb(79 124 255 / 12%));
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
}

.cc-tenant__meta {
  display: flex;
  gap: 8px;
  align-items: center;
  margin-top: 4px;
}

.cc-tenant__code {
  font-size: 12px;
  color: var(--text-secondary, rgb(107 114 128));
}

.cc-tenant__arrow {
  flex-shrink: 0;
  color: var(--text-secondary, rgb(107 114 128));
}

.cc-empty {
  padding: 24px 0;
}

.cc-empty__hint {
  font-size: 12px;
  color: var(--text-secondary, rgb(107 114 128));
}
</style>

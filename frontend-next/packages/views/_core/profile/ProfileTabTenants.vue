<script lang="ts" setup>
import type { AppTenantSwitcherItem } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XUserAvatar } from '~/components'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAppContext, useAuthStore } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabTenants' })

const { apis } = useAppContext()
const authStore = useAuthStore()
const { t } = useI18n()

/**
 * 身份标签：所有者（租户的 AdminUserId，先于注册地判定）、注册地（没有成员类型）、运维支持、普通成员。
 * 4.x 取后端 TenantMemberType 枚举元数据；6.0 没有枚举元数据，成员类型只有 Member / Support、所有者单独由 isOwner 给出，
 * 与控制中心同口径用本地文案（键静态写出以便 i18n 门禁核对）
 */
function memberTypeLabel(tenant: AppTenantSwitcherItem) {
  if (tenant.isOwner) {
    return t('component.profile.tenants.member_type.owner')
  }
  if (tenant.isHome) {
    return t('component.profile.tenants.home_tenant')
  }
  return tenant.memberType === 'Support' ? t('component.profile.tenants.member_type.support') : t('component.profile.tenants.member_type.member')
}

const loading = ref(false)
const loaded = ref(false)
const switching = ref(false)
const tenants = ref<AppTenantSwitcherItem[]>([])

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余取 message */
function messageOf(error: unknown): string | undefined {
  return error instanceof ApiError ? error.detail : (error as Error | undefined)?.message
}

async function loadTenants() {
  loading.value = true
  try {
    tenants.value = await apis.tenantApi.myAvailableTenants()
    loaded.value = true
  }
  catch (e: unknown) {
    toast.danger(messageOf(e) || t('component.profile.tenants.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 切换租户（与控制中心同一个入口）：服务端在目标租户续接会话，本地按新上下文整页重建 */
async function switchTo(tenantId: string) {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    await authStore.switchTenant(tenantId)
  }
  catch (e: unknown) {
    toast.danger(messageOf(e) || t('component.profile.tenants.err_switch_failed'))
    switching.value = false
  }
}

/** 身份 → 标签语气（所有者、注册地、运维支持各自高亮，与控制中心一致） */
function memberTagType(tenant: AppTenantSwitcherItem) {
  if (tenant.isOwner) {
    return 'warning'
  }
  if (tenant.isHome) {
    return 'brand'
  }
  if (tenant.memberType === 'Support') {
    return 'info'
  }
  return 'neutral'
}

onMounted(loadTenants)
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:building-2" width="16" />
            <span>{{ t('component.profile.tenants.section_title') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.tenants.section_desc') }}
          </div>
        </div>
        <div class="pf-section__extra">
          <XhButton size="sm" variant="ghost" icon-only :aria-label="t('common.actions.refresh')" @click="loadTenants">
            <Icon icon="lucide:refresh-cw" />
          </XhButton>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <div class="pf-list">
            <XhEmptyStateRoot v-if="tenants.length === 0 && loaded">
              <XhEmptyStateIndicator>
                <Icon icon="lucide:inbox" width="28" height="28" />
              </XhEmptyStateIndicator>
              <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
              <XhEmptyStateDescription>{{ t('component.profile.tenants.empty') }}</XhEmptyStateDescription>
            </XhEmptyStateRoot>
            <div
              v-for="tenant in tenants"
              :key="tenant.tenantId"
              class="pf-list-item"
              :class="{ 'pf-list-item--active': tenant.isCurrent }"
            >
              <div class="pf-list-icon pf-tenant-logo" :class="{ 'pf-list-icon--active': tenant.isCurrent }">
                <XUserAvatar
                  :name="tenant.tenantName"
                  :size="32"
                  :round="false"
                />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ tenant.tenantName }}
                  <XhTagRoot v-if="tenant.isCurrent" variant="subtle" tone="success" size="sm">
                    <XhTagLabel>
                      {{ t('component.profile.tenants.tag_current') }}
                    </XhTagLabel>
                  </XhTagRoot>
                </div>
                <div class="pf-list-desc">
                  {{ memberTypeLabel(tenant) }}
                  · {{ t('component.profile.tenants.joined_at', { time: formatDate(tenant.joinedTime, 'YYYY-MM-DD') }) }}
                </div>
              </div>
              <div class="pf-tenant-actions">
                <XhTagRoot variant="subtle" :tone="memberTagType(tenant)" size="sm">
                  <XhTagLabel>
                    {{ memberTypeLabel(tenant) }}
                  </XhTagLabel>
                </XhTagRoot>
                <XhButton
                  v-if="!tenant.isCurrent"
                  size="sm"
                  variant="subtle"
                  :loading="switching"
                  @click="switchTo(tenant.tenantId)"
                >
                  <XhButtonIndicator />
                  <XhButtonLabel>{{ t('component.profile.tenants.btn_switch') }}</XhButtonLabel>
                </XhButton>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />

<style scoped>
.pf-tenant-logo {
  overflow: hidden;
  font-size: 13px;
  font-weight: 600;
}

.pf-tenant-actions {
  display: flex;
  flex-shrink: 0;
  gap: 8px;
  align-items: center;
}
</style>

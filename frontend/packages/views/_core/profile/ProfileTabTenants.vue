<script lang="ts" setup>
import type { AppTenantSwitcherItem } from '~/types'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { XUserAvatar } from '~/components'
import { toast } from '~/composables'
import { MEMBER_TYPE_OPTIONS } from '~/constants'
import { useEnumOptions } from '~/hooks'
import { Icon } from '~/iconify'
import { useAppContext, useAuthStore } from '~/stores'
import { TenantMemberType } from '~/types/enums'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'ProfileTabTenants' })

const { apis } = useAppContext()
const authStore = useAuthStore()
const { t } = useI18n()

// 成员类型走后端枚举元数据（本地化、切语言响应式重取），未加载/未部署时回退静态 MEMBER_TYPE_OPTIONS
const memberTypeOptions = useEnumOptions('TenantMemberType', MEMBER_TYPE_OPTIONS)
function memberTypeLabel(value: AppTenantSwitcherItem['memberType']) {
  return getOptionLabel(memberTypeOptions.value, value)
}

const loading = ref(false)
const loaded = ref(false)
const switching = ref(false)
const tenants = ref<AppTenantSwitcherItem[]>([])

async function loadTenants() {
  loading.value = true
  try {
    tenants.value = await apis.tenantApi.myAvailableTenants()
    loaded.value = true
  }
  catch (e: unknown) {
    toast.danger((e as Error)?.message || t('component.profile.tenants.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 切换租户：服务端在目标租户续接会话，本地按新上下文整页重建 */
async function switchTo(tenantId: string) {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    await authStore.switchContext(tenantId)
  }
  catch (e: unknown) {
    toast.danger((e as Error)?.message || t('component.profile.tenants.err_switch_failed'))
    switching.value = false
  }
}

/** 成员类型 → NTag 类型（所有者/管理员高亮） */
function memberTagType(type: TenantMemberType) {
  if (type === TenantMemberType.Owner) {
    return 'warning'
  }
  if (type === TenantMemberType.Admin || type === TenantMemberType.PlatformAdmin) {
    return 'brand'
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
          <XhButton size="sm" variant="ghost" @click="loadTenants">
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
              :key="tenant.membershipId"
              class="pf-list-item"
              :class="{ 'pf-list-item--active': tenant.isCurrent }"
            >
              <div class="pf-list-icon pf-tenant-logo" :class="{ 'pf-list-icon--active': tenant.isCurrent }">
                <XUserAvatar
                  :avatar="tenant.logo"
                  :name="tenant.tenantShortName || tenant.tenantName"
                  :size="32"
                  :round="false"
                />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ tenant.tenantName }}
                  <span v-if="tenant.tenantShortName" class="pf-tenant-short">{{ tenant.tenantShortName }}</span>
                  <XhTagRoot v-if="tenant.isCurrent" variant="subtle" tone="success" size="sm">
                    <XhTagLabel>
                      {{ t('component.profile.tenants.tag_current') }}
                    </XhTagLabel>
                  </XhTagRoot>
                </div>
                <div class="pf-list-desc">
                  {{ memberTypeLabel(tenant.memberType) }}
                  <template v-if="tenant.joinedTime">
                    · {{ t('component.profile.tenants.joined_at', { time: formatDate(tenant.joinedTime, 'YYYY-MM-DD') }) }}
                  </template>
                  <template v-if="tenant.membershipExpirationTime">
                    · {{ t('component.profile.tenants.valid_until', { time: formatDate(tenant.membershipExpirationTime, 'YYYY-MM-DD') }) }}
                  </template>
                  <template v-else-if="tenant.domain">
                    · {{ tenant.domain }}
                  </template>
                </div>
              </div>
              <div class="pf-tenant-actions">
                <XhTagRoot variant="subtle" :tone="memberTagType(tenant.memberType)" size="sm">
                  <XhTagLabel>
                    {{ memberTypeLabel(tenant.memberType) }}
                  </XhTagLabel>
                </XhTagRoot>
                <XhButton
                  v-if="!tenant.isCurrent"
                  size="sm"
                  variant="subtle"
                  :loading="switching"
                  @click="switchTo(String(tenant.tenantId))"
                >
                  {{ t('component.profile.tenants.btn_switch') }}
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

.pf-tenant-short {
  font-size: 12px;
  font-weight: 400;
  color: var(--text-secondary);
}

.pf-tenant-actions {
  display: flex;
  flex-shrink: 0;
  gap: 8px;
  align-items: center;
}
</style>

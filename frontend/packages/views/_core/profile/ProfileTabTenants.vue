<script lang="ts" setup>
import type { TenantSwitcherDto } from '@/api'
import { NAvatar, NButton, NEmpty, NSpin, NTag, useMessage } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { tenantApi, TenantMemberType } from '@/api'
import { MEMBER_TYPE_OPTIONS } from '~/constants'
import { useEnumService } from '~/hooks'
import { Icon } from '~/iconify'
import { useAccessStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'ProfileTabTenants' })

const message = useMessage()
const accessStore = useAccessStore()
const { t } = useI18n()

// 成员类型走后端枚举元数据（本地化），未加载/未部署时回退静态 MEMBER_TYPE_OPTIONS
const enumService = useEnumService()
void enumService.ensureEnum('TenantMemberType')
function memberTypeLabel(value: TenantSwitcherDto['memberType']) {
  return enumService.getLabel('TenantMemberType', value, getOptionLabel(MEMBER_TYPE_OPTIONS, value))
}

const loading = ref(false)
const loaded = ref(false)
const switching = ref(false)
const tenants = ref<TenantSwitcherDto[]>([])

async function loadTenants() {
  loading.value = true
  try {
    tenants.value = await tenantApi.myAvailableTenants()
    loaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.tenants.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

/** 切换租户：重签发令牌后重载应用以加载新上下文 */
async function switchTo(tenantId: string, label: string) {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    const token = await tenantApi.switchTenant({ tenantId })
    accessStore.setAccessToken(token.accessToken)
    accessStore.setRefreshToken(token.refreshToken)
    message.success(t('component.profile.tenants.msg_switched_to', { label }))
    // 新上下文的权限/菜单需重新引导，直接重载以确保一致
    window.location.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.tenants.err_switch_failed'))
    switching.value = false
  }
}

/** 成员类型 → NTag 类型（所有者/管理员高亮） */
function memberTagType(type: TenantMemberType) {
  if (type === TenantMemberType.Owner) {
    return 'warning'
  }
  if (type === TenantMemberType.Admin || type === TenantMemberType.PlatformAdmin) {
    return 'primary'
  }
  return 'default'
}

/** 无 logo 时的占位首字符 */
function tenantInitial(tenant: TenantSwitcherDto): string {
  const name = tenant.tenantShortName || tenant.tenantName || ''
  return name.trim().charAt(0) || t('component.profile.tenants.tenant_initial_fallback')
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
          <NButton size="tiny" quaternary @click="loadTenants">
            <template #icon>
              <Icon icon="lucide:refresh-cw" />
            </template>
          </NButton>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="loading">
          <div class="pf-list">
            <NEmpty v-if="tenants.length === 0 && loaded" :description="t('component.profile.tenants.empty')" />
            <div
              v-for="tenant in tenants"
              :key="tenant.membershipId"
              class="pf-list-item"
              :class="{ 'pf-list-item--active': tenant.isCurrent }"
            >
              <div class="pf-list-icon pf-tenant-logo" :class="{ 'pf-list-icon--active': tenant.isCurrent }">
                <NAvatar
                  v-if="tenant.logo"
                  :src="tenant.logo"
                  :size="32"
                  object-fit="cover"
                  class="pf-tenant-avatar"
                />
                <template v-else>
                  {{ tenantInitial(tenant) }}
                </template>
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ tenant.tenantName }}
                  <span v-if="tenant.tenantShortName" class="pf-tenant-short">{{ tenant.tenantShortName }}</span>
                  <NTag v-if="tenant.isCurrent" type="success" size="tiny" :bordered="false">
                    {{ t('component.profile.tenants.tag_current') }}
                  </NTag>
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
                <NTag :type="memberTagType(tenant.memberType)" size="small" round :bordered="false">
                  {{ memberTypeLabel(tenant.memberType) }}
                </NTag>
                <NButton
                  v-if="!tenant.isCurrent"
                  size="small"
                  secondary
                  :loading="switching"
                  @click="switchTo(String(tenant.tenantId), tenant.tenantName)"
                >
                  {{ t('component.profile.tenants.btn_switch') }}
                </NButton>
              </div>
            </div>
          </div>
        </NSpin>
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

.pf-tenant-avatar {
  border-radius: 8px;
  background: transparent;
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

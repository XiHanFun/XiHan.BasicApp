<script lang="ts" setup>
import type { TenantSwitcherDto } from '@/api'
import { NAvatar, NButton, NEmpty, NSpin, NTag, useMessage } from 'naive-ui'
import { computed, onMounted, ref } from 'vue'
import { tenantApi, TenantMemberType } from '@/api'
import { MEMBER_TYPE_OPTIONS } from '~/constants'
import { Icon } from '~/iconify'
import { useAccessStore, useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'ProfileTabTenants' })

const message = useMessage()
const accessStore = useAccessStore()
const userStore = useUserStore()

const loading = ref(false)
const loaded = ref(false)
const switching = ref(false)
const tenants = ref<TenantSwitcherDto[]>([])

/** 是否可进入平台运维态（超管 / 平台管理员） */
const canAccessPlatform = computed(() => userStore.userInfo?.canAccessPlatform ?? false)
/** 当前是否处于平台运维态 */
const isPlatform = computed(() => userStore.userInfo?.isPlatform ?? false)

async function loadTenants() {
  loading.value = true
  try {
    tenants.value = await tenantApi.myAvailableTenants()
    loaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载租户列表失败')
  }
  finally {
    loading.value = false
  }
}

/** 切换租户 / 进入平台运维态：重签发令牌后重载应用以加载新上下文 */
async function switchTo(tenantId: null | string, label: string) {
  if (switching.value) {
    return
  }
  switching.value = true
  try {
    const token = await tenantApi.switchTenant({ tenantId })
    accessStore.setAccessToken(token.accessToken)
    accessStore.setRefreshToken(token.refreshToken)
    message.success(`已切换到「${label}」`)
    // 新上下文的权限/菜单需重新引导，直接重载以确保一致
    window.location.reload()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '切换失败')
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
function tenantInitial(t: TenantSwitcherDto): string {
  const name = t.tenantShortName || t.tenantName || ''
  return name.trim().charAt(0) || '租'
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
            <span>我的租户</span>
          </div>
          <div class="pf-section__desc">
            您当前可访问的租户组织及其成员角色
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
            <div
              v-if="canAccessPlatform"
              class="pf-list-item"
              :class="{ 'pf-list-item--active': isPlatform }"
            >
              <div class="pf-list-icon" :class="{ 'pf-list-icon--active': isPlatform }">
                <Icon icon="lucide:shield-check" width="18" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  平台运维
                  <NTag v-if="isPlatform" type="success" size="tiny" :bordered="false">
                    当前
                  </NTag>
                </div>
                <div class="pf-list-desc">
                  维护平台级全局菜单 / 权限 / 角色 / 版本
                </div>
              </div>
              <NButton v-if="!isPlatform" size="small" secondary :loading="switching" @click="switchTo(null, '平台运维')">
                进入
              </NButton>
            </div>
            <NEmpty v-if="tenants.length === 0 && loaded && !canAccessPlatform" description="暂无可访问的租户" />
            <div
              v-for="t in tenants"
              :key="t.membershipId"
              class="pf-list-item"
              :class="{ 'pf-list-item--active': t.isCurrent }"
            >
              <div class="pf-list-icon pf-tenant-logo" :class="{ 'pf-list-icon--active': t.isCurrent }">
                <NAvatar
                  v-if="t.logo"
                  :src="t.logo"
                  :size="32"
                  object-fit="cover"
                  class="pf-tenant-avatar"
                />
                <template v-else>
                  {{ tenantInitial(t) }}
                </template>
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ t.tenantName }}
                  <span v-if="t.tenantShortName" class="pf-tenant-short">{{ t.tenantShortName }}</span>
                  <NTag v-if="t.isCurrent" type="success" size="tiny" :bordered="false">
                    当前
                  </NTag>
                </div>
                <div class="pf-list-desc">
                  {{ getOptionLabel(MEMBER_TYPE_OPTIONS, t.memberType) }}
                  <template v-if="t.membershipExpirationTime">
                    · 加入 {{ formatDate(t.membershipExpirationTime, 'YYYY-MM-DD') }}
                  </template>
                  <template v-else-if="t.domain">
                    · {{ t.domain }}
                  </template>
                  <template v-else>
                    · {{ t.tenantCode }}
                  </template>
                </div>
              </div>
              <div class="pf-tenant-actions">
                <NTag :type="memberTagType(t.memberType)" size="small" round :bordered="false">
                  {{ getOptionLabel(MEMBER_TYPE_OPTIONS, t.memberType) }}
                </NTag>
                <NButton
                  v-if="!t.isCurrent"
                  size="small"
                  secondary
                  :loading="switching"
                  @click="switchTo(String(t.tenantId), t.tenantName)"
                >
                  切换
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

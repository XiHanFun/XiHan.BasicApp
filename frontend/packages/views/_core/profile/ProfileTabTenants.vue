<script lang="ts" setup>
import type { TenantSwitcherDto } from '@/api'
import { NAvatar, NCard, NEmpty, NSpin, NTag, useMessage } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { tenantApi, TenantMemberType } from '@/api'
import { Icon } from '~/iconify'
import { MEMBER_TYPE_OPTIONS } from '~/constants'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'ProfileTabTenants' })

const message = useMessage()

const loading = ref(false)
const loaded = ref(false)
const tenants = ref<TenantSwitcherDto[]>([])

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
    <NCard :bordered="false" size="small" class="pf-card">
      <template #header>
        <div class="pf-card-header">
          <Icon icon="lucide:building-2" width="16" />
          <span>我的租户</span>
        </div>
      </template>
      <NSpin :show="loading">
        <NEmpty v-if="tenants.length === 0 && loaded" description="暂无可访问的租户" />
        <div v-else class="pf-list">
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
            <NTag :type="memberTagType(t.memberType)" size="small" round :bordered="false">
              {{ getOptionLabel(MEMBER_TYPE_OPTIONS, t.memberType) }}
            </NTag>
          </div>
        </div>
      </NSpin>
    </NCard>
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
</style>

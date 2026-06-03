<script lang="ts" setup>
import type { UserProfile } from '~/types'
import { computed } from 'vue'
import { XUserAvatar } from '~/components'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { formatDate } from '~/utils'

const props = defineProps<{
  profile: UserProfile | null
  sessionsCount: number
  sessionsLoaded: boolean
}>()

const userStore = useUserStore()

/** 显示名 / 用户名 */
const displayName = computed(() => props.profile?.nickName || props.profile?.userName || userStore.nickname || '—')
const userName = computed(() => props.profile?.userName || '—')

/** 状态 chips */
const chips = computed(() => {
  const p = props.profile
  if (!p) {
    return [] as Array<{ key: string, label: string, on?: boolean }>
  }
  const list: Array<{ key: string, label: string, on?: boolean }> = []
  list.push({ key: 'active', label: '已激活', on: true })
  if (p.isSystemAccount) {
    list.push({ key: 'sys', label: '系统内置账号' })
  }
  if (p.twoFactorEnabled) {
    list.push({ key: '2fa', label: '已开启两步验证', on: true })
  }
  return list
})

/** meta 概要 */
const metaItems = computed(() => {
  const p = props.profile
  return [
    { key: 'lastLogin', icon: 'lucide:clock', label: '最后登录', value: p?.lastLoginTime ? formatDate(p.lastLoginTime) : '—' },
    { key: 'lastIp', icon: 'lucide:map-pin', label: '登录 IP', value: p?.lastLoginIp || '—' },
    { key: 'devices', icon: 'lucide:monitor', label: '在线设备', value: props.sessionsLoaded ? `${props.sessionsCount} 台` : '—' },
  ]
})
</script>

<template>
  <div class="pf-usercard">
    <div class="pf-usercard__top">
      <div class="pf-usercard__avatar">
        <XUserAvatar
          :size="56"
          :avatar="props.profile?.avatar || userStore.avatar"
          :name="displayName"
        />
      </div>
      <div class="pf-usercard__id">
        <div class="pf-usercard__name">
          {{ displayName }}
        </div>
        <div class="pf-usercard__uname">
          @{{ userName }}
        </div>
      </div>
    </div>

    <div v-if="chips.length" class="pf-usercard__chips">
      <span
        v-for="chip in chips"
        :key="chip.key"
        class="pf-usercard__chip"
        :class="{ 'is-on': chip.on }"
      >
        <i v-if="chip.on" class="pf-usercard__chip-dot" />
        {{ chip.label }}
      </span>
    </div>

    <div class="pf-usercard__meta">
      <div v-for="item in metaItems" :key="item.key" class="pf-usercard__meta-row">
        <span class="pf-usercard__meta-label">
          <Icon :icon="item.icon" width="13" />
          {{ item.label }}
        </span>
        <span class="pf-usercard__meta-value">{{ item.value }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.pf-usercard {
  padding: 18px;
  border-radius: var(--radius-lg);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
}

.pf-usercard__top {
  display: flex;
  align-items: center;
  gap: 12px;
}

.pf-usercard__avatar {
  flex-shrink: 0;
}

.pf-usercard__id {
  min-width: 0;
  flex: 1;
}

.pf-usercard__name {
  font-size: 16px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pf-usercard__uname {
  font-size: 12.5px;
  color: var(--text-secondary);
  margin-top: 2px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pf-usercard__chips {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  margin-top: 14px;
}

.pf-usercard__chip {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 11.5px;
  padding: 3px 9px;
  border-radius: 999px;
  background: hsl(var(--accent));
  color: var(--text-secondary);
}

.pf-usercard__chip.is-on {
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.pf-usercard__chip-dot {
  width: 5px;
  height: 5px;
  border-radius: 50%;
  background: hsl(var(--primary));
}

.pf-usercard__meta {
  margin-top: 14px;
  padding-top: 12px;
  border-top: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  gap: 9px;
}

.pf-usercard__meta-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
}

.pf-usercard__meta-label {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  color: var(--text-secondary);
}

.pf-usercard__meta-value {
  font-size: 12.5px;
  font-weight: 600;
  color: var(--text-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>

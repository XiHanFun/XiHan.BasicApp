<script lang="ts" setup>
import type { UserProfile } from '~/types'
import { NAvatar } from 'naive-ui'
import { computed } from 'vue'
import { useAvatarUrl } from '~/composables'
import { useUserStore } from '~/stores'
import { formatDate } from '~/utils'

const props = defineProps<{
  profile: UserProfile | null
  sessionsCount: number
  sessionsLoaded: boolean
}>()

const userStore = useUserStore()
const avatarDisplayUrl = useAvatarUrl(computed(() => props.profile?.avatar || userStore.avatar))

/** 显示名 / 用户名 */
const displayName = computed(() => props.profile?.nickName || props.profile?.userName || userStore.nickname || '—')
const userName = computed(() => props.profile?.userName || '—')

/** 首字母回退 */
const initials = computed(() => {
  const n = displayName.value
  return n && n !== '—' ? n.substring(0, 2) : '?'
})

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

/** 右侧 meta 概要 */
const metaItems = computed(() => {
  const p = props.profile
  return [
    { key: 'lastLogin', label: '最后登录', value: p?.lastLoginTime ? formatDate(p.lastLoginTime) : '—' },
    { key: 'lastIp', label: '登录 IP', value: p?.lastLoginIp || '—' },
    { key: 'devices', label: '在线设备', value: props.sessionsLoaded ? `${props.sessionsCount} 台` : '—' },
  ]
})
</script>

<template>
  <div class="pf-hero">
    <div class="pf-hero__bg" />
    <div class="pf-hero__grid" />
    <div class="pf-hero__inner">
      <div class="pf-hero__avatar">
        <NAvatar
          v-if="avatarDisplayUrl"
          round
          :size="80"
          :src="avatarDisplayUrl"
          object-fit="cover"
        />
        <div v-else class="pf-hero__avatar-text">
          {{ initials }}
        </div>
      </div>

      <div class="pf-hero__id">
        <div class="pf-hero__name">
          {{ displayName }}
        </div>
        <div class="pf-hero__uname">
          @{{ userName }}
          <template v-if="profile?.realName">
            · {{ profile.realName }}
          </template>
        </div>
        <div v-if="chips.length" class="pf-hero__chips">
          <span
            v-for="chip in chips"
            :key="chip.key"
            class="pf-hero__chip"
            :class="{ 'is-on': chip.on }"
          >
            <i v-if="chip.on" class="pf-hero__chip-dot" />
            {{ chip.label }}
          </span>
        </div>
      </div>

      <div class="pf-hero__meta">
        <div v-for="item in metaItems" :key="item.key" class="pf-hero__meta-item">
          <span class="pf-hero__meta-label">{{ item.label }}</span>
          <span class="pf-hero__meta-value">{{ item.value }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.pf-hero {
  position: relative;
  overflow: hidden;
  padding: 24px 28px;
  border-radius: var(--radius-lg, 12px);
  background: linear-gradient(135deg, hsl(var(--primary) / 92%) 0%, hsl(var(--primary)) 50%, hsl(var(--primary) / 78%) 100%);
  color: #fff;
  box-shadow: 0 18px 40px -24px hsl(var(--primary) / 60%);
}

/* 光晕 */
.pf-hero__bg {
  position: absolute;
  inset: 0;
  background:
    radial-gradient(420px 220px at 88% -10%, rgb(255 255 255 / 0.22), transparent 60%),
    radial-gradient(360px 300px at 6% 130%, rgb(0 0 0 / 0.18), transparent 60%);
  pointer-events: none;
}

/* 网格纹理 */
.pf-hero__grid {
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgb(255 255 255 / 0.07) 1px, transparent 0),
    linear-gradient(90deg, rgb(255 255 255 / 0.07) 1px, transparent 0);
  background-size: 38px 38px;
  mask-image: linear-gradient(180deg, #000, transparent 78%);
  -webkit-mask-image: linear-gradient(180deg, #000, transparent 78%);
  opacity: 0.6;
  pointer-events: none;
}

.pf-hero__inner {
  position: relative;
  display: flex;
  align-items: center;
  gap: 20px;
  flex-wrap: wrap;
}

.pf-hero__avatar {
  flex-shrink: 0;
}

.pf-hero__avatar-text {
  width: 80px;
  height: 80px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-size: 30px;
  font-weight: 700;
  color: #fff;
  background: rgb(255 255 255 / 0.18);
  border: 2px solid rgb(255 255 255 / 0.3);
}

.pf-hero__id {
  flex: 1;
  min-width: 220px;
}

.pf-hero__name {
  font-size: 24px;
  font-weight: 700;
  letter-spacing: 0.3px;
  line-height: 1.2;
}

.pf-hero__uname {
  font-size: 13px;
  color: rgb(255 255 255 / 0.78);
  margin-top: 4px;
}

.pf-hero__chips {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-top: 12px;
}

.pf-hero__chip {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  padding: 4px 11px;
  border-radius: 999px;
  background: rgb(255 255 255 / 0.14);
  border: 1px solid rgb(255 255 255 / 0.2);
}

.pf-hero__chip.is-on {
  background: rgb(255 255 255 / 0.22);
  border-color: rgb(255 255 255 / 0.4);
}

.pf-hero__chip-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #fff;
}

.pf-hero__meta {
  display: flex;
  gap: 28px;
  flex-wrap: wrap;
}

.pf-hero__meta-item {
  display: flex;
  flex-direction: column;
  gap: 3px;
  text-align: right;
}

.pf-hero__meta-label {
  font-size: 11.5px;
  color: rgb(255 255 255 / 0.62);
}

.pf-hero__meta-value {
  font-size: 14px;
  font-weight: 600;
}

@media (max-width: 720px) {
  .pf-hero {
    padding: 18px 18px;
  }

  .pf-hero__meta {
    width: 100%;
    gap: 18px;
  }

  .pf-hero__meta-item {
    text-align: left;
  }
}
</style>

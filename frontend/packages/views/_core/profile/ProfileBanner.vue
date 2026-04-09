<script lang="ts" setup>
import type { UserProfile } from '~/types'
import { NAvatar } from 'naive-ui'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import { formatDate } from '~/utils'

defineProps<{
  profile: UserProfile | null
  sessionsCount: number
  sessionsLoaded: boolean
}>()

const userStore = useUserStore()
</script>

<template>
  <div class="pf-banner">
    <div class="pf-banner-glow" />
    <div class="pf-banner-head">
      <div class="pf-banner-title">
        <Icon icon="lucide:user-cog" width="18" />
        <span>个人中心</span>
      </div>
    </div>
    <div class="pf-hero">
      <div class="pf-avatar-wrap">
        <NAvatar
          round :size="64"
          :src="profile?.avatar || userStore.avatar"
          :fallback-src="`https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
        />
        <button class="pf-avatar-edit">
          <Icon icon="lucide:camera" width="12" />
        </button>
      </div>
      <div class="pf-hero-body">
        <div class="pf-hero-name">
          {{ profile?.nickName || profile?.userName || userStore.nickname || '---' }}
        </div>
        <div class="pf-hero-sub">
          @{{ profile?.userName || '---' }}
          <template v-if="profile?.lastLoginTime">
            · {{ formatDate(profile.lastLoginTime) }}
          </template>
          <template v-if="profile?.lastLoginIp">
            ({{ profile.lastLoginIp }})
          </template>
        </div>
      </div>
    </div>
    <div class="pf-stat-grid">
      <div class="pf-stat-item">
        <div class="pf-stat-icon">
          <Icon icon="lucide:shield-check" width="16" />
        </div>
        <div class="pf-stat-body">
          <div class="pf-stat-label">
            两步验证
          </div>
          <div class="pf-stat-value">
            {{ profile?.twoFactorEnabled ? '已启用' : '未启用' }}
          </div>
        </div>
      </div>
      <div class="pf-stat-item">
        <div class="pf-stat-icon">
          <Icon icon="lucide:mail-check" width="16" />
        </div>
        <div class="pf-stat-body">
          <div class="pf-stat-label">
            邮箱验证
          </div>
          <div class="pf-stat-value">
            {{ profile?.emailVerified ? '已验证' : '未验证' }}
          </div>
        </div>
      </div>
      <div class="pf-stat-item">
        <div class="pf-stat-icon">
          <Icon icon="lucide:smartphone" width="16" />
        </div>
        <div class="pf-stat-body">
          <div class="pf-stat-label">
            手机验证
          </div>
          <div class="pf-stat-value">
            {{ profile?.phoneVerified ? '已验证' : '未验证' }}
          </div>
        </div>
      </div>
      <div class="pf-stat-item">
        <div class="pf-stat-icon">
          <Icon icon="lucide:monitor" width="16" />
        </div>
        <div class="pf-stat-body">
          <div class="pf-stat-label">
            在线设备
          </div>
          <div class="pf-stat-value">
            {{ sessionsLoaded ? sessionsCount : '-' }}
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.pf-banner {
  position: relative;
  overflow: hidden;
  padding: 16px 20px;
  border-radius: var(--radius);
  background:
    radial-gradient(circle at 85% 0%, hsl(var(--primary) / 16%), transparent 45%),
    linear-gradient(135deg, hsl(var(--accent)), hsl(var(--muted)));
  border: 1px solid var(--border-color);
}

.pf-banner-glow {
  position: absolute;
  right: -60px;
  top: -60px;
  width: 180px;
  height: 180px;
  border-radius: 50%;
  background: hsl(var(--primary) / 12%);
  filter: blur(20px);
  pointer-events: none;
}

.pf-banner-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 14px;
}

.pf-banner-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.pf-hero {
  display: flex;
  align-items: center;
  gap: 14px;
  margin-bottom: 14px;
}

.pf-avatar-wrap {
  position: relative;
  flex-shrink: 0;
}

.pf-avatar-edit {
  position: absolute;
  bottom: -2px;
  right: -2px;
  width: 22px;
  height: 22px;
  border-radius: 50%;
  border: 2px solid var(--bg-surface, #fff);
  background: hsl(var(--primary));
  color: #fff;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: transform 0.15s;
}

.pf-avatar-edit:hover {
  transform: scale(1.1);
}

.pf-hero-body {
  min-width: 0;
}

.pf-hero-name {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.2;
}

.pf-hero-sub {
  font-size: 13px;
  color: var(--text-secondary);
  margin-top: 4px;
}

.pf-stat-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}

.pf-stat-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: var(--radius);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  transition: border-color 0.2s;
}

.pf-stat-item:hover {
  border-color: hsl(var(--primary) / 30%);
}

.pf-stat-icon {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
}

.pf-stat-body {
  min-width: 0;
}

.pf-stat-label {
  font-size: 12px;
  color: var(--text-secondary);
}

.pf-stat-value {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  margin-top: 1px;
}

@media (max-width: 900px) {
  .pf-stat-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 640px) {
  .pf-banner {
    padding: 12px 14px;
  }

  .pf-stat-grid {
    grid-template-columns: 1fr 1fr;
    gap: 8px;
  }

  .pf-hero-name {
    font-size: 18px;
  }
}
</style>

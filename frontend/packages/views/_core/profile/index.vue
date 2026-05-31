<script lang="ts" setup>
import type { Component } from 'vue'
import type { UserProfile } from '~/types'
import { NSpin, useMessage } from 'naive-ui'
import { computed, markRaw, onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import ProfileBanner from './ProfileBanner.vue'
import ProfileTabDeveloper from './ProfileTabDeveloper.vue'
import ProfileTabDevices from './ProfileTabDevices.vue'
import ProfileTabInfo from './ProfileTabInfo.vue'
import ProfileTabNotifications from './ProfileTabNotifications.vue'
import ProfileTabSecurity from './ProfileTabSecurity.vue'
import ProfileTabStats from './ProfileTabStats.vue'

defineOptions({ name: 'ProfilePage' })

interface ProfileNavItem {
  key: string
  label: string
  desc: string
  icon: string
}

const message = useMessage()
const { apis } = useAppContext()
const activeTab = ref('profile')
const profileLoading = ref(false)
const profile = ref<UserProfile | null>(null)
const devicesRef = ref<InstanceType<typeof ProfileTabDevices> | null>(null)

const navItems: ProfileNavItem[] = [
  { key: 'profile', label: '个人资料', desc: '头像、昵称与联系方式', icon: 'lucide:contact' },
  { key: 'security', label: '安全设置', desc: '密码、两步验证与账号', icon: 'lucide:shield-check' },
  { key: 'devices', label: '登录设备', desc: '在线会话与设备管理', icon: 'lucide:monitor-smartphone' },
  { key: 'stats', label: '数据统计', desc: '登录、访问与活跃度', icon: 'lucide:bar-chart-3' },
  { key: 'notifications', label: '通知偏好', desc: '消息渠道与提醒', icon: 'lucide:bell-ring' },
  { key: 'developer', label: '开发者设置', desc: '令牌与第三方接入', icon: 'lucide:code-2' },
]

// markRaw 避免组件被响应式代理
const tabComponents: Record<string, Component> = {
  profile: markRaw(ProfileTabInfo),
  security: markRaw(ProfileTabSecurity),
  devices: markRaw(ProfileTabDevices),
  stats: markRaw(ProfileTabStats),
  notifications: markRaw(ProfileTabNotifications),
  developer: markRaw(ProfileTabDeveloper),
}

const activeComponent = computed(() => tabComponents[activeTab.value] ?? tabComponents.profile)

async function loadProfile() {
  profileLoading.value = true
  try {
    profile.value = await apis.getProfileApi()
  }
  // eslint-disable-next-line ts/no-explicit-any
  catch (e: any) {
    message.error(e?.message || '加载个人资料失败')
  }
  finally {
    profileLoading.value = false
  }
}

onMounted(() => {
  loadProfile()
})
</script>

<template>
  <div class="pf-page">
    <NSpin :show="profileLoading && !profile">
      <div class="pf-layout">
        <!-- 左侧：用户信息卡 + 导航 -->
        <aside class="pf-aside">
          <ProfileBanner
            :profile="profile"
            :sessions-count="devicesRef?.sessions?.length ?? 0"
            :sessions-loaded="devicesRef?.sessionsLoaded ?? false"
          />
          <nav class="pf-nav">
          <button
            v-for="item in navItems"
            :key="item.key"
            class="pf-nav-item"
            :class="{ 'is-active': activeTab === item.key }"
            type="button"
            @click="activeTab = item.key"
          >
            <span class="pf-nav-icon">
              <Icon :icon="item.icon" width="18" />
            </span>
            <span class="pf-nav-text">
              <span class="pf-nav-label">{{ item.label }}</span>
              <span class="pf-nav-desc">{{ item.desc }}</span>
            </span>
            <Icon class="pf-nav-arrow" icon="lucide:chevron-right" width="16" />
          </button>
          </nav>
        </aside>

        <!-- 右侧内容 -->
        <section class="pf-content">
          <KeepAlive>
            <component
              :is="activeComponent"
              v-bind="activeTab === 'profile' || activeTab === 'security' ? { profile } : {}"
              :ref="activeTab === 'devices' ? (el: any) => (devicesRef = el) : undefined"
              @saved="loadProfile"
              @updated="loadProfile"
            />
          </KeepAlive>
        </section>
      </div>
    </NSpin>
  </div>
</template>

<style scoped>
.pf-page {
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.pf-layout {
  display: grid;
  grid-template-columns: 248px 1fr;
  gap: 14px;
  align-items: start;
}

/* 左侧栏：用户卡 + 导航，整体 sticky */
.pf-aside {
  position: sticky;
  top: 12px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

/* 导航 */
.pf-nav {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 8px;
  border-radius: var(--radius);
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
}

.pf-nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  width: 100%;
  padding: 10px 12px;
  border: none;
  border-radius: var(--radius);
  background: transparent;
  cursor: pointer;
  text-align: left;
  transition: background 0.18s, color 0.18s;
  color: var(--text-secondary);
}

.pf-nav-item:hover {
  background: hsl(var(--accent));
  color: var(--text-primary);
}

.pf-nav-item.is-active {
  background: hsl(var(--primary) / 12%);
  color: hsl(var(--primary));
}

.pf-nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  flex-shrink: 0;
  border-radius: 9px;
  background: hsl(var(--primary) / 10%);
  color: hsl(var(--primary));
  transition: background 0.18s;
}

.pf-nav-item.is-active .pf-nav-icon {
  background: hsl(var(--primary) / 18%);
}

.pf-nav-text {
  display: flex;
  flex-direction: column;
  min-width: 0;
  flex: 1;
}

.pf-nav-label {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  line-height: 1.3;
}

.pf-nav-item.is-active .pf-nav-label {
  color: hsl(var(--primary));
}

.pf-nav-desc {
  font-size: 12px;
  color: var(--text-secondary);
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pf-nav-arrow {
  flex-shrink: 0;
  opacity: 0;
  transform: translateX(-4px);
  transition: opacity 0.18s, transform 0.18s;
}

.pf-nav-item.is-active .pf-nav-arrow {
  opacity: 1;
  transform: translateX(0);
  color: hsl(var(--primary));
}

/* 右侧内容 */
.pf-content {
  min-width: 0;
}

/* 移动端：左栏转为顶部、导航横向 */
@media (max-width: 860px) {
  .pf-layout {
    grid-template-columns: 1fr;
  }

  .pf-aside {
    position: static;
  }

  .pf-nav {
    flex-direction: row;
    overflow-x: auto;
    padding: 6px;
  }

  .pf-nav-item {
    flex-direction: column;
    align-items: center;
    gap: 6px;
    min-width: 88px;
    padding: 10px 8px;
    text-align: center;
  }

  .pf-nav-desc,
  .pf-nav-arrow {
    display: none;
  }
}
</style>

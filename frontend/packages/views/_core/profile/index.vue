<script lang="ts" setup>
import type { Component } from 'vue'
import type { UserProfile } from '~/types'
import { NSpin, useMessage } from 'naive-ui'
import { computed, markRaw, onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import ProfileTabBinding from './ProfileTabBinding.vue'
import ProfileTabDeveloper from './ProfileTabDeveloper.vue'
import ProfileTabDevices from './ProfileTabDevices.vue'
import ProfileTabInfo from './ProfileTabInfo.vue'
import ProfileTabLoginLogs from './ProfileTabLoginLogs.vue'
import ProfileTabNotifications from './ProfileTabNotifications.vue'
import ProfileTabSecurity from './ProfileTabSecurity.vue'
import ProfileTabStats from './ProfileTabStats.vue'
import ProfileTabTenants from './ProfileTabTenants.vue'

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

const navItems: ProfileNavItem[] = [
  { key: 'profile', label: '个人资料', desc: '头像、昵称与联系方式', icon: 'lucide:contact' },
  { key: 'security', label: '安全设置', desc: '密码、两步验证与账号状态', icon: 'lucide:shield-check' },
  { key: 'loginLogs', label: '登录日志', desc: '近期登录记录与异常排查', icon: 'lucide:file-clock' },
  { key: 'binding', label: '账号绑定', desc: '第三方账号关联', icon: 'lucide:link' },
  {
    key: 'devices',
    label: '登录设备',
    desc: '在线会话与设备管理',
    icon: 'lucide:monitor-smartphone',
  },
  { key: 'tenants', label: '我的租户', desc: '可访问的租户与成员身份', icon: 'lucide:building-2' },
  { key: 'stats', label: '数据统计', desc: '登录、访问与活跃度', icon: 'lucide:bar-chart-3' },
  { key: 'notifications', label: '通知偏好', desc: '消息渠道与提醒', icon: 'lucide:bell-ring' },
  { key: 'developer', label: '开发者设置', desc: '令牌与第三方接入', icon: 'lucide:code-2' },
]

const tabComponents: Record<string, Component> = {
  profile: markRaw(ProfileTabInfo),
  security: markRaw(ProfileTabSecurity),
  loginLogs: markRaw(ProfileTabLoginLogs),
  binding: markRaw(ProfileTabBinding),
  devices: markRaw(ProfileTabDevices),
  tenants: markRaw(ProfileTabTenants),
  stats: markRaw(ProfileTabStats),
  notifications: markRaw(ProfileTabNotifications),
  developer: markRaw(ProfileTabDeveloper),
}

const activeComponent = computed(() => tabComponents[activeTab.value] ?? tabComponents.profile)
const currentComponentProps = computed(() =>
  activeTab.value === 'profile' || activeTab.value === 'security'
    ? { profile: profile.value }
    : {},
)

function selectTab(key: string) {
  activeTab.value = key
}

async function loadProfile() {
  profileLoading.value = true
  try {
    profile.value = await apis.getProfileApi()
  }
  catch (error: unknown) {
    message.error(error instanceof Error && error.message ? error.message : '加载个人资料失败')
  }
  finally {
    profileLoading.value = false
  }
}

onMounted(loadProfile)
</script>

<template>
  <div class="pc">
    <nav class="pc__tabs" aria-label="个人中心选项卡" role="tablist">
      <button
        v-for="item in navItems"
        :key="item.key"
        type="button"
        role="tab"
        class="pc__tab"
        :class="{ 'is-active': activeTab === item.key }"
        :aria-selected="activeTab === item.key"
        @click="selectTab(item.key)"
      >
        <span class="pc__tab-icon">
          <Icon :icon="item.icon" width="17" />
        </span>
        <span class="pc__tab-copy">
          <span class="pc__tab-title">{{ item.label }}</span>
        </span>
      </button>
    </nav>

    <main class="pc__content">
      <NSpin :show="profileLoading && !profile">
        <KeepAlive>
          <component
            :is="activeComponent"
            v-bind="currentComponentProps"
            @saved="loadProfile"
            @updated="loadProfile"
          />
        </KeepAlive>
      </NSpin>
    </main>
  </div>
</template>

<style scoped>
.pc {
  min-height: 100%;
  display: flex;
  flex-direction: column;
  gap: 10px;
  padding: 14px 16px;
  background: var(--bg-base);
}

.pc__tabs {
  position: sticky;
  top: 0;
  z-index: 3;
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
  padding: 2px 0 4px;
  background: var(--bg-base);
}

.pc__tab {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 0 0 auto;
  min-width: 0;
  min-height: 38px;
  padding: 7px 11px;
  border: 0;
  border-radius: var(--radius);
  background: hsl(var(--accent) / 72%);
  color: var(--text-secondary);
  cursor: pointer;
  text-align: left;
  transition:
    background 0.16s,
    box-shadow 0.16s,
    color 0.16s;
}

.pc__tab:hover {
  background: hsl(var(--background) / 72%);
  color: var(--text-primary);
}

.pc__tab.is-active {
  background: var(--bg-surface);
  color: hsl(var(--primary));
  box-shadow: 0 10px 24px hsl(var(--primary) / 8%);
}

.pc__tab-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  flex-shrink: 0;
  border-radius: 8px;
  background: hsl(var(--background) / 70%);
  color: inherit;
}

.pc__tab-copy {
  display: flex;
  flex-direction: column;
  gap: 2px;
  min-width: 0;
}

.pc__tab-title {
  font-size: 14px;
  font-weight: 700;
  line-height: 1.3;
  color: inherit;
}

.pc__content {
  min-width: 0;
}

@media (max-width: 768px) {
  .pc {
    padding: 12px;
  }

  .pc__tabs {
    flex-wrap: nowrap;
    overflow-x: auto;
    overscroll-behavior-x: contain;
  }

  .pc__tab {
    flex: 0 0 auto;
    flex-direction: column;
    justify-content: center;
    gap: 5px;
    min-width: 70px;
    min-height: 56px;
    padding: 8px;
    text-align: center;
  }

  .pc__tab-icon {
    width: 30px;
    height: 30px;
  }

  .pc__tab-copy {
    align-items: center;
  }
}
</style>

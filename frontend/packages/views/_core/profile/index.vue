<script lang="ts" setup>
import type { Component } from 'vue'
import type { UserProfile } from '~/types'
import { NSpin, useMessage } from 'naive-ui'
import { computed, markRaw, nextTick, onMounted, ref } from 'vue'
import { XUserAvatar } from '~/components'
import { Icon } from '~/iconify'
import { useAppContext, useUserStore } from '~/stores'
import ProfileTabBinding from './ProfileTabBinding.vue'
import ProfileTabDeveloper from './ProfileTabDeveloper.vue'
import ProfileTabDevices from './ProfileTabDevices.vue'
import ProfileTabInfo from './ProfileTabInfo.vue'
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
const userStore = useUserStore()

const activeTab = ref('profile')
const profileLoading = ref(false)
const profile = ref<UserProfile | null>(null)
const devicesRef = ref<InstanceType<typeof ProfileTabDevices> | null>(null)
const bodyRef = ref<HTMLElement | null>(null)

const navItems: ProfileNavItem[] = [
  { key: 'profile', label: '个人资料', desc: '头像、昵称与联系方式', icon: 'lucide:contact' },
  { key: 'security', label: '安全设置', desc: '密码、两步验证与账号', icon: 'lucide:shield-check' },
  { key: 'binding', label: '账号绑定', desc: '第三方账号关联', icon: 'lucide:link' },
  { key: 'devices', label: '登录设备', desc: '在线会话与设备管理', icon: 'lucide:monitor-smartphone' },
  { key: 'tenants', label: '我的租户', desc: '可访问的租户与成员身份', icon: 'lucide:building-2' },
  { key: 'stats', label: '数据统计', desc: '登录、访问与活跃度', icon: 'lucide:bar-chart-3' },
  { key: 'notifications', label: '通知偏好', desc: '消息渠道与提醒', icon: 'lucide:bell-ring' },
  { key: 'developer', label: '开发者设置', desc: '令牌与第三方接入', icon: 'lucide:code-2' },
]

// markRaw 避免组件被响应式代理
const tabComponents: Record<string, Component> = {
  profile: markRaw(ProfileTabInfo),
  security: markRaw(ProfileTabSecurity),
  binding: markRaw(ProfileTabBinding),
  devices: markRaw(ProfileTabDevices),
  tenants: markRaw(ProfileTabTenants),
  stats: markRaw(ProfileTabStats),
  notifications: markRaw(ProfileTabNotifications),
  developer: markRaw(ProfileTabDeveloper),
}

const activeComponent = computed(() => tabComponents[activeTab.value] ?? tabComponents.profile)
const activeNav = computed(() => navItems.find(item => item.key === activeTab.value) ?? navItems[0]!)

const displayName = computed(() => profile.value?.nickName || profile.value?.userName || userStore.nickname || '—')
const userName = computed(() => profile.value?.userName || userStore.username || '—')
const avatarSource = computed(() => profile.value?.avatar || userStore.avatar)

/** 切换分区：仅替换右侧内容并把内容区滚动复位，框架不动 → 无页面跳动 */
function selectTab(key: string) {
  if (key === activeTab.value) {
    return
  }
  activeTab.value = key
  void nextTick(() => {
    if (bodyRef.value) {
      bodyRef.value.scrollTop = 0
    }
  })
}

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

onMounted(loadProfile)
</script>

<template>
  <div class="pc">
    <div class="pc__shell">
      <!-- 左侧栏：身份 + 导航 -->
      <aside class="pc__rail">
        <div class="pc__me">
          <XUserAvatar :size="42" :avatar="avatarSource" :name="displayName" />
          <div class="pc__me-text">
            <div class="pc__me-name">
              {{ displayName }}
            </div>
            <div class="pc__me-sub">
              @{{ userName }}
            </div>
          </div>
        </div>

        <div class="pc__rail-label">
          账户设置
        </div>
        <nav class="pc__nav">
          <button
            v-for="item in navItems"
            :key="item.key"
            type="button"
            class="pc__nav-item"
            :class="{ 'is-active': activeTab === item.key }"
            @click="selectTab(item.key)"
          >
            <span class="pc__nav-icon">
              <Icon :icon="item.icon" width="17" />
            </span>
            <span class="pc__nav-label">{{ item.label }}</span>
          </button>
        </nav>
      </aside>

      <!-- 右侧：分区标题栏 + 可滚动内容 -->
      <main class="pc__main">
        <header class="pc__bar">
          <div class="pc__bar-title">
            <Icon :icon="activeNav.icon" width="18" />
            <span>{{ activeNav.label }}</span>
          </div>
          <div class="pc__bar-desc">
            {{ activeNav.desc }}
          </div>
        </header>

        <div ref="bodyRef" class="pc__body">
          <NSpin :show="profileLoading && !profile">
            <KeepAlive>
              <component
                :is="activeComponent"
                v-bind="activeTab === 'profile' || activeTab === 'security' ? { profile } : {}"
                :ref="activeTab === 'devices' ? (el: any) => (devicesRef = el) : undefined"
                @saved="loadProfile"
                @updated="loadProfile"
              />
            </KeepAlive>
          </NSpin>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
/* 整页填满内容区高度，自身不产生页面级滚动 → 切换分区时框架零位移 */
.pc {
  height: 100%;
  padding: 16px;
}

.pc__shell {
  display: grid;
  grid-template-columns: 264px 1fr;
  height: 100%;
  overflow: hidden;
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius-lg);
}

/* ===== 左侧栏 ===== */
.pc__rail {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 16px 12px;
  background: hsl(var(--muted) / 0.5);
  border-right: 1px solid var(--border-color);
  overflow-y: auto;
}

.pc__me {
  display: flex;
  align-items: center;
  gap: 11px;
  padding: 6px 8px 16px;
  margin-bottom: 8px;
  border-bottom: 1px solid var(--border-color);
}

.pc__me-text {
  min-width: 0;
}

.pc__me-name {
  font-size: 14px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.3;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pc__me-sub {
  font-size: 12px;
  color: var(--text-secondary);
  margin-top: 1px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pc__rail-label {
  padding: 6px 10px 4px;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.14em;
  color: var(--text-secondary);
  opacity: 0.6;
}

.pc__nav {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.pc__nav-item {
  display: flex;
  align-items: center;
  gap: 11px;
  width: 100%;
  padding: 9px 10px;
  border: none;
  border-radius: var(--radius);
  background: transparent;
  color: var(--text-secondary);
  cursor: pointer;
  text-align: left;
  transition: background 0.16s, color 0.16s;
}

.pc__nav-item:hover {
  background: hsl(var(--accent));
  color: var(--text-primary);
}

.pc__nav-item.is-active {
  background: hsl(var(--primary) / 11%);
  color: hsl(var(--primary));
}

.pc__nav-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  flex-shrink: 0;
  border-radius: 8px;
  background: hsl(var(--muted));
  color: var(--text-secondary);
  transition: background 0.16s, color 0.16s;
}

.pc__nav-item:hover .pc__nav-icon {
  color: var(--text-primary);
}

.pc__nav-item.is-active .pc__nav-icon {
  background: hsl(var(--primary) / 16%);
  color: hsl(var(--primary));
}

.pc__nav-label {
  font-size: 14px;
  font-weight: 500;
}

/* ===== 右侧 ===== */
.pc__main {
  display: flex;
  flex-direction: column;
  min-width: 0;
  overflow: hidden;
}

.pc__bar {
  flex-shrink: 0;
  padding: 16px 28px;
  border-bottom: 1px solid var(--border-color);
}

.pc__bar-title {
  display: flex;
  align-items: center;
  gap: 9px;
  font-size: 16px;
  font-weight: 700;
  color: var(--text-primary);
  line-height: 1.3;
}

.pc__bar-desc {
  font-size: 13px;
  color: var(--text-secondary);
  margin-top: 3px;
}

.pc__body {
  flex: 1;
  overflow-y: auto;
  padding: 24px 28px 32px;
  scrollbar-gutter: stable;
}

/* ===== 响应式 ===== */
@media (max-width: 1023px) {
  .pc__shell {
    grid-template-columns: 220px 1fr;
  }

  .pc__bar,
  .pc__body {
    padding-left: 20px;
    padding-right: 20px;
  }
}

/* 手机：取消固定高度，转为自然页面滚动；导航变顶部横向 Tab 条 */
@media (max-width: 768px) {
  .pc {
    height: auto;
    padding: 12px;
  }

  .pc__shell {
    grid-template-columns: 1fr;
    height: auto;
  }

  .pc__rail {
    border-right: none;
    border-bottom: 1px solid var(--border-color);
    overflow-x: auto;
  }

  .pc__me,
  .pc__rail-label {
    display: none;
  }

  .pc__nav {
    flex-direction: row;
    gap: 4px;
  }

  .pc__nav-item {
    flex-direction: column;
    gap: 5px;
    min-width: 74px;
    padding: 9px 8px;
    text-align: center;
  }

  .pc__main {
    overflow: visible;
  }

  .pc__body {
    overflow: visible;
    padding: 18px 16px 24px;
  }
}
</style>

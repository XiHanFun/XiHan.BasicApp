<script lang="ts" setup>
import type { Component } from 'vue'
import type { UserProfile } from '~/types'
import { XhSpinner } from '@xihan-ui/vue'
import { computed, markRaw, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import { XUserAvatar } from '~/components'
import { toast } from '~/composables'
import { PROFILE_ACTIVE_TAB_KEY } from '~/constants'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAppContext, useUserStore } from '~/stores'
import { SessionStorage } from '~/utils'
import ProfileTabBinding from './ProfileTabBinding.vue'
import ProfileTabDeveloper from './ProfileTabDeveloper.vue'
import ProfileTabDevices from './ProfileTabDevices.vue'
import ProfileTabInfo from './ProfileTabInfo.vue'
import ProfileTabLoginLogs from './ProfileTabLoginLogs.vue'
import ProfileTabNotifications from './ProfileTabNotifications.vue'
import ProfileTabOAuth from './ProfileTabOAuth.vue'
import ProfileTabSecurity from './ProfileTabSecurity.vue'
import ProfileTabStats from './ProfileTabStats.vue'
import ProfileTabTenants from './ProfileTabTenants.vue'

defineOptions({ name: 'ProfilePage' })

interface ProfileNavItem {
  key: string
  label: string
  icon: string
}

interface ProfileNavGroup {
  title: string | null
  items: ProfileNavItem[]
}

const { apis } = useAppContext()
const userStore = useUserStore()
const route = useRoute()
const { t } = useI18n()

const activeTab = ref('profile')
const profileLoading = ref(false)
const profile = ref<UserProfile | null>(null)

/** GitHub 风格分组导航：顶部资料项 + 按主题分组 */
const navGroups = computed<ProfileNavGroup[]>(() => [
  {
    title: null,
    items: [
      {
        key: 'profile',
        label: t('component.profile.nav.tab_profile'),
        icon: 'lucide:contact',
      },
    ],
  },
  {
    title: t('component.profile.nav.group_access_security'),
    items: [
      {
        key: 'security',
        label: t('component.profile.nav.tab_security'),
        icon: 'lucide:shield-check',
      },
      {
        key: 'binding',
        label: t('component.profile.nav.tab_binding'),
        icon: 'lucide:link',
      },
      {
        key: 'devices',
        label: t('component.profile.nav.tab_devices'),
        icon: 'lucide:monitor-smartphone',
      },
      {
        key: 'loginLogs',
        label: t('component.profile.nav.tab_login_logs'),
        icon: 'lucide:file-clock',
      },
    ],
  },
  {
    title: t('component.profile.nav.group_preferences'),
    items: [
      {
        key: 'notifications',
        label: t('component.profile.nav.tab_notifications'),
        icon: 'lucide:bell-ring',
      },
    ],
  },
  {
    title: t('component.profile.nav.group_tenants_data'),
    items: [
      {
        key: 'tenants',
        label: t('component.profile.nav.tab_tenants'),
        icon: 'lucide:building-2',
      },
      {
        key: 'stats',
        label: t('component.profile.nav.tab_stats'),
        icon: 'lucide:bar-chart-3',
      },
    ],
  },
  {
    title: t('component.profile.nav.group_developer'),
    items: [
      {
        key: 'developer',
        label: t('component.profile.nav.tab_developer'),
        icon: 'lucide:code-2',
      },
      {
        key: 'oauth',
        label: t('component.profile.nav.tab_oauth'),
        icon: 'lucide:blocks',
      },
    ],
  },
])

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
  oauth: markRaw(ProfileTabOAuth),
}

// 初始选中页签：URL ?tab=（深链兼容）> 会话记忆 > 默认「个人资料」
// 注意：个人中心是「一个页面」，子页签切换只改内部状态，不再写入路由 query，
// 否则带不同 ?tab= 的 fullPath 会被标签栏识别为新标签，导致每点一次就多开一个标签
activeTab.value = (() => {
  const queryTab = typeof route.query.tab === 'string'
    ? route.query.tab
    : ''
  if (queryTab && queryTab in tabComponents) {
    return queryTab
  }
  const stored = SessionStorage.get<string>(PROFILE_ACTIVE_TAB_KEY)
  if (stored && stored in tabComponents) {
    return stored
  }
  return 'profile'
})()

const activeComponent = computed(() => tabComponents[activeTab.value] ?? tabComponents.profile)
const currentComponentProps = computed(() =>
  activeTab.value === 'profile' || activeTab.value === 'security'
    ? { profile: profile.value }
    : {},
)

function selectTab(key: string) {
  activeTab.value = key
  // 仅记入会话存储（刷新后仍停留在原子页），不写入路由，避免标签栏重复开标签
  SessionStorage.set(PROFILE_ACTIVE_TAB_KEY, key)
}

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余取 message */
function messageOf(error: unknown): string | undefined {
  return error instanceof ApiError
    ? error.detail
    : (error as Error | undefined)?.message
}

async function loadProfile() {
  profileLoading.value = true
  try {
    profile.value = await apis.getProfileApi()
  }
  catch (error: unknown) {
    toast.danger(messageOf(error) || t('component.profile.msg_load_profile_failed'))
  }
  finally {
    profileLoading.value = false
  }
}

onMounted(loadProfile)
</script>

<template>
  <div class="pc">
    <div class="pc__container">
      <!-- 左侧：身份信息 + 分组导航（GitHub 设置页风格） -->
      <aside class="pc__sidebar">
        <div class="pc__identity">
          <XUserAvatar
            :size="40"
            :avatar="userStore.avatar"
            :name="userStore.nickname || userStore.username"
          />
          <div class="pc__identity-copy">
            <div class="pc__identity-name">
              {{ userStore.nickname || userStore.username }}
            </div>
            <div class="pc__identity-sub">
              @{{ userStore.username }} · {{ t('component.profile.identity_personal_account') }}
            </div>
          </div>
        </div>

        <nav class="pc__nav" :aria-label="t('component.profile.nav.aria_nav')">
          <div
            v-for="(group, groupIndex) in navGroups"
            :key="groupIndex"
            class="pc__nav-group"
          >
            <div v-if="group.title" class="pc__nav-group-title">
              {{ group.title }}
            </div>
            <button
              v-for="item in group.items"
              :key="item.key"
              type="button"
              class="pc__nav-item"
              :class="{ 'is-active': activeTab === item.key }"
              :aria-current="activeTab === item.key ? 'page' : undefined"
              @click="selectTab(item.key)"
            >
              <Icon :icon="item.icon" width="16" class="pc__nav-icon" />
              <span class="pc__nav-label">{{ item.label }}</span>
            </button>
          </div>
        </nav>
      </aside>

      <!-- 右侧：内容区 -->
      <main class="pc__content">
        <div class="xh-loading-stage" :class="{ 'is-loading': profileLoading && !profile }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <KeepAlive>
            <component
              :is="activeComponent"
              v-bind="currentComponentProps"
              @saved="loadProfile"
              @updated="loadProfile"
            />
          </KeepAlive>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
.pc {
  min-height: 100%;
  padding: var(--xh-space-6) var(--xh-space-5);
  /* GitHub 式纯净背景：亮色为白、暗色随主题，不再用灰色底 */
  background: var(--xh-bg-surface);
}

/* GitHub 式居中容器：侧边栏 + 内容整体限宽水平居中 */
.pc__container {
  display: flex;
  gap: var(--xh-space-6);
  align-items: flex-start;
  width: 100%;
  max-width: 1336px;
  margin: 0 auto;
}

/* ===== 左侧导航 ===== */
.pc__sidebar {
  position: sticky;
  top: 14px;
  flex-shrink: 0;
  width: 232px;
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.pc__identity {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
  padding: var(--xh-space-0_5) var(--xh-space-1);
  min-width: 0;
}

.pc__identity-copy {
  min-width: 0;
}

.pc__identity-name {
  font-size: 14px;
  font-weight: 700;
  color: var(--xh-fg-default);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pc__identity-sub {
  font-size: 12px;
  color: var(--xh-fg-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.pc__nav {
  display: flex;
  flex-direction: column;
}

.pc__nav-group {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
}

.pc__nav-group + .pc__nav-group {
  margin-top: 10px;
  padding-top: var(--xh-space-2_5);
  border-top: 1px solid color-mix(in srgb, var(--xh-border-default) 70%, transparent);
}

.pc__nav-group-title {
  padding: var(--xh-space-0_5) var(--xh-space-2_5) var(--xh-space-1_5);
  font-size: 12px;
  font-weight: 600;
  color: var(--xh-fg-muted);
}

.pc__nav-item {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  width: 100%;
  padding: var(--xh-space-1_5) var(--xh-space-2_5);
  border: 0;
  border-radius: var(--xh-shape-control);
  background: transparent;
  color: var(--xh-fg-default);
  font-size: 13.5px;
  text-align: left;
  cursor: pointer;
  transition: background var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

.pc__nav-item:hover {
  background: var(--xh-bg-subtle-hover);
}

.pc__nav-item.is-active {
  background: var(--xh-bg-subtle-hover);
  font-weight: 600;
}

/* GitHub 式激活指示条 */
.pc__nav-item.is-active::before {
  content: '';
  position: absolute;
  inset-inline-start: -8px;
  top: 50%;
  transform: translateY(-50%);
  width: 3px;
  height: 60%;
  border-radius: var(--xh-shape-inset);
  background: var(--xh-bg-brand);
}

.pc__nav-icon {
  flex-shrink: 0;
  color: var(--xh-fg-muted);
}

.pc__nav-item.is-active .pc__nav-icon {
  color: var(--xh-fg-brand);
}

.pc__nav-label {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* ===== 右侧内容 ===== */
.pc__content {
  flex: 1;
  min-width: 0;
  max-width: 1080px; /* GitHub 式可读宽度上限，超宽屏不无限拉伸 */
}

/* ===== 窄屏：导航横向滑动条 ===== */
@media (max-width: 900px) {
  .pc {
    padding: var(--xh-space-3);
  }

  .pc__container {
    flex-direction: column;
    /* 纵向布局时横轴改为拉伸：flex-start 会让内容按固有宽度靠左，右侧留白 */
    align-items: stretch;
    gap: var(--xh-space-3);
  }

  .pc__sidebar {
    position: static;
    width: 100%;
    gap: var(--xh-space-2_5);
  }

  .pc__nav {
    flex-direction: row;
    gap: var(--xh-space-1_5);
    overflow-x: auto;
    overscroll-behavior-x: contain;
    padding-bottom: var(--xh-space-1);
  }

  .pc__nav-group {
    flex-direction: row;
    gap: var(--xh-space-1_5);
  }

  .pc__nav-group + .pc__nav-group {
    margin-top: 0;
    padding-top: 0;
    border-top: 0;
  }

  .pc__nav-group-title {
    display: none;
  }

  .pc__nav-item {
    flex: 0 0 auto;
    width: auto;
    padding: var(--xh-space-1_5) var(--xh-space-3);
    background: color-mix(in srgb, var(--xh-bg-subtle-hover) 72%, transparent);
  }

  .pc__nav-item.is-active {
    color: var(--xh-fg-brand);
  }

  .pc__nav-item.is-active::before {
    display: none;
  }
}
</style>

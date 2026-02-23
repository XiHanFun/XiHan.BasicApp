<script lang="ts" setup>
import { Icon } from '@iconify/vue'
import { NConfigProvider, NDialogProvider, NIcon, NInput, NLoadingBarProvider, NMessageProvider, NNotificationProvider } from 'naive-ui'
import { computed, onMounted, onUnmounted, ref, watchEffect } from 'vue'
import { useAuthStore } from '~/stores'
import { useNaiveLocale, useTheme } from '~/hooks'
import { useAppStore, useUserStore } from '~/stores'

defineOptions({ name: 'App' })

const appStore = useAppStore()
const authStore = useAuthStore()
const userStore = useUserStore()
const { isDark, naiveTheme, themeOverrides } = useTheme()
const { naiveLocale, naiveDateLocale } = useNaiveLocale()

// 水印：动态生成 SVG data URI，全屏 repeat 平铺
const watermarkStyle = computed(() => {
  const text = appStore.watermarkText || 'XiHan BasicApp'
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="200" height="140">
    <text transform="rotate(-25, 100, 70)" x="10" y="80"
      fill="#808080" fill-opacity="0.18" font-size="13" font-family="sans-serif">
      ${text}
    </text>
  </svg>`
  const encoded = `url("data:image/svg+xml,${encodeURIComponent(svg)}")`
  return { backgroundImage: encoded, backgroundSize: '200px 140px' }
})

// 同步 html class 以支持 Tailwind dark mode
// 注意：用 classList.toggle 而非 className= 全量替换，
// 避免清除 theme-switching 等其他类（否则 ViewTransition 期间的 CSS 过渡无法被抑制）
watchEffect(() => {
  document.documentElement.classList.toggle('dark', isDark.value)
  document.documentElement.classList.toggle('light', !isDark.value)
  document.documentElement.style.filter = appStore.grayscaleEnabled
    ? 'grayscale(100%)'
    : appStore.colorWeaknessEnabled
      ? 'invert(0.8) hue-rotate(180deg)'
      : ''
  document.documentElement.style.fontSize = `${appStore.fontSize}px`
})

// ── 锁屏 ──────────────────────────────────────────────────────────────────────
// 锁屏分三个阶段：
//   setting  → 锁定时设置密码（新密码 + 确认）
//   locked   → 已锁，输入锁屏密码解锁
//   fallback → 连续错误 MAX_ATTEMPTS 次后，改用账号密码
type LockMode = 'off' | 'setting' | 'locked'

const LOCK_SESS_KEY = 'xihan_locked'
const LOCK_PWD_SESS_KEY = 'xihan_lock_pwd'
const MAX_LOCK_ATTEMPTS = 5

const lockMode = ref<LockMode>('off')
const lockAttempts = ref(0)
const lockPwdNew = ref('') // 设置阶段：新密码
const lockPwdConfirm = ref('') // 设置阶段：确认密码
const lockPwdError = ref('')
const unlockPwd = ref('') // 解锁阶段：输入密码
const unlockError = ref('')
/** 是否设置了锁屏密码（不能在模板里直接访问 sessionStorage） */
const hasLockPwd = ref(false)

/** 触发锁屏：进入 setting 阶段让用户设置本次密码 */
function doLock() {
  lockMode.value = 'setting'
  lockPwdNew.value = ''
  lockPwdConfirm.value = ''
  lockPwdError.value = ''
  // 可在此处向后端上报锁屏事件，参数：{ username, lockedAt }
}

/** setting 阶段：确认设置密码后真正锁屏 */
function confirmLock() {
  if (lockPwdNew.value && lockPwdNew.value !== lockPwdConfirm.value) {
    lockPwdError.value = '两次输入不一致'
    return
  }
  // 将密码（或空）存 sessionStorage，页面刷新仍保持锁定
  sessionStorage.setItem(LOCK_SESS_KEY, '1')
  if (lockPwdNew.value) {
    sessionStorage.setItem(LOCK_PWD_SESS_KEY, btoa(lockPwdNew.value))
    hasLockPwd.value = true
  }
  else {
    sessionStorage.removeItem(LOCK_PWD_SESS_KEY)
    hasLockPwd.value = false
  }
  lockMode.value = 'locked'
  unlockPwd.value = ''
  unlockError.value = ''
  lockAttempts.value = 0
}

/** locked 阶段：校验锁屏密码 */
function doUnlock() {
  const stored = sessionStorage.getItem(LOCK_PWD_SESS_KEY)
  if (!stored) {
    // 未设置密码：直接解锁
    releaselock()
    return
  }
  if (btoa(unlockPwd.value) !== stored) {
    lockAttempts.value++
    unlockPwd.value = ''
    if (lockAttempts.value >= MAX_LOCK_ATTEMPTS) {
      // 超过最大尝试次数，直接退出登录
      releaselock()
      authStore.logout()
      return
    }
    unlockError.value = `密码错误，还可尝试 ${MAX_LOCK_ATTEMPTS - lockAttempts.value} 次`
    return
  }
  releaselock()
}

function releaselock() {
  sessionStorage.removeItem(LOCK_SESS_KEY)
  sessionStorage.removeItem(LOCK_PWD_SESS_KEY)
  lockMode.value = 'off'
  lockAttempts.value = 0
  hasLockPwd.value = false
  unlockPwd.value = ''
  unlockError.value = ''
}

function handleGlobalShortcuts(e: KeyboardEvent) {
  if (!appStore.shortcutEnable)
    return

  // Ctrl/Cmd + K：全局搜索
  if (appStore.shortcutSearch && (e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
    e.preventDefault()
    window.dispatchEvent(new Event('xihan-open-global-search'))
    return
  }

  // Alt + Q：退出登录
  if (appStore.shortcutLogout && e.altKey && e.key.toLowerCase() === 'q') {
    e.preventDefault()
    authStore.logout()
    return
  }

  // Alt + L：锁屏
  if (appStore.shortcutLock && e.altKey && e.key.toLowerCase() === 'l') {
    e.preventDefault()
    if (appStore.widgetLockScreen)
      doLock()
  }

  // Esc：无密码时直接解锁
  if (e.key === 'Escape' && lockMode.value === 'locked') {
    if (!sessionStorage.getItem(LOCK_PWD_SESS_KEY))
      releaselock()
  }
}

function handleLockScreenRequest() {
  if (appStore.widgetLockScreen)
    doLock()
}

onMounted(() => {
  window.addEventListener('keydown', handleGlobalShortcuts)
  window.addEventListener('xihan-lock-screen', handleLockScreenRequest)
  // 页面刷新后恢复锁定状态
  if (sessionStorage.getItem(LOCK_SESS_KEY) === '1') {
    lockMode.value = 'locked'
    lockAttempts.value = 0
    hasLockPwd.value = !!sessionStorage.getItem(LOCK_PWD_SESS_KEY)
  }
})

onUnmounted(() => {
  window.removeEventListener('keydown', handleGlobalShortcuts)
  window.removeEventListener('xihan-lock-screen', handleLockScreenRequest)
})
</script>

<template>
  <NConfigProvider
    :locale="naiveLocale"
    :date-locale="naiveDateLocale"
    :theme="naiveTheme"
    :theme-overrides="themeOverrides"
    class="h-full"
  >
    <NLoadingBarProvider>
      <NMessageProvider placement="top" :duration="3000" :max="5">
        <NNotificationProvider placement="top-right">
          <NDialogProvider>
            <RouterView />
            <!-- 水印：SVG data URI repeat 平铺，全屏均匀分布 -->
            <div
              v-if="appStore.watermarkEnabled"
              class="watermark-layer"
              :style="watermarkStyle"
            />
            <!-- 锁屏遮罩 -->
            <Teleport to="body">
              <div v-if="lockMode !== 'off'" class="lock-screen-mask">
                <div class="lock-screen-card">
                  <!-- 用户信息 -->
                  <img
                    class="lock-screen-avatar"
                    :src="userStore.avatar || `https://api.dicebear.com/9.x/initials/svg?seed=${userStore.nickname}`"
                    :alt="userStore.nickname"
                  >
                  <div class="lock-screen-name">
                    {{ userStore.nickname || userStore.username }}
                  </div>

                  <!-- ① 设置密码阶段 -->
                  <template v-if="lockMode === 'setting'">
                    <div class="lock-screen-hint">
                      设置本次锁屏密码（可留空跳过）
                    </div>
                    <form class="lock-screen-input-wrap" @submit.prevent="confirmLock">
                      <NInput
                        v-model:value="lockPwdNew"
                        type="password"
                        show-password-on="click"
                        placeholder="新密码（留空则无需密码）"
                        size="large"
                        autocomplete="new-password"
                      >
                        <template #prefix>
                          <NIcon><Icon icon="lucide:key-round" width="16" /></NIcon>
                        </template>
                      </NInput>
                      <NInput
                        v-if="lockPwdNew"
                        v-model:value="lockPwdConfirm"
                        type="password"
                        show-password-on="click"
                        placeholder="确认密码"
                        size="large"
                        style="margin-top: 8px"
                        :status="lockPwdError ? 'error' : undefined"
                        autocomplete="new-password"
                      >
                        <template #prefix>
                          <NIcon><Icon icon="lucide:lock" width="16" /></NIcon>
                        </template>
                      </NInput>
                      <div v-if="lockPwdError" class="lock-screen-error">
                        {{ lockPwdError }}
                      </div>
                      <button type="submit" class="unlock-btn" style="margin-top: 12px">
                        <NIcon size="15" style="vertical-align: -2px; margin-right: 5px;">
                          <Icon icon="lucide:lock" />
                        </NIcon>
                        确认锁屏
                      </button>
                    </form>
                  </template>

                  <!-- ② 已锁定，输入锁屏密码 -->
                  <template v-else-if="lockMode === 'locked'">
                    <div class="lock-screen-hint">
                      {{ hasLockPwd ? '请输入锁屏密码' : '按下解锁按钮继续' }}
                    </div>
                    <form v-if="hasLockPwd" class="lock-screen-input-wrap" @submit.prevent="doUnlock">
                      <NInput
                        v-model:value="unlockPwd"
                        type="password"
                        show-password-on="click"
                        placeholder="锁屏密码"
                        size="large"
                        :status="unlockError ? 'error' : undefined"
                        autocomplete="current-password"
                      >
                        <template #prefix>
                          <NIcon><Icon icon="lucide:key-round" width="16" /></NIcon>
                        </template>
                      </NInput>
                      <div v-if="unlockError" class="lock-screen-error">
                        {{ unlockError }}
                      </div>
                      <button type="submit" class="unlock-btn" style="margin-top: 12px">
                        <NIcon size="15" style="vertical-align: -2px; margin-right: 5px;">
                          <Icon icon="lucide:lock-open" />
                        </NIcon>
                        解锁
                      </button>
                    </form>
                    <button v-else class="unlock-btn" @click="doUnlock">
                      <NIcon size="15" style="vertical-align: -2px; margin-right: 5px;">
                        <Icon icon="lucide:lock-open" />
                      </NIcon>
                      解锁
                    </button>
                  </template>
                </div>
              </div>
            </Teleport>
          </NDialogProvider>
        </NNotificationProvider>
      </NMessageProvider>
    </NLoadingBarProvider>
  </NConfigProvider>
</template>

<style>
.watermark-layer {
  pointer-events: none;
  position: fixed;
  inset: 0;
  z-index: 1999;
  background-repeat: repeat;
  user-select: none;
}

.lock-screen-mask {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  background: hsl(222 47% 6% / 0.92);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
}

.lock-screen-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 40px 48px;
  border-radius: 20px;
  background: hsl(var(--card) / 0.9);
  border: 1px solid hsl(var(--border));
  text-align: center;
  box-shadow: 0 24px 80px hsl(0 0% 0% / 0.4);
  min-width: 300px;
}

.lock-screen-avatar {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  border: 3px solid hsl(var(--border));
  object-fit: cover;
}

.lock-screen-name {
  font-size: 18px;
  font-weight: 600;
  color: hsl(var(--foreground));
  line-height: 1.3;
}

.lock-screen-hint {
  font-size: 13px;
  color: hsl(var(--muted-foreground));
  margin-bottom: 4px;
}

.lock-screen-input-wrap {
  width: 100%;
  display: flex;
  flex-direction: column;
}

.lock-screen-error {
  margin-top: 6px;
  font-size: 12px;
  color: hsl(var(--destructive));
  text-align: left;
}

.unlock-btn {
  margin-top: 12px;
  padding: 9px 32px;
  border-radius: 10px;
  background: hsl(var(--primary));
  color: hsl(var(--primary-foreground));
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: opacity 0.15s ease;
  border: none;
  display: inline-flex;
  align-items: center;
}

.unlock-btn:hover {
  opacity: 0.88;
}

.unlock-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>

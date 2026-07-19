<script lang="ts" setup>
import { NIcon, NInput } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { useLockScreen } from '~/composables'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import UserAvatar from './UserAvatar.vue'

defineOptions({ name: 'LockScreen' })

const { t } = useI18n()
const userStore = useUserStore()
const {
  lockMode,
  lockPwdNew,
  lockPwdConfirm,
  lockPwdError,
  lockLoading,
  unlockPwd,
  unlockError,
  unlockLoading,
  logoutLoading,
  confirmLock,
  cancelLock,
  doUnlock,
  logoutAndRelogin,
} = useLockScreen()
</script>

<template>
  <Teleport to="body">
    <div v-if="lockMode !== 'off'" class="lock-screen-mask">
      <!-- 不透明背景：锁屏遮住的内容不该透出来（半透明 + 模糊仍能看清大致版面）。
           动效沿用登录页的漂移光斑，保持同一套视觉语言 -->
      <div class="lock-screen-bg" aria-hidden="true">
        <div class="lock-blob lock-blob--1" />
        <div class="lock-blob lock-blob--2" />
        <div class="lock-blob lock-blob--3" />
      </div>
      <div class="lock-screen-card">
        <!-- 用户信息 -->
        <UserAvatar
          class="lock-screen-avatar"
          :size="72"
          :avatar="userStore.avatar"
          :name="userStore.nickname || userStore.username"
        />
        <div class="lock-screen-name">
          {{ userStore.nickname || userStore.username }}
        </div>

        <!-- ① 设置密码阶段 -->
        <template v-if="lockMode === 'setting'">
          <div class="lock-screen-hint">
            {{ t('component.lock_screen.setup_password_hint') }}
          </div>
          <form class="lock-screen-input-wrap" @submit.prevent="confirmLock">
            <NInput
              v-model:value="lockPwdNew"
              type="password"
              show-password-on="click"
              :placeholder="t('component.lock_screen.new_password_placeholder')"
              size="large"
              autocomplete="new-password"
            >
              <template #prefix>
                <NIcon><Icon icon="lucide:key-round" width="16" /></NIcon>
              </template>
            </NInput>
            <NInput
              v-model:value="lockPwdConfirm"
              type="password"
              show-password-on="click"
              :placeholder="t('component.lock_screen.confirm_password_placeholder')"
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
            <button type="submit" class="unlock-btn" style="margin-top: 12px" :disabled="lockLoading">
              <NIcon size="15" style="vertical-align: -2px; margin-right: 5px">
                <Icon icon="lucide:lock" />
              </NIcon>
              {{ t('component.lock_screen.confirm_lock_btn') }}
            </button>
            <!-- 此刻还没真正锁上，允许直接退出这个设置框，免得误点锁屏后无路可走 -->
            <button type="button" class="lock-text-btn" @click="cancelLock">
              {{ t('component.lock_screen.cancel_btn') }}
            </button>
          </form>
        </template>

        <!-- ② 已锁定，输入锁屏密码（口令由服务端校验；无密码解锁与 Esc 解锁已移除——那是绕过口） -->
        <template v-else-if="lockMode === 'locked'">
          <div class="lock-screen-hint">
            {{ t('component.lock_screen.input_password_hint') }}
          </div>
          <form class="lock-screen-input-wrap" @submit.prevent="doUnlock">
            <NInput
              v-model:value="unlockPwd"
              type="password"
              show-password-on="click"
              :placeholder="t('component.lock_screen.password_placeholder')"
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
            <button type="submit" class="unlock-btn" style="margin-top: 12px" :disabled="unlockLoading">
              <NIcon size="15" style="vertical-align: -2px; margin-right: 5px">
                <Icon icon="lucide:lock-open" />
              </NIcon>
              {{ t('component.lock_screen.unlock_btn') }}
            </button>
            <!-- 忘了锁屏口令时的出口：结束会话回登录页，而不是把用户永远困在遮罩里 -->
            <button
              type="button"
              class="lock-text-btn"
              :disabled="logoutLoading"
              @click="logoutAndRelogin"
            >
              <NIcon size="14" style="vertical-align: -2px; margin-right: 4px">
                <Icon icon="lucide:log-out" />
              </NIcon>
              {{ t('component.lock_screen.logout_btn') }}
            </button>
          </form>
        </template>
      </div>
    </div>
  </Teleport>
</template>

<style>
.lock-screen-mask {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  /* 完全不透明：锁屏就该真的挡住内容，不能靠模糊糊弄过去 */
  background: hsl(var(--background-deep));
  animation: lock-screen-fade 0.24s ease;
}

.lock-screen-bg {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: none;
}

.lock-blob {
  position: absolute;
  border-radius: 50%;
  will-change: transform;
}

.lock-blob--1 {
  left: -10%;
  top: -14%;
  width: 620px;
  height: 620px;
  background: hsl(var(--primary) / 0.22);
  filter: blur(100px);
  animation: lock-blob-1 18s ease-in-out infinite;
}

.lock-blob--2 {
  right: 6%;
  top: -8%;
  width: 520px;
  height: 520px;
  background: hsl(var(--info) / 0.18);
  filter: blur(120px);
  animation: lock-blob-2 22s ease-in-out infinite;
}

.lock-blob--3 {
  left: 18%;
  bottom: -18%;
  width: 580px;
  height: 580px;
  background: hsl(var(--success) / 0.14);
  filter: blur(100px);
  animation: lock-blob-3 20s ease-in-out infinite;
}

@keyframes lock-screen-fade {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@keyframes lock-blob-1 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  33% {
    transform: translate(26%, 16%) scale(1.1);
  }

  66% {
    transform: translate(10%, 32%) scale(0.94);
  }
}

@keyframes lock-blob-2 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  33% {
    transform: translate(-22%, 22%) scale(0.92);
  }

  66% {
    transform: translate(-32%, 42%) scale(1.08);
  }
}

@keyframes lock-blob-3 {
  0%,
  100% {
    transform: translate(0, 0) scale(1);
  }

  33% {
    transform: translate(18%, -28%) scale(1.06);
  }

  66% {
    transform: translate(36%, -14%) scale(0.95);
  }
}

/* 尊重系统的「减少动态效果」：静态光斑同样不透明，不影响遮挡 */
@media (prefers-reduced-motion: reduce) {
  .lock-screen-mask {
    animation: none;
  }

  .lock-blob {
    animation: none;
  }
}

.lock-screen-card {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 40px 48px;
  border-radius: 20px;
  background: hsl(var(--card));
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

.lock-text-btn {
  margin-top: 10px;
  padding: 4px;
  background: transparent;
  border: none;
  font-size: 13px;
  color: hsl(var(--muted-foreground));
  cursor: pointer;
  transition: color 0.15s ease;
}

.lock-text-btn:hover {
  color: hsl(var(--foreground));
}

.lock-text-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>

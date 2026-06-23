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
  unlockPwd,
  unlockError,
  hasLockPwd,
  confirmLock,
  doUnlock,
} = useLockScreen()
</script>

<template>
  <Teleport to="body">
    <div v-if="lockMode !== 'off'" class="lock-screen-mask">
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
              v-if="lockPwdNew"
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
            <button type="submit" class="unlock-btn" style="margin-top: 12px">
              <NIcon size="15" style="vertical-align: -2px; margin-right: 5px">
                <Icon icon="lucide:lock" />
              </NIcon>
              {{ t('component.lock_screen.confirm_lock_btn') }}
            </button>
          </form>
        </template>

        <!-- ② 已锁定，输入锁屏密码 -->
        <template v-else-if="lockMode === 'locked'">
          <div class="lock-screen-hint">
            {{ hasLockPwd ? t('component.lock_screen.input_password_hint') : t('component.lock_screen.press_unlock_hint') }}
          </div>
          <form v-if="hasLockPwd" class="lock-screen-input-wrap" @submit.prevent="doUnlock">
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
            <button type="submit" class="unlock-btn" style="margin-top: 12px">
              <NIcon size="15" style="vertical-align: -2px; margin-right: 5px">
                <Icon icon="lucide:lock-open" />
              </NIcon>
              {{ t('component.lock_screen.unlock_btn') }}
            </button>
          </form>
          <button v-else class="unlock-btn" @click="doUnlock">
            <NIcon size="15" style="vertical-align: -2px; margin-right: 5px">
              <Icon icon="lucide:lock-open" />
            </NIcon>
            {{ t('component.lock_screen.unlock_btn') }}
          </button>
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

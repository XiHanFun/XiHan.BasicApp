<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import { XhButton, XhButtonIndicator, XhButtonLabel, XhButtonPrefix } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { useLockScreen, usePageScrollLock } from '~/composables'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import UserAvatar from './UserAvatar.vue'
import XInput from './XInput.vue'

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
  changePwdOld,
  changePwdNew,
  changePwdConfirm,
  changePwdError,
  changePwdLoading,
  confirmLock,
  cancelLock,
  doUnlock,
  doChangePassword,
  logoutAndRelogin,
} = useLockScreen()

// 锁屏期间背后的内容不该还能滚
usePageScrollLock(() => lockMode.value !== 'off')
</script>

<template>
  <Teleport to="body">
    <div v-if="lockMode !== 'off'" class="lock-screen-mask">
      <!-- 不透明背景：锁屏遮住的内容不该透出来（半透明 + 模糊仍能看清大致版面）
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
            <XInput
              v-model:value="lockPwdNew"
              type="password"
              :placeholder="t('component.lock_screen.new_password_placeholder')"
              size="lg"
              autocomplete="new-password"
            >
              <template #prefix>
                <Icon icon="lucide:key-round" width="16" />
              </template>
            </XInput>
            <XInput
              v-model:value="lockPwdConfirm"
              type="password"
              :placeholder="t('component.lock_screen.confirm_password_placeholder')"
              size="lg"
              style="margin-top: 8px"
              :invalid="Boolean(lockPwdError)"
              autocomplete="new-password"
            >
              <template #prefix>
                <Icon icon="lucide:lock" width="16" />
              </template>
            </XInput>
            <div v-if="lockPwdError" class="lock-screen-error">
              {{ lockPwdError }}
            </div>
            <XhButton type="submit" variant="solid" tone="brand" size="lg" full-width class="lock-screen-submit" :loading="lockLoading">
              <XhButtonIndicator />
              <XhButtonPrefix><Icon icon="lucide:lock" /></XhButtonPrefix>
              <XhButtonLabel>{{ t('component.lock_screen.confirm_lock_btn') }}</XhButtonLabel>
            </XhButton>
            <!-- 此刻还没真正锁上，允许直接退出这个设置框，免得误点锁屏后无路可走 -->
            <XhButton type="button" variant="ghost" size="sm" class="lock-screen-text-action" @click="cancelLock">
              <XhButtonLabel>{{ t('component.lock_screen.cancel_btn') }}</XhButtonLabel>
            </XhButton>
          </form>
        </template>

        <!-- ② 已锁定，输入锁屏密码（口令由服务端校验；无密码解锁与 Esc 解锁已移除——那是绕过口） -->
        <template v-else-if="lockMode === 'locked'">
          <div class="lock-screen-hint">
            {{ t('component.lock_screen.input_password_hint') }}
          </div>
          <form class="lock-screen-input-wrap" @submit.prevent="doUnlock">
            <XInput
              v-model:value="unlockPwd"
              type="password"
              :placeholder="t('component.lock_screen.password_placeholder')"
              size="lg"
              :invalid="Boolean(unlockError)"
              autocomplete="current-password"
            >
              <template #prefix>
                <Icon icon="lucide:key-round" width="16" />
              </template>
            </XInput>
            <div v-if="unlockError" class="lock-screen-error">
              {{ unlockError }}
            </div>
            <XhButton type="submit" variant="solid" tone="brand" size="lg" full-width class="lock-screen-submit" :loading="unlockLoading">
              <XhButtonIndicator />
              <XhButtonPrefix><Icon icon="lucide:lock-open" /></XhButtonPrefix>
              <XhButtonLabel>{{ t('component.lock_screen.unlock_btn') }}</XhButtonLabel>
            </XhButton>
            <!-- 忘了锁屏口令时的出口：结束会话回登录页，而不是把用户永远困在遮罩里 -->
            <XhButton type="button" variant="ghost" size="sm" class="lock-screen-text-action" :loading="logoutLoading" @click="logoutAndRelogin">
              <XhButtonIndicator />
              <XhButtonPrefix><Icon icon="lucide:log-out" /></XhButtonPrefix>
              <XhButtonLabel>{{ t('component.lock_screen.logout_btn') }}</XhButtonLabel>
            </XhButton>
          </form>
        </template>

        <!-- ③ 强制改密：密码由他人设置且开启强制改密时被服务端锁定（仅放行改密/登出/刷新），改密成功自动解锁 -->
        <template v-else-if="lockMode === 'password-change'">
          <div class="lock-screen-hint">
            {{ t('component.lock_screen.change_password_hint') }}
          </div>
          <form class="lock-screen-input-wrap" @submit.prevent="doChangePassword">
            <XInput
              v-model:value="changePwdOld"
              type="password"
              :placeholder="t('component.lock_screen.change_old_password_placeholder')"
              size="lg"
              autocomplete="current-password"
            >
              <template #prefix>
                <Icon icon="lucide:key-round" width="16" />
              </template>
            </XInput>
            <XInput
              v-model:value="changePwdNew"
              type="password"
              :placeholder="t('component.lock_screen.change_new_password_placeholder')"
              size="lg"
              style="margin-top: 8px"
              :invalid="Boolean(changePwdError)"
              autocomplete="new-password"
            >
              <template #prefix>
                <Icon icon="lucide:shield-check" width="16" />
              </template>
            </XInput>
            <XInput
              v-model:value="changePwdConfirm"
              type="password"
              :placeholder="t('component.lock_screen.confirm_password_placeholder')"
              size="lg"
              style="margin-top: 8px"
              :invalid="Boolean(changePwdError)"
              autocomplete="new-password"
            >
              <template #prefix>
                <Icon icon="lucide:shield-check" width="16" />
              </template>
            </XInput>
            <div v-if="changePwdError" class="lock-screen-error">
              {{ changePwdError }}
            </div>
            <XhButton type="submit" variant="solid" tone="brand" size="lg" full-width class="lock-screen-submit" :loading="changePwdLoading">
              <XhButtonIndicator />
              <XhButtonPrefix><Icon icon="lucide:shield-check" /></XhButtonPrefix>
              <XhButtonLabel>{{ t('component.lock_screen.change_password_btn') }}</XhButtonLabel>
            </XhButton>
            <!-- 不想改密的出口：结束会话回登录页（下次登录仍会被锁定，直到改密为止） -->
            <XhButton type="button" variant="ghost" size="sm" class="lock-screen-text-action" :loading="logoutLoading" @click="logoutAndRelogin">
              <XhButtonIndicator />
              <XhButtonPrefix><Icon icon="lucide:log-out" /></XhButtonPrefix>
              <XhButtonLabel>{{ t('component.lock_screen.logout_btn') }}</XhButtonLabel>
            </XhButton>
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
  background: var(--xh-bg-canvas);
  animation: lock-screen-fade var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
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
  inset-inline-start: -10%;
  top: -14%;
  width: 620px;
  height: 620px;
  background: color-mix(in srgb, var(--xh-fg-brand) 22%, transparent);
  filter: blur(100px);
  animation: lock-blob-1 18s var(--xh-motion-ease-sweep) infinite;
}

.lock-blob--2 {
  inset-inline-end: 6%;
  top: -8%;
  width: 520px;
  height: 520px;
  background: color-mix(in srgb, var(--xh-color-info-500) 18%, transparent);
  filter: blur(120px);
  animation: lock-blob-2 22s var(--xh-motion-ease-sweep) infinite;
}

.lock-blob--3 {
  inset-inline-start: 18%;
  bottom: -18%;
  width: 580px;
  height: 580px;
  background: color-mix(in srgb, var(--xh-fg-success) 14%, transparent);
  filter: blur(100px);
  animation: lock-blob-3 20s var(--xh-motion-ease-sweep) infinite;
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
    will-change: auto;
  }
}

.lock-screen-card {
  position: relative;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--xh-space-3);
  padding: calc(var(--xh-space-5) * 2) calc(var(--xh-space-6) * 2);
  border-radius: calc(var(--xh-shape-control) * 3);
  background: var(--xh-bg-surface);
  border: 1px solid var(--xh-border-default);
  text-align: center;
  box-shadow: 0 24px 80px hsl(0 0% 0% / 0.4);
  min-width: 300px;
}

.lock-screen-avatar {
  width: 72px;
  height: 72px;
  border-radius: 50%;
  border: 3px solid var(--xh-border-default);
  object-fit: cover;
}

.lock-screen-name {
  font-size: var(--xh-font-size-lg);
  font-weight: 600;
  color: var(--xh-fg-default);
  line-height: 1.3;
}

.lock-screen-hint {
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-muted);
  margin-bottom: 4px;
}

.lock-screen-input-wrap {
  width: 100%;
  display: flex;
  flex-direction: column;
}

.lock-screen-error {
  margin-top: 6px;
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-danger);
  text-align: left;
}

.lock-screen-submit {
  margin-block-start: var(--xh-space-3);
}

/* 取消、退出登录这类次要出口：按内容收宽、居中，不随表单撑满 */
.lock-screen-text-action {
  align-self: center;
  margin-block-start: var(--xh-space-2);
}
</style>

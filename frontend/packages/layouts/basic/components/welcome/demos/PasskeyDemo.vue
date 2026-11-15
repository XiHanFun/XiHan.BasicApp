<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useSiteStore } from '~/stores'

/**
 * 欢迎页演示：通行密钥。点登录、验证指纹、直接进入，一遍 8 秒循环播放
 * 整段是一条时间线，各部件按同一个时长的关键帧各自出场，不用脚本编排
 */
defineOptions({ name: 'WelcomePasskeyDemo' })

const { t } = useI18n()
const site = useSiteStore()
const appLogo = computed(() => site.logo)
</script>

<template>
  <div class="demo">
    <div class="login">
      <img :src="appLogo" alt="" class="login__logo">
      <strong>{{ t('welcome.passkey.demo_welcome_back') }}</strong>
      <div class="login__field">
        <Icon icon="lucide:user" />
        <span>admin</span>
      </div>
      <div class="login__slot">
        <div class="login__button">
          <Icon icon="lucide:key-round" />
          {{ t('welcome.passkey.demo_sign_in') }}
        </div>
        <div class="login__done">
          <Icon icon="lucide:check" />
          {{ t('welcome.passkey.demo_signed_in') }}
        </div>
      </div>
      <small>{{ t('welcome.passkey.demo_no_password') }}</small>
    </div>

    <div class="sheet">
      <div class="print">
        <svg class="print__ring" viewBox="0 0 100 100">
          <circle class="print__track" cx="50" cy="50" r="46" />
          <circle class="print__progress" cx="50" cy="50" r="46" pathLength="100" />
        </svg>
        <Icon icon="lucide:fingerprint" class="print__icon" />
        <Icon icon="lucide:check" class="print__check" />
        <span class="print__scan" />
      </div>
      <div class="sheet__text">
        <div class="sheet__ask">
          <strong>{{ t('welcome.passkey.demo_verify') }}</strong>
          <small>{{ t('welcome.passkey.demo_touch') }}</small>
        </div>
        <div class="sheet__ok">
          <strong>{{ t('welcome.passkey.demo_verified') }}</strong>
          <small>{{ t('welcome.passkey.demo_no_password') }}</small>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.demo {
  position: relative;
  display: grid;
  place-items: center;
  block-size: 100%;
  overflow: hidden;
  padding: var(--xh-space-6);
}

/* 登录卡片 */
.login {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--xh-space-3);
  inline-size: 58%;
  margin-block-end: calc(var(--xh-space-8) * 1.2);
  padding: var(--xh-space-6) var(--xh-space-5) var(--xh-space-5);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-sheet);
  background-color: var(--xh-bg-surface);
  box-shadow: var(--xh-shadow-xl);
  animation: passkey-login 8s ease infinite;
}

.login__logo {
  inline-size: calc(var(--xh-space-8) * 1.4);
  block-size: calc(var(--xh-space-8) * 1.4);
  object-fit: contain;
}

.login > strong {
  font-size: var(--xh-font-size-lg);
}

.login > small {
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
}

.login__field {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  inline-size: 100%;
  block-size: calc(var(--xh-space-8) + var(--xh-space-2));
  padding-inline: var(--xh-space-3);
  border: 1px solid var(--xh-border-control);
  border-radius: var(--xh-shape-control);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-md);
}

.login__field span {
  color: var(--xh-fg-default);
}

.login__slot {
  position: relative;
  inline-size: 100%;
  block-size: calc(var(--xh-space-8) + var(--xh-space-2));
}

.login__button,
.login__done {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: var(--xh-space-2);
  border-radius: var(--xh-shape-control);
  font-size: var(--xh-font-size-md);
  font-weight: var(--xh-font-weight-semibold);
}

.login__button {
  background-color: var(--xh-bg-brand);
  color: var(--xh-fg-on-brand);
  animation: passkey-button 8s ease infinite;
}

.login__done {
  background-color: color-mix(in oklab, var(--xh-fg-success) 16%, transparent);
  color: var(--xh-fg-success);
  opacity: 0;
  animation: passkey-done 8s ease infinite;
}

/* 系统弹出的验证面板 */
.sheet {
  position: absolute;
  inset-block-end: var(--xh-space-6);
  inset-inline: 13%;
  display: flex;
  align-items: center;
  gap: var(--xh-space-5);
  padding: var(--xh-space-5);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-sheet);
  background-color: var(--xh-bg-surface-raised);
  box-shadow: var(--xh-shadow-xl);
  transform: translateY(160%);
  animation: passkey-sheet 8s var(--xh-motion-ease-enter-strong) infinite;
}

.print {
  position: relative;
  flex: none;
  display: grid;
  place-items: center;
  inline-size: calc(var(--xh-space-8) * 2.4);
  block-size: calc(var(--xh-space-8) * 2.4);
}

.print > * {
  position: absolute;
}

.print__ring {
  inset: 0;
  inline-size: 100%;
  block-size: 100%;
  transform: rotate(-90deg);
}

.print__ring circle {
  fill: none;
  stroke-width: 5;
}

.print__track {
  stroke: var(--xh-border-default);
}

.print__progress {
  stroke: var(--xh-bg-brand);
  stroke-linecap: round;
  stroke-dasharray: 100;
  stroke-dashoffset: 100;
  animation: passkey-ring 8s ease infinite;
}

.print__icon {
  color: var(--xh-fg-brand);
  font-size: var(--xh-font-size-5xl);
  animation: passkey-print 8s ease infinite;
}

.print__check {
  color: var(--xh-fg-success);
  font-size: var(--xh-font-size-5xl);
  opacity: 0;
  animation: passkey-check 8s ease infinite;
}

.print__scan {
  inset-block-start: 20%;
  inset-inline: 22%;
  block-size: 2px;
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-brand);
  opacity: 0;
  animation: passkey-scan 8s ease-in-out infinite;
}

.sheet__text {
  position: relative;
  flex: 1;
  block-size: calc(var(--xh-space-8) * 1.5);
}

.sheet__ask,
.sheet__ok {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: var(--xh-space-0_5);
}

.sheet__text strong {
  font-size: var(--xh-font-size-lg);
}

.sheet__text small {
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-sm);
}

.sheet__ask {
  animation: passkey-ask 8s ease infinite;
}

.sheet__ok {
  opacity: 0;
  animation: passkey-ok 8s ease infinite;
}

.sheet__ok strong {
  color: var(--xh-fg-success);
}

/* 时间线（一遍 8 秒）：点按钮 → 面板升起 → 扫指纹 → 打勾 → 面板落下 → 卡片显示已登录 */
@keyframes passkey-sheet {
  0%,
  14% {
    transform: translateY(160%);
  }

  21%,
  60% {
    transform: none;
  }

  68%,
  100% {
    transform: translateY(160%);
  }
}

@keyframes passkey-login {
  0%,
  14%,
  68%,
  100% {
    opacity: 1;
    transform: none;
  }

  21%,
  60% {
    opacity: 0.45;
    transform: scale(0.965);
  }
}

@keyframes passkey-button {
  0%,
  9%,
  14%,
  62% {
    opacity: 1;
    transform: none;
  }

  11.5% {
    transform: scale(0.94);
  }

  66%,
  95% {
    opacity: 0;
  }

  100% {
    opacity: 1;
  }
}

@keyframes passkey-done {
  0%,
  62%,
  100% {
    opacity: 0;
  }

  66%,
  95% {
    opacity: 1;
  }
}

@keyframes passkey-ring {
  0%,
  22% {
    stroke: var(--xh-bg-brand);
    stroke-dashoffset: 100;
  }

  42% {
    stroke: var(--xh-bg-brand);
    stroke-dashoffset: 0;
  }

  45%,
  100% {
    stroke: var(--xh-fg-success);
    stroke-dashoffset: 0;
  }
}

@keyframes passkey-scan {
  0%,
  22%,
  43%,
  100% {
    inset-block-start: 20%;
    opacity: 0;
  }

  24%,
  41% {
    inset-block-start: 20%;
    opacity: 1;
  }

  32% {
    inset-block-start: 78%;
    opacity: 1;
  }
}

@keyframes passkey-print {
  0%,
  42% {
    opacity: 1;
    transform: none;
  }

  46%,
  100% {
    opacity: 0;
    transform: scale(0.7);
  }
}

@keyframes passkey-check {
  0%,
  43% {
    opacity: 0;
    transform: scale(0.6);
  }

  49%,
  100% {
    opacity: 1;
    transform: none;
  }
}

@keyframes passkey-ask {
  0%,
  42% {
    opacity: 1;
  }

  45%,
  100% {
    opacity: 0;
  }
}

@keyframes passkey-ok {
  0%,
  44% {
    opacity: 0;
    transform: translateY(var(--xh-space-1_5));
  }

  48%,
  100% {
    opacity: 1;
    transform: none;
  }
}
</style>

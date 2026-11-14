<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useSequence } from '~/composables'
import { Icon } from '~/iconify'

/**
 * 欢迎页演示：聊天。一来一回几条消息（引用与提及），接着起一通语音、再共享屏幕，循环播放
 */
defineOptions({ name: 'ChatWelcomeDemo' })

const { t } = useI18n()
/** 演到第几步：1 来消息，2 回复，3 对方在输入，4 对方回话，5 通话，6 共享屏幕 */
const step = ref(0)
const fading = ref(false)

const loop = useSequence(async (wait) => {
  fading.value = false
  step.value = 0
  await wait(400)
  step.value = 1
  await wait(1300)
  step.value = 2
  await wait(1500)
  step.value = 3
  await wait(1300)
  step.value = 4
  await wait(1100)
  step.value = 5
  await wait(1400)
  step.value = 6
  await wait(3200)
  fading.value = true
  await wait(450)
})

onMounted(loop.start)
</script>

<template>
  <div class="demo">
    <div class="chat" :class="{ 'is-fading': fading }">
      <aside class="chat__list">
        <div class="chat__peer is-on">
          <span class="chat__avatar">{{ t('chat.welcome.demo_peer').slice(0, 1) }}</span>
          <p><b /><i /></p>
          <em>2</em>
        </div>
        <div v-for="n in 3" :key="n" class="chat__peer">
          <span class="chat__avatar" :data-tone="n" />
          <p><b /><i /></p>
        </div>
        <Transition name="chat-pop">
          <div v-if="step >= 6" class="chat__share">
            <div class="chat__screen">
              <i /><i /><i />
              <Icon icon="lucide:mouse-pointer-2" class="chat__cursor" />
            </div>
            <small>{{ t('chat.welcome.demo_sharing') }}</small>
          </div>
        </Transition>
      </aside>

      <div class="chat__pane">
        <header class="chat__head">
          <span class="chat__avatar">{{ t('chat.welcome.demo_peer').slice(0, 1) }}</span>
          <strong>{{ t('chat.welcome.demo_peer') }}</strong>
          <small>{{ t('chat.welcome.demo_online') }}</small>
          <em />
          <span class="chat__tool"><Icon icon="lucide:phone" /></span>
          <span class="chat__tool"><Icon icon="lucide:video" /></span>
          <span class="chat__tool"><Icon icon="lucide:monitor-up" /></span>
        </header>

        <Transition name="chat-drop">
          <div v-if="step >= 5" class="chat__call">
            <span class="chat__avatar">{{ t('chat.welcome.demo_peer').slice(0, 1) }}</span>
            <span class="chat__call-text">
              <strong>{{ t('chat.welcome.demo_calling') }}</strong>
              <small>00:07</small>
            </span>
            <span class="chat__wave"><i /><i /><i /><i /><i /></span>
            <span class="chat__hang"><Icon icon="lucide:phone-off" /></span>
          </div>
        </Transition>

        <div class="chat__messages">
          <TransitionGroup name="chat-pop">
            <div v-if="step >= 1" key="ask" class="chat__message">
              <span>{{ t('chat.welcome.demo_ask') }}</span>
            </div>
            <div v-if="step >= 2" key="reply" class="chat__message is-mine">
              <span>
                <q>{{ t('chat.welcome.demo_ask') }}</q>
                {{ t('chat.welcome.demo_reply') }}
                <b>{{ `@${t('chat.welcome.demo_peer')}` }}</b>
              </span>
            </div>
            <div v-if="step >= 3" key="answer" class="chat__message">
              <span v-if="step === 3" class="chat__typing"><b /><b /><b /></span>
              <span v-else>{{ t('chat.welcome.demo_answer') }}</span>
            </div>
          </TransitionGroup>
        </div>

        <footer class="chat__input">
          <span />
          <b><Icon icon="lucide:send" /></b>
        </footer>
      </div>
    </div>
  </div>
</template>

<style scoped>
.demo {
  block-size: 100%;
  padding: var(--xh-space-6);
}

.chat {
  position: relative;
  display: flex;
  block-size: 100%;
  overflow: hidden;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-canvas);
  box-shadow: var(--xh-shadow-xl);
}

/* 一遍演完，消息与通话一起淡掉再重来 */
.chat__messages,
.chat__call,
.chat__share {
  transition: opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

.chat.is-fading .chat__messages,
.chat.is-fading .chat__call,
.chat.is-fading .chat__share {
  opacity: 0;
}

.chat__list {
  position: relative;
  flex: 0 0 31%;
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1);
  padding: var(--xh-space-2);
  border-inline-end: 1px solid var(--xh-border-default);
  background-color: var(--xh-bg-surface);
}

.chat__peer {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  padding: var(--xh-space-2);
  border-radius: var(--xh-shape-control);
}

.chat__peer.is-on {
  background-color: var(--xh-bg-brand-subtle);
}

.chat__peer p {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-1_5);
  min-inline-size: 0;
  margin: 0;
}

.chat__peer p b {
  inline-size: 58%;
  block-size: var(--xh-space-2);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-fg-default);
  opacity: 0.7;
}

.chat__peer p i {
  inline-size: 88%;
  block-size: var(--xh-space-1_5);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-bg-subtle-active);
}

.chat__peer em {
  display: grid;
  place-items: center;
  min-inline-size: var(--xh-space-5);
  block-size: var(--xh-space-5);
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-fg-danger);
  color: var(--xh-fg-on-brand);
  font-size: var(--xh-font-size-xs);
  font-style: normal;
  font-weight: var(--xh-font-weight-semibold);
}

.chat__avatar {
  flex: none;
  display: grid;
  place-items: center;
  inline-size: var(--xh-space-8);
  block-size: var(--xh-space-8);
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-bg-brand);
  color: var(--xh-fg-on-brand);
  font-size: var(--xh-font-size-sm);
  font-weight: var(--xh-font-weight-semibold);
}

.chat__avatar[data-tone='1'] {
  background-color: var(--xh-fg-success);
}

.chat__avatar[data-tone='2'] {
  background-color: var(--xh-fg-warning);
}

.chat__avatar[data-tone='3'] {
  background-color: var(--xh-fg-info);
}

.chat__pane {
  position: relative;
  flex: 1;
  display: flex;
  flex-direction: column;
  min-inline-size: 0;
}

.chat__head {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  block-size: calc(var(--xh-space-8) * 1.5);
  padding-inline: var(--xh-space-3);
  border-block-end: 1px solid var(--xh-border-default);
  background-color: var(--xh-bg-surface);
}

.chat__head strong {
  font-size: var(--xh-font-size-md);
}

.chat__head small {
  color: var(--xh-fg-success);
  font-size: var(--xh-font-size-xs);
}

.chat__head em {
  flex: 1;
}

.chat__tool {
  display: grid;
  place-items: center;
  inline-size: var(--xh-space-7);
  block-size: var(--xh-space-7);
  border-radius: var(--xh-shape-control);
  background-color: var(--xh-bg-subtle);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-sm);
}

/* 通话条：任何主题下都是深色的一条，颜色取媒体控件那一族 */
.chat__call {
  position: absolute;
  inset-block-start: calc(var(--xh-space-8) * 1.5 + var(--xh-space-2));
  inset-inline: var(--xh-space-3);
  z-index: 1;
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
  block-size: calc(var(--xh-space-8) * 1.7);
  padding-inline: var(--xh-space-3);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-media-chrome);
  box-shadow: var(--xh-shadow-xl);
  color: var(--xh-fg-on-media-chrome);
}

.chat__call-text {
  display: flex;
  flex-direction: column;
  line-height: var(--xh-leading-tight);
}

.chat__call-text strong {
  font-size: var(--xh-font-size-sm);
}

.chat__call-text small {
  font-size: var(--xh-font-size-xs);
  font-variant-numeric: tabular-nums;
  opacity: 0.6;
}

.chat__wave {
  display: flex;
  align-items: center;
  gap: var(--xh-space-0_5);
  block-size: var(--xh-space-5);
  margin-inline-start: auto;
}

.chat__wave i {
  inline-size: var(--xh-space-0_5);
  block-size: 30%;
  border-radius: var(--xh-shape-pill);
  background-color: var(--xh-fg-success);
  animation: chat-wave 1s ease-in-out infinite;
}

.chat__wave i:nth-child(2) {
  animation-delay: -0.2s;
}

.chat__wave i:nth-child(3) {
  animation-delay: -0.45s;
}

.chat__wave i:nth-child(4) {
  animation-delay: -0.7s;
}

.chat__wave i:nth-child(5) {
  animation-delay: -0.3s;
}

@keyframes chat-wave {
  50% {
    block-size: 100%;
  }
}

.chat__hang {
  display: grid;
  place-items: center;
  inline-size: var(--xh-space-8);
  block-size: var(--xh-space-8);
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-fg-danger);
  font-size: var(--xh-font-size-sm);
}

.chat__messages {
  flex: 1;
  display: flex;
  flex-direction: column;
  justify-content: flex-end;
  gap: var(--xh-space-2_5);
  min-block-size: 0;
  padding: var(--xh-space-4) var(--xh-space-3);
}

.chat__message {
  display: flex;
  transform-origin: left bottom;
}

.chat__message.is-mine {
  justify-content: flex-end;
  transform-origin: right bottom;
}

.chat__message > span {
  max-inline-size: 82%;
  padding: var(--xh-space-2) var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-surface);
  background-color: var(--xh-bg-surface);
  font-size: var(--xh-font-size-md);
  line-height: var(--xh-leading-normal);
}

.chat__message.is-mine > span {
  border-color: transparent;
  background-color: var(--xh-bg-brand);
  color: var(--xh-fg-on-brand);
}

.chat__message q {
  display: block;
  margin-block-end: var(--xh-space-1_5);
  padding: var(--xh-space-1) var(--xh-space-2);
  border-inline-start: 2px solid currentColor;
  border-radius: var(--xh-shape-inset);
  background-color: color-mix(in oklab, currentColor 16%, transparent);
  font-size: var(--xh-font-size-sm);
  opacity: 0.9;
  quotes: none;
}

.chat__message b {
  padding-inline: var(--xh-space-1);
  border-radius: var(--xh-shape-inset);
  background-color: color-mix(in oklab, currentColor 22%, transparent);
  font-weight: var(--xh-font-weight-semibold);
}

.chat__typing {
  display: flex;
  gap: var(--xh-space-1);
}

.chat__typing b {
  inline-size: var(--xh-space-1_5);
  block-size: var(--xh-space-1_5);
  padding: 0;
  border-radius: var(--xh-shape-circle);
  background-color: var(--xh-fg-muted);
  animation: chat-bounce 0.9s ease-in-out infinite;
}

.chat__typing b:nth-child(2) {
  animation-delay: 0.15s;
}

.chat__typing b:nth-child(3) {
  animation-delay: 0.3s;
}

@keyframes chat-bounce {
  30% {
    transform: translateY(calc(var(--xh-space-1) * -1));
  }
}

.chat__input {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2);
  block-size: calc(var(--xh-space-8) * 1.5);
  padding-inline: var(--xh-space-3);
  border-block-start: 1px solid var(--xh-border-default);
  background-color: var(--xh-bg-surface);
}

.chat__input span {
  flex: 1;
  block-size: var(--xh-space-7);
  border-radius: var(--xh-shape-control);
  background-color: var(--xh-bg-subtle);
}

.chat__input b {
  display: grid;
  place-items: center;
  inline-size: var(--xh-space-7);
  block-size: var(--xh-space-7);
  border-radius: var(--xh-shape-control);
  background-color: var(--xh-bg-brand);
  color: var(--xh-fg-on-brand);
  font-size: var(--xh-font-size-sm);
}

/* 共享屏幕的小窗，压在会话列表的下方 */
.chat__share {
  position: absolute;
  inset-block-end: var(--xh-space-2);
  inset-inline: var(--xh-space-2);
  padding: var(--xh-space-1_5);
  border-radius: var(--xh-shape-control);
  background-color: var(--xh-bg-media-chrome);
  box-shadow: var(--xh-shadow-xl);
  color: var(--xh-fg-on-media-chrome);
  transform-origin: left bottom;
}

.chat__screen {
  position: relative;
  display: grid;
  align-content: start;
  gap: var(--xh-space-1);
  aspect-ratio: 16 / 10;
  padding: var(--xh-space-2);
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-media-chrome-hover);
}

.chat__screen i {
  block-size: var(--xh-space-1);
  border-radius: var(--xh-shape-pill);
  background-color: currentColor;
  opacity: 0.3;
}

.chat__screen i:nth-child(2) {
  inline-size: 70%;
}

.chat__screen i:nth-child(3) {
  inline-size: 45%;
  background-color: var(--xh-bg-brand);
  opacity: 1;
}

.chat__cursor {
  position: absolute;
  inset-block-end: 18%;
  inset-inline-end: 22%;
  font-size: var(--xh-font-size-sm);
  animation: chat-cursor 2.4s ease-in-out infinite;
}

@keyframes chat-cursor {
  50% {
    transform: translate(calc(var(--xh-space-6) * -1), calc(var(--xh-space-3) * -1));
  }
}

.chat__share small {
  display: block;
  margin-block-start: var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  opacity: 0.85;
}

.chat-pop-enter-active {
  transition:
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-reveal) var(--xh-motion-ease-settle);
}

.chat-pop-enter-from {
  opacity: 0;
  transform: translateY(var(--xh-space-3)) scale(0.88);
}

.chat-drop-enter-active {
  transition:
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-reveal) var(--xh-motion-ease-settle);
}

.chat-drop-enter-from {
  opacity: 0;
  transform: translateY(calc(var(--xh-space-4) * -1)) scale(0.94);
}
</style>

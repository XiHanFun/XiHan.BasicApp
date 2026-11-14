<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhKbd } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { GLOBAL_HOTKEYS, useSequence } from '~/composables'
import { Icon } from '~/iconify'

/**
 * 欢迎页演示：命令面板。敲几个字、结果筛出来、回车直达，循环播放
 */
defineOptions({ name: 'WelcomeCommandDemo' })

interface Row {
  icon: string
  title: string
  kind: string
}

const { t } = useI18n()
/** 已经敲进去的字数 */
const typed = ref(0)
const active = ref(0)
const hit = ref(false)
const closed = ref(false)

const keyword = computed(() => t('welcome.command.demo_query'))
const query = computed(() => keyword.value.slice(0, typed.value))
const searching = computed(() => typed.value >= keyword.value.length)

const recent = computed<Row[]>(() => [
  {
    icon: 'lucide:layout-dashboard',
    title: t('welcome.command.demo_dashboard'),
    kind: t('welcome.command.demo_page'),
  },
  {
    icon: 'lucide:users',
    title: t('welcome.command.demo_users'),
    kind: t('welcome.command.demo_page'),
  },
  {
    icon: 'lucide:file-text',
    title: t('welcome.command.demo_logs'),
    kind: t('welcome.command.demo_page'),
  },
])
const matched = computed<Row[]>(() => [
  {
    icon: 'lucide:building-2',
    title: t('welcome.command.demo_tenants'),
    kind: t('welcome.command.demo_page'),
  },
  {
    icon: 'lucide:user-plus',
    title: t('welcome.command.demo_members'),
    kind: t('welcome.command.demo_page'),
  },
  {
    icon: 'lucide:repeat',
    title: t('welcome.command.demo_switch'),
    kind: t('welcome.command.demo_action'),
  },
])
const rows = computed(() => (searching.value ? matched.value : recent.value))

/** 把标题按关键词拆成三段，中间那段高亮；标题里没有这个词就整段原样 */
function split(title: string): [string, string, string] {
  if (!searching.value)
    return [title, '', '']
  const at = title.toLowerCase().indexOf(keyword.value.toLowerCase())
  return at < 0
    ? [title, '', '']
    : [
        title.slice(0, at),
        title.slice(at, at + keyword.value.length),
        title.slice(at + keyword.value.length),
      ]
}

const loop = useSequence(async (wait) => {
  closed.value = false
  hit.value = false
  typed.value = 0
  active.value = 0
  await wait(1400)
  for (let n = 1; n <= keyword.value.length; n++) {
    typed.value = n
    await wait(140)
  }
  await wait(900)
  active.value = 1
  await wait(520)
  active.value = 2
  await wait(750)
  hit.value = true
  await wait(500)
  closed.value = true
  await wait(1200)
})

onMounted(loop.start)
</script>

<template>
  <div class="demo" :class="{ 'is-closed': closed }">
    <XhKbd class="demo__keys" :keys="[...GLOBAL_HOTKEYS.search]" />
    <div class="cmd">
      <div class="cmd__input">
        <Icon icon="lucide:search" class="cmd__search" />
        <span>{{ query }}</span>
        <i class="cmd__caret" />
        <span v-if="typed === 0" class="cmd__placeholder">{{ t('welcome.command.demo_placeholder') }}</span>
        <em />
        <XhKbd :keys="['Escape']" />
      </div>
      <div class="cmd__group">
        {{ searching ? t('welcome.command.demo_found', { count: matched.length }) : t('welcome.command.demo_recent') }}
      </div>
      <div class="cmd__list">
        <div
          v-for="(row, n) in rows"
          :key="row.title"
          class="cmd__row"
          :class="{ 'is-on': n === active, 'is-hit': hit && n === active }"
          :style="{ '--cmd-order': n }"
        >
          <span class="cmd__icon"><Icon :icon="row.icon" /></span>
          <span>{{ split(row.title)[0] }}<mark>{{ split(row.title)[1] }}</mark>{{ split(row.title)[2] }}</span>
          <small>{{ row.kind }}</small>
        </div>
      </div>
      <div class="cmd__foot">
        <span><XhKbd :keys="['ArrowUp']" /><XhKbd :keys="['ArrowDown']" />{{ t('welcome.command.demo_select') }}</span>
        <span><XhKbd :keys="['Enter']" />{{ t('welcome.command.demo_open') }}</span>
      </div>
    </div>
  </div>
</template>

<style scoped>
.demo {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: var(--xh-space-5);
  block-size: 100%;
  padding: var(--xh-space-6);
}

.cmd {
  inline-size: 86%;
  overflow: hidden;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-overlay);
  background-color: var(--xh-bg-surface-raised);
  box-shadow: var(--xh-shadow-xl);
  transition:
    opacity var(--xh-motion-duration-slide) var(--xh-motion-ease-enter),
    transform var(--xh-motion-duration-slide) var(--xh-motion-ease-enter);
}

/* 回车之后面板收起，下一轮再打开 */
.demo.is-closed .cmd {
  opacity: 0;
  transform: translateY(var(--xh-space-2)) scale(0.96);
}

.cmd__input {
  display: flex;
  align-items: center;
  block-size: calc(var(--xh-space-8) * 1.6);
  padding-inline: var(--xh-space-4);
  border-block-end: 1px solid var(--xh-border-default);
  font-size: var(--xh-font-size-lg);
}

.cmd__input em {
  flex: 1;
}

.cmd__search {
  margin-inline-end: var(--xh-space-2_5);
  color: var(--xh-fg-muted);
}

.cmd__caret {
  inline-size: 2px;
  block-size: 1.15em;
  background-color: var(--xh-bg-brand);
  animation: cmd-blink 1s steps(2, jump-none) infinite;
}

@keyframes cmd-blink {
  50% {
    opacity: 0;
  }
}

.cmd__placeholder,
.cmd__group,
.cmd__foot,
.cmd__row small {
  color: var(--xh-fg-muted);
}

.cmd__group {
  padding: var(--xh-space-3) var(--xh-space-5) var(--xh-space-1_5);
  font-size: var(--xh-font-size-xs);
}

.cmd__list {
  min-block-size: calc(var(--xh-space-8) * 4.2);
  padding-block-end: var(--xh-space-2);
}

.cmd__row {
  display: flex;
  align-items: center;
  gap: var(--xh-space-3);
  block-size: calc(var(--xh-space-8) + var(--xh-space-2_5));
  margin-inline: var(--xh-space-2);
  padding-inline: var(--xh-space-3);
  border-radius: var(--xh-shape-inset);
  font-size: var(--xh-font-size-md);
  animation: cmd-row-in var(--xh-motion-duration-slide) var(--xh-motion-ease-enter-strong) backwards;
  animation-delay: calc(var(--cmd-order) * var(--xh-motion-duration-micro) * 0.4);
  transition: background-color var(--xh-motion-duration-micro) var(--xh-motion-ease-enter);
}

@keyframes cmd-row-in {
  from {
    opacity: 0;
    transform: translateY(var(--xh-space-2));
  }
}

.cmd__row small {
  margin-inline-start: auto;
  font-size: var(--xh-font-size-xs);
}

.cmd__row mark {
  background-color: transparent;
  color: var(--xh-fg-brand);
  font-weight: var(--xh-font-weight-semibold);
}

.cmd__icon {
  display: grid;
  place-items: center;
  inline-size: var(--xh-space-7);
  block-size: var(--xh-space-7);
  border-radius: var(--xh-shape-inset);
  background-color: var(--xh-bg-subtle);
  color: var(--xh-fg-muted);
}

.cmd__row.is-on {
  background-color: var(--xh-bg-brand-subtle);
}

.cmd__row.is-on .cmd__icon {
  background-color: var(--xh-bg-brand);
  color: var(--xh-fg-on-brand);
}

.cmd__row.is-hit {
  background-color: color-mix(in oklab, var(--xh-bg-brand) 40%, transparent);
}

.cmd__foot {
  display: flex;
  gap: var(--xh-space-5);
  padding: var(--xh-space-2_5) var(--xh-space-5);
  border-block-start: 1px solid var(--xh-border-default);
  font-size: var(--xh-font-size-xs);
}

.cmd__foot span {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
}
</style>

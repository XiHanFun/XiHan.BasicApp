<script lang="ts" setup>
import { NDropdown, NIcon } from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { useSplitViewStore, useTabbarStore } from '~/stores'

defineOptions({ name: 'SplitPane' })

const splitView = useSplitViewStore()
const tabbarStore = useTabbarStore()
const router = useRouter()
const { t, te } = useI18n()

/** iframe reload 令牌：变更 src 触发重载 */
const reloadKey = ref(0)

function tr(title: string): string {
  return te(title) ? t(title) : title
}

/** 右侧 pane 的 iframe 源：目标路径 + 内容-only 模式（hash 路由） */
const paneSrc = computed(() => {
  const path = splitView.rightPath
  if (!path) {
    return ''
  }
  const base = `${window.location.origin}${window.location.pathname}`
  const sep = path.includes('?') ? '&' : '?'
  return `${base}#${path}${sep}__pane=1&__k=${reloadKey.value}`
})

const paneTitle = computed(() => {
  const tab = tabbarStore.tabs.find(item => item.path === splitView.rightPath)
  return tr(tab?.title ?? splitView.rightPath)
})

// 可切换的右侧标签：其它已打开标签（排除左侧锚定标签）
const tabOptions = computed(() =>
  tabbarStore.tabs
    .filter(item => item.path !== splitView.leftPath)
    .map(item => ({ key: item.path, label: tr(item.title) })),
)

function onSelect(key: string): void {
  splitView.setRightPath(key)
}

function reload(): void {
  reloadKey.value += 1
}

function openInMain(): void {
  const path = splitView.rightPath
  splitView.close()
  if (path) {
    void router.push(path)
  }
}
</script>

<template>
  <div class="split-pane">
    <div class="split-pane__head">
      <NDropdown trigger="click" :options="tabOptions" @select="onSelect">
        <button type="button" class="split-pane__title" :title="paneTitle">
          <NIcon size="14" class="shrink-0 opacity-70">
            <Icon icon="lucide:columns-2" />
          </NIcon>
          <span class="truncate">{{ paneTitle }}</span>
          <NIcon size="13" class="shrink-0 opacity-50">
            <Icon icon="lucide:chevron-down" />
          </NIcon>
        </button>
      </NDropdown>
      <div class="split-pane__actions">
        <button type="button" class="split-pane__btn" :title="t('tabbar.reload')" @click="reload">
          <NIcon size="14">
            <Icon icon="lucide:rotate-cw" />
          </NIcon>
        </button>
        <button type="button" class="split-pane__btn" :title="t('tabbar.open')" @click="openInMain">
          <NIcon size="14">
            <Icon icon="lucide:external-link" />
          </NIcon>
        </button>
        <button type="button" class="split-pane__btn" :title="t('tabbar.split_close')" @click="splitView.close()">
          <NIcon size="15">
            <Icon icon="lucide:x" />
          </NIcon>
        </button>
      </div>
    </div>
    <iframe
      v-if="paneSrc"
      :src="paneSrc"
      class="split-pane__frame"
      title="split-pane"
    />
  </div>
</template>

<style scoped>
.split-pane {
  display: flex;
  flex-direction: column;
  height: 100%;
  width: 100%;
  background: hsl(var(--background));
  border-left: 1px solid hsl(var(--border));
}

.split-pane__head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  height: 34px;
  padding: 0 6px 0 10px;
  flex-shrink: 0;
  border-bottom: 1px solid hsl(var(--border));
  background: hsl(var(--header));
}

.split-pane__title {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  min-width: 0;
  max-width: 70%;
  height: 26px;
  padding: 0 8px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--foreground));
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
}

.split-pane__title:hover {
  background: hsl(var(--accent));
}

.split-pane__actions {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}

.split-pane__btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 26px;
  height: 26px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: hsl(var(--foreground) / 65%);
  cursor: pointer;
  transition: background 0.15s ease;
}

.split-pane__btn:hover {
  background: hsl(var(--accent));
  color: hsl(var(--foreground));
}

.split-pane__frame {
  flex: 1;
  width: 100%;
  border: none;
  background: hsl(var(--background));
}
</style>

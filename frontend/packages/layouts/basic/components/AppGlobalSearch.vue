<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NEmpty, NIcon, NInput, NModal, NScrollbar } from 'naive-ui'
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { useAppStore } from '~/stores'

defineOptions({ name: 'AppGlobalSearch', inheritAttrs: false })

const router = useRouter()
const { t } = useI18n()
const appStore = useAppStore()
const showShortcut = computed(() => appStore.shortcutEnable && appStore.shortcutSearch)
const visible = ref(false)
const keyword = ref('')

const routeItems = computed(() => {
  const routes = router
    .getRoutes()
    .filter(item => item.meta?.title && !item.meta?.hidden && item.path && item.name)
    .map(item => ({
      name: String(item.name),
      path: item.path,
      title: t(String(item.meta.title), String(item.meta.title)),
      icon: item.meta.icon as string | undefined,
    }))

  const text = keyword.value.trim().toLowerCase()
  if (!text)
    return routes.slice(0, 20)
  return routes.filter(
    item =>
      item.title.toLowerCase().includes(text)
      || item.path.toLowerCase().includes(text)
      || item.name.toLowerCase().includes(text),
  )
})

function openSearch() {
  visible.value = true
}

function jumpTo(path: string) {
  visible.value = false
  keyword.value = ''
  router.push(path)
}

function handleOpenEvent() {
  openSearch()
}

onMounted(() => {
  window.addEventListener('xihan-open-global-search', handleOpenEvent)
})

onUnmounted(() => {
  window.removeEventListener('xihan-open-global-search', handleOpenEvent)
})
</script>

<template>
  <!-- 触发按钮容器：$attrs 挂到此处（如外部传 class="mr-1"） -->
  <div v-bind="$attrs">
    <!-- 桌面端：胶囊样式，含搜索图标 + 文字 + kbd 徽标 -->
    <div class="hidden sm:block">
      <button type="button" class="search-trigger" @click="openSearch">
        <NIcon size="14" class="shrink-0 text-[hsl(var(--muted-foreground))]">
          <Icon icon="lucide:search" />
        </NIcon>
        <span class="search-trigger-text">{{ t('header.search.placeholder') }}</span>
        <kbd v-if="showShortcut" class="search-kbd">Ctrl K</kbd>
      </button>
    </div>
    <!-- 移动端：只显示图标按钮 -->
    <div class="sm:hidden">
      <button type="button" class="search-trigger-icon" @click="openSearch">
        <NIcon size="16"><Icon icon="lucide:search" /></NIcon>
      </button>
    </div>
  </div>

  <NModal v-model:show="visible" preset="card" :bordered="false" class="w-[580px]" :on-after-leave="() => keyword = ''">
    <template #header>
      <NInput
        v-model:value="keyword"
        :placeholder="t('header.search.input_hint')"
        clearable
        autofocus
      >
        <template #prefix>
          <NIcon size="15" class="text-[hsl(var(--muted-foreground))]">
            <Icon icon="lucide:search" />
          </NIcon>
        </template>
      </NInput>
    </template>
    <NScrollbar style="max-height: 400px">
      <div v-if="routeItems.length" class="space-y-1">
        <button
          v-for="item in routeItems"
          :key="item.name"
          type="button"
          class="search-result-item"
          @click="jumpTo(item.path)"
        >
          <span class="flex items-center gap-2">
            <NIcon size="16" class="text-[hsl(var(--muted-foreground))]">
              <Icon v-if="item.icon" :icon="item.icon" />
              <Icon v-else icon="lucide:file" />
            </NIcon>
            <span class="text-sm text-[hsl(var(--foreground))]">{{ item.title }}</span>
          </span>
          <span class="text-xs text-[hsl(var(--muted-foreground))]">{{ item.path }}</span>
        </button>
      </div>
      <NEmpty v-else :description="t('header.search.empty')" class="py-8" />
    </NScrollbar>
  </NModal>
</template>

<style scoped>
/* 胶囊样式触发按钮 */
.search-trigger {
  display: flex;
  align-items: center;
  gap: 6px;
  height: 32px;
  padding: 0 10px;
  border: 1px solid hsl(var(--border));
  border-radius: 9999px;
  background: hsl(var(--muted) / 0.4);
  cursor: pointer;
  transition:
    background 0.15s ease,
    border-color 0.15s ease;
  outline: none;
}

.search-trigger:hover {
  background: hsl(var(--muted) / 0.8);
  border-color: hsl(var(--border));
}

.search-trigger-text {
  font-size: 13px;
  color: hsl(var(--muted-foreground));
  white-space: nowrap;
  user-select: none;
}

.search-kbd {
  display: inline-flex;
  align-items: center;
  padding: 1px 6px;
  font-size: 11px;
  font-family: ui-monospace, 'SFMono-Regular', monospace;
  color: hsl(var(--muted-foreground));
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  border-radius: 4px;
  line-height: 1.6;
  white-space: nowrap;
  pointer-events: none;
}

/* 移动端纯图标按钮 */
.search-trigger-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: none;
  border-radius: 50%;
  background: transparent;
  cursor: pointer;
  color: hsl(var(--foreground));
  transition: background 0.15s ease;
  outline: none;
}

.search-trigger-icon:hover {
  background: hsl(var(--accent));
}

/* 搜索结果项 */
.search-result-item {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: space-between;
  border-radius: 6px;
  padding: 8px 10px;
  text-align: left;
  background: transparent;
  border: none;
  cursor: pointer;
  transition: background 0.12s ease;
  outline: none;
}

.search-result-item:hover {
  background: hsl(var(--accent));
}
</style>

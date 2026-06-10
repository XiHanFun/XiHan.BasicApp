<script setup lang="ts">
import type { MenuRoute } from '~/types'
import { useFullscreen } from '@vueuse/core'
import { NIcon } from 'naive-ui'
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ensurePinyin, getPinyinIndex, usePinyinReady } from '~/composables/usePinyin'
import { usePlatform } from '~/composables/usePlatform'
import { useRecentRoutes } from '~/composables/useRecentRoutes'
import { AUTH_PATH, LAYOUT_EVENT_OPEN_GLOBAL_SEARCH } from '~/constants'
import { useRefresh, useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAccessStore, useAppStore, useAuthStore, useFavoritesStore, useLayoutBridgeStore } from '~/stores'

defineOptions({ name: 'AppGlobalSearch', inheritAttrs: false })

const router = useRouter()
const route = useRoute()
const { t, te } = useI18n()
const appStore = useAppStore()
const accessStore = useAccessStore()
const authStore = useAuthStore()
const favoritesStore = useFavoritesStore()
const layoutBridgeStore = useLayoutBridgeStore()
const { isDark, toggleThemeWithTransition } = useTheme()
const { refresh: refreshCurrentTab } = useRefresh()
const { isFullscreen, toggle: toggleFullscreen } = useFullscreen()
const { recent, recordRecent } = useRecentRoutes()
const { formatShortcut } = usePlatform()
const pinyinReady = usePinyinReady()

// 仅在快捷键启用时展示触发按钮上的 ⌘K/Ctrl+K 徽标
const showShortcut = computed(() => appStore.shortcutEnable && appStore.shortcutSearch)

const visible = ref(false)
const keyword = ref('')
const activeIndex = ref(0)
const inputRef = ref<HTMLInputElement | null>(null)
const listRef = ref<HTMLElement | null>(null)

type ItemGroup = 'action' | 'menu' | 'favorite' | 'recent'
interface PaletteItem {
  id: string
  group: ItemGroup
  title: string
  subtitle?: string
  icon: string
  keywords?: string
  run: () => void
}
type PaletteView = PaletteItem & { hl?: number[], gi?: number }

function tt(title: string): string {
  return te(title) ? t(title) : title
}

function resolveIcon(icon?: string): string {
  if (!icon) {
    return 'lucide:file'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

// ── 数据源 ───────────────────────────────────────────────────────
interface FlatMenu { name: string, path: string, title: string, icon?: string }
function flattenMenuRoutes(routes: MenuRoute[]): FlatMenu[] {
  const result: FlatMenu[] = []
  for (const r of routes) {
    if (r.meta?.hidden) {
      continue
    }
    if (r.path && r.name && r.meta?.title && r.component) {
      result.push({ name: r.name, path: r.path, title: t(r.meta.title, r.meta.title), icon: r.meta.icon })
    }
    if (r.children?.length) {
      result.push(...flattenMenuRoutes(r.children))
    }
  }
  return result
}

const menuItems = computed<PaletteItem[]>(() =>
  flattenMenuRoutes(accessStore.accessRoutes).map(m => ({
    id: `menu:${m.path}`,
    group: 'menu',
    title: m.title,
    subtitle: m.path,
    icon: resolveIcon(m.icon),
    keywords: `${m.name} ${m.path}`,
    run: () => jumpTo(m.path),
  })),
)

const favoriteItems = computed<PaletteItem[]>(() =>
  favoritesStore.favorites.map(f => ({
    id: `fav:${f.path}`,
    group: 'favorite',
    title: tt(f.title),
    subtitle: f.path,
    icon: resolveIcon(f.icon),
    run: () => jumpTo(f.path),
  })),
)

const recentItems = computed<PaletteItem[]>(() =>
  recent.value.map(r => ({
    id: `recent:${r.path}`,
    group: 'recent',
    title: tt(r.title),
    subtitle: r.path,
    icon: resolveIcon(r.icon),
    run: () => jumpTo(r.path),
  })),
)

const currentFavorited = computed(() => favoritesStore.has(route.fullPath))

function toggleFavoriteCurrent(): void {
  favoritesStore.toggle({
    key: route.fullPath,
    path: route.fullPath,
    title: (route.meta?.title as string) || route.fullPath,
    icon: route.meta?.icon as string | undefined,
  })
}

const actionItems = computed<PaletteItem[]>(() => {
  const list: PaletteItem[] = [
    {
      id: 'act:theme',
      group: 'action',
      title: isDark.value ? t('header.search.action.theme_light') : t('header.search.action.theme_dark'),
      icon: isDark.value ? 'lucide:sun' : 'lucide:moon',
      keywords: 'theme dark light 主题 深色 浅色 切换',
      run: () => toggleThemeWithTransition(),
    },
    {
      id: 'act:prefs',
      group: 'action',
      title: t('header.search.action.preferences'),
      icon: 'lucide:settings-2',
      keywords: 'preference settings 设置 偏好 配置',
      run: () => layoutBridgeStore.requestOpenPreferenceDrawer(),
    },
    {
      id: 'act:favorite',
      group: 'action',
      title: currentFavorited.value ? t('header.search.action.favorite_remove') : t('header.search.action.favorite_add'),
      icon: currentFavorited.value ? 'lucide:star-off' : 'lucide:star',
      keywords: 'favorite 收藏 标记',
      run: toggleFavoriteCurrent,
    },
    {
      id: 'act:fullscreen',
      group: 'action',
      title: isFullscreen.value ? t('header.search.action.fullscreen_exit') : t('header.search.action.fullscreen_enter'),
      icon: isFullscreen.value ? 'lucide:minimize' : 'lucide:maximize',
      keywords: 'fullscreen 全屏',
      run: () => void toggleFullscreen(),
    },
    {
      id: 'act:refresh',
      group: 'action',
      title: t('header.search.action.refresh'),
      icon: 'lucide:rotate-cw',
      keywords: 'refresh reload 刷新 重载',
      run: () => refreshCurrentTab(),
    },
    {
      id: 'act:sidebar',
      group: 'action',
      title: t('header.search.action.sidebar_toggle'),
      icon: 'lucide:panel-left',
      keywords: 'sidebar 侧栏 折叠 展开',
      run: () => layoutBridgeStore.requestSidebarToggle(),
    },
  ]
  if (appStore.widgetLockScreen) {
    list.push({
      id: 'act:lock',
      group: 'action',
      title: t('header.search.action.lock'),
      icon: 'lucide:lock',
      keywords: 'lock 锁屏 锁定',
      run: () => layoutBridgeStore.requestLockScreen(),
    })
  }
  list.push({
    id: 'act:logout',
    group: 'action',
    title: t('header.search.action.logout'),
    icon: 'lucide:log-out',
    keywords: 'logout 退出 登出 注销',
    run: () => void authStore.logout(),
  })
  return list
})

// ── 模糊匹配 ─────────────────────────────────────────────────────
function fuzzy(q: string, text: string): { score: number, indices: number[] } | null {
  const lower = text.toLowerCase()
  const ql = q.toLowerCase()
  let qi = 0
  let score = 0
  let last = -2
  const indices: number[] = []
  for (let i = 0; i < lower.length && qi < ql.length; i++) {
    if (lower[i] === ql[qi]) {
      indices.push(i)
      score += i === last + 1 ? 3 : 1
      if (i === 0) {
        score += 3
      }
      last = i
      qi++
    }
  }
  return qi === ql.length ? { score, indices } : null
}

const ASCII_QUERY_RE = /^[\w\s]+$/

function matchItem(item: PaletteItem, q: string): { score: number, indices: number[] } | null {
  const onTitle = fuzzy(q, item.title)
  if (onTitle) {
    return { score: onTitle.score + 24, indices: onTitle.indices }
  }
  // 拼音匹配（字母查询 × 含汉字标题）：首字母优先、全拼次之；命中位映射回原字符做高亮
  if (ASCII_QUERY_RE.test(q)) {
    const idx = getPinyinIndex(item.title)
    if (idx) {
      const onInitials = fuzzy(q, idx.initials)
      if (onInitials) {
        return { score: onInitials.score + 16, indices: onInitials.indices }
      }
      const onFull = fuzzy(q, idx.full)
      if (onFull) {
        return { score: onFull.score + 10, indices: [...new Set(onFull.indices.map(i => idx.fullMap[i]!))] }
      }
    }
  }
  const extra = fuzzy(q, `${item.keywords ?? ''} ${item.subtitle ?? ''}`)
  return extra ? { score: extra.score, indices: [] } : null
}

function matchAndSort(items: PaletteItem[], q: string): PaletteView[] {
  const scored: { view: PaletteView, score: number }[] = []
  for (const it of items) {
    const m = matchItem(it, q)
    if (m) {
      scored.push({ view: { ...it, hl: m.indices }, score: m.score })
    }
  }
  scored.sort((a, b) => b.score - a.score)
  return scored.map(s => s.view)
}

const groups = computed<{ key: string, label: string, items: PaletteView[] }[]>(() => {
  // 拼音词典懒加载完成后触发重算（命中拼音的结果即时生效）
  void pinyinReady.value
  const q = keyword.value.trim()
  const result: { key: string, label: string, items: PaletteView[] }[] = []
  if (!q) {
    if (recentItems.value.length) {
      result.push({ key: 'recent', label: t('header.search.group.recent'), items: recentItems.value.slice(0, 6) })
    }
    if (favoriteItems.value.length) {
      result.push({ key: 'favorite', label: t('header.search.group.favorite'), items: favoriteItems.value.slice(0, 6) })
    }
    result.push({ key: 'action', label: t('header.search.group.action'), items: actionItems.value })
    return result
  }
  const acts = matchAndSort(actionItems.value, q)
  const menus = matchAndSort(menuItems.value, q)
  if (acts.length) {
    result.push({ key: 'action', label: t('header.search.group.action'), items: acts })
  }
  if (menus.length) {
    result.push({ key: 'menu', label: t('header.search.group.menu'), items: menus })
  }
  return result
})

// 为每项分配全局索引（键盘导航用）
const groupsView = computed(() => {
  let i = 0
  return groups.value.map(g => ({
    ...g,
    items: g.items.map(it => ({ ...it, gi: i++ })),
  }))
})
const flat = computed<PaletteView[]>(() => groupsView.value.flatMap(g => g.items))
const activeId = computed(() => flat.value[activeIndex.value]?.id)

// 高亮匹配字符 → 连续段
function highlight(text: string, indices?: number[]): { text: string, hit: boolean }[] {
  if (!indices?.length) {
    return [{ text, hit: false }]
  }
  const set = new Set(indices)
  const segs: { text: string, hit: boolean }[] = []
  let cur = ''
  let curHit = set.has(0)
  for (let i = 0; i < text.length; i++) {
    const hit = set.has(i)
    if (hit === curHit) {
      cur += text[i]
    }
    else {
      if (cur) {
        segs.push({ text: cur, hit: curHit })
      }
      cur = text[i]!
      curHit = hit
    }
  }
  if (cur) {
    segs.push({ text: cur, hit: curHit })
  }
  return segs
}

// ── 操作 / 导航 ──────────────────────────────────────────────────
function close(): void {
  visible.value = false
}

function jumpTo(path: string): void {
  if (route.fullPath !== path) {
    void router.push(path)
  }
}

function execute(item: PaletteView): void {
  close()
  item.run()
}

function setActive(item: PaletteView): void {
  if (item.gi != null) {
    activeIndex.value = item.gi
  }
}

function scrollActiveIntoView(): void {
  void nextTick(() => {
    listRef.value
      ?.querySelector(`[data-gi="${activeIndex.value}"]`)
      ?.scrollIntoView({ block: 'nearest' })
  })
}

function onKeydown(e: KeyboardEvent): void {
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    activeIndex.value = Math.min(activeIndex.value + 1, flat.value.length - 1)
    scrollActiveIntoView()
  }
  else if (e.key === 'ArrowUp') {
    e.preventDefault()
    activeIndex.value = Math.max(activeIndex.value - 1, 0)
    scrollActiveIntoView()
  }
  else if (e.key === 'Enter') {
    e.preventDefault()
    const item = flat.value[activeIndex.value]
    if (item) {
      execute(item)
    }
  }
  else if (e.key === 'Escape') {
    e.preventDefault()
    close()
  }
}

// 搜索词变化 → 选中归零
watch(keyword, () => {
  activeIndex.value = 0
})

function openSearch(): void {
  visible.value = true
  keyword.value = ''
  activeIndex.value = 0
  // 懒加载拼音词典（幂等；就绪后 groups 自动重算）
  void ensurePinyin()
  void nextTick(() => inputRef.value?.focus())
}

function handleOpenEvent(): void {
  layoutBridgeStore.requestOpenGlobalSearch()
}

// 记录最近访问（设备本地）
watch(
  () => route.fullPath,
  (path) => {
    const title = route.meta?.title as string | undefined
    if (!title || path.startsWith(AUTH_PATH)) {
      return
    }
    recordRecent({ path, title, icon: route.meta?.icon as string | undefined })
  },
  { immediate: true },
)

onMounted(() => {
  window.addEventListener(LAYOUT_EVENT_OPEN_GLOBAL_SEARCH, handleOpenEvent)
})

onUnmounted(() => {
  window.removeEventListener(LAYOUT_EVENT_OPEN_GLOBAL_SEARCH, handleOpenEvent)
})

watch(
  () => layoutBridgeStore.globalSearchVersion,
  () => openSearch(),
)
</script>

<template>
  <!-- 触发按钮容器：$attrs 挂到此处（如外部传 class="mr-1"） -->
  <div v-bind="$attrs">
    <div class="hidden sm:block">
      <button type="button" class="search-trigger" @click="layoutBridgeStore.requestOpenGlobalSearch()">
        <NIcon size="14" class="shrink-0 text-[hsl(var(--muted-foreground))]">
          <Icon icon="lucide:search" />
        </NIcon>
        <span class="search-trigger-text">{{ t('header.search.placeholder') }}</span>
        <kbd v-if="showShortcut" class="search-kbd">{{ formatShortcut('Ctrl+K') }}</kbd>
      </button>
    </div>
    <div class="sm:hidden">
      <button type="button" class="search-trigger-icon" @click="layoutBridgeStore.requestOpenGlobalSearch()">
        <NIcon size="16">
          <Icon icon="lucide:search" />
        </NIcon>
      </button>
    </div>
  </div>

  <Teleport to="body">
    <Transition name="cmdk">
      <div v-if="visible" class="cmdk-mask" @click="close">
        <div class="cmdk-panel" role="dialog" aria-modal="true" @click.stop>
          <!-- 输入框 -->
          <div class="cmdk-input">
            <NIcon size="17" class="cmdk-input__icon">
              <Icon icon="lucide:search" />
            </NIcon>
            <input
              ref="inputRef"
              v-model="keyword"
              class="cmdk-input__field"
              :placeholder="t('header.search.input_hint')"
              spellcheck="false"
              autocomplete="off"
              @keydown="onKeydown"
            >
            <kbd class="cmdk-esc">esc</kbd>
          </div>

          <!-- 结果列表 -->
          <div ref="listRef" class="cmdk-list">
            <template v-for="g in groupsView" :key="g.key">
              <div class="cmdk-group">
                {{ g.label }}
              </div>
              <button
                v-for="it in g.items"
                :key="it.id"
                type="button"
                class="cmdk-item"
                :class="{ 'is-active': it.id === activeId }"
                :data-gi="it.gi"
                @click="execute(it)"
                @mousemove="setActive(it)"
              >
                <span class="cmdk-item__icon">
                  <NIcon size="17"><Icon :icon="it.icon" /></NIcon>
                </span>
                <span class="cmdk-item__title">
                  <span
                    v-for="(seg, si) in highlight(it.title, it.hl)"
                    :key="si"
                    :class="{ 'cmdk-hl': seg.hit }"
                  >{{ seg.text }}</span>
                </span>
                <span v-if="it.group === 'action'" class="cmdk-item__tag">{{ t('header.search.tag_action') }}</span>
                <span v-else-if="it.subtitle" class="cmdk-item__path">{{ it.subtitle }}</span>
              </button>
            </template>

            <div v-if="!flat.length" class="cmdk-empty">
              <NIcon size="22" class="opacity-40">
                <Icon icon="lucide:search-x" />
              </NIcon>
              <span>{{ t('header.search.empty') }}</span>
            </div>
          </div>

          <!-- 底部快捷键提示 -->
          <div class="cmdk-footer">
            <span><kbd>↑</kbd><kbd>↓</kbd> {{ t('header.search.footer.select') }}</span>
            <span><kbd>↵</kbd> {{ t('header.search.footer.open') }}</span>
            <span><kbd>esc</kbd> {{ t('header.search.footer.close') }}</span>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
/* ===== 触发按钮（保持原样） ===== */
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

/* ===== 命令面板 ===== */
.cmdk-mask {
  position: fixed;
  inset: 0;
  z-index: 2800;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding-top: 14vh;
  background: hsl(var(--overlay, 0 0% 0%) / 0.4);
  backdrop-filter: blur(2px);
}

.cmdk-panel {
  width: min(92vw, 600px);
  max-height: 70vh;
  display: flex;
  flex-direction: column;
  border-radius: 14px;
  overflow: hidden;
  background: hsl(var(--background));
  border: 1px solid hsl(var(--border));
  box-shadow: 0 24px 60px rgb(0 0 0 / 28%);
}

.cmdk-input {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 0 14px;
  height: 52px;
  border-bottom: 1px solid hsl(var(--border));
  flex-shrink: 0;
}

.cmdk-input__icon {
  color: hsl(var(--muted-foreground));
  flex-shrink: 0;
}

.cmdk-input__field {
  flex: 1;
  height: 100%;
  border: none;
  outline: none;
  background: transparent;
  color: hsl(var(--foreground));
  font-size: 15px;
}

.cmdk-input__field::placeholder {
  color: hsl(var(--muted-foreground));
}

.cmdk-esc {
  padding: 2px 7px;
  font-size: 11px;
  font-family: ui-monospace, monospace;
  color: hsl(var(--muted-foreground));
  background: hsl(var(--muted) / 0.6);
  border-radius: 5px;
  flex-shrink: 0;
}

.cmdk-list {
  flex: 1;
  overflow-y: auto;
  padding: 6px;
  scrollbar-width: thin;
}

.cmdk-group {
  padding: 8px 8px 4px;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.04em;
  color: hsl(var(--muted-foreground));
}

.cmdk-item {
  display: flex;
  align-items: center;
  gap: 10px;
  width: 100%;
  padding: 9px 10px;
  border: none;
  border-radius: 8px;
  background: transparent;
  text-align: left;
  cursor: pointer;
  outline: none;
}

.cmdk-item.is-active {
  background: hsl(var(--primary) / 0.12);
}

.cmdk-item__icon {
  display: inline-flex;
  flex-shrink: 0;
  color: hsl(var(--foreground) / 0.7);
}

.cmdk-item.is-active .cmdk-item__icon {
  color: hsl(var(--primary));
}

.cmdk-item__title {
  flex: 1;
  min-width: 0;
  font-size: 14px;
  color: hsl(var(--foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cmdk-hl {
  color: hsl(var(--primary));
  font-weight: 600;
}

.cmdk-item__path {
  flex-shrink: 0;
  max-width: 45%;
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cmdk-item__tag {
  flex-shrink: 0;
  padding: 1px 7px;
  font-size: 11px;
  color: hsl(var(--muted-foreground));
  background: hsl(var(--muted) / 0.6);
  border-radius: 9999px;
}

.cmdk-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8px;
  padding: 40px 0;
  font-size: 13px;
  color: hsl(var(--muted-foreground));
}

.cmdk-footer {
  display: flex;
  gap: 16px;
  padding: 8px 14px;
  border-top: 1px solid hsl(var(--border));
  font-size: 12px;
  color: hsl(var(--muted-foreground));
  flex-shrink: 0;
}

.cmdk-footer kbd {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  padding: 1px 4px;
  margin-right: 2px;
  font-family: ui-monospace, monospace;
  font-size: 11px;
  background: hsl(var(--muted) / 0.6);
  border-radius: 4px;
}

/* 过渡 */
.cmdk-enter-active {
  transition: opacity 0.18s ease;
}

.cmdk-leave-active {
  transition: opacity 0.14s ease;
}

.cmdk-enter-from,
.cmdk-leave-to {
  opacity: 0;
}

.cmdk-enter-active .cmdk-panel {
  transition: transform 0.24s cubic-bezier(0.22, 1.2, 0.36, 1);
}

.cmdk-enter-from .cmdk-panel {
  transform: translateY(-12px) scale(0.97);
}
</style>

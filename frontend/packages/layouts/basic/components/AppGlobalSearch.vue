<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { MenuRoute } from '~/types'
import { useFullscreen } from '@vueuse/core'
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import { ensurePinyin, getPinyinIndex, usePinyinReady } from '~/composables/usePinyin'
import { useRecentRoutes } from '~/composables/useRecentRoutes'
import { AUTH_PATH, LAYOUT_EVENT_OPEN_GLOBAL_SEARCH } from '~/constants'
import { useRefresh, useTheme } from '~/hooks'
import { Icon } from '~/iconify'
import { useAccessStore, useAppStore, useAuthStore, useFavoritesStore, useLayoutBridgeStore } from '~/stores'

/**
 * 命令面板：布局层常驻一份，由 layoutBridgeStore.requestOpenGlobalSearch 打开
 * 顶栏入口（HeaderSearchTrigger）、悬浮组入口与快捷键都只发请求，入口放哪、藏不藏都不影响面板可用
 */
defineOptions({ name: 'AppGlobalSearch' })

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
const pinyinReady = usePinyinReady()

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
  return te(title)
    ? t(title)
    : title
}

function resolveIcon(icon?: string): string {
  if (!icon) {
    return 'lucide:file'
  }
  return icon.includes(':')
    ? icon
    : `lucide:${icon}`
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
      result.push({
        name: r.name,
        path: r.path,
        title: t(r.meta.title, r.meta.title),
        icon: r.meta.icon,
      })
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
      title: isDark.value
        ? t('header.search.action.theme_light')
        : t('header.search.action.theme_dark'),
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
      title: currentFavorited.value
        ? t('header.search.action.favorite_remove')
        : t('header.search.action.favorite_add'),
      icon: currentFavorited.value ? 'lucide:star-off' : 'lucide:star',
      keywords: 'favorite 收藏 标记',
      run: toggleFavoriteCurrent,
    },
    {
      id: 'act:fullscreen',
      group: 'action',
      title: isFullscreen.value
        ? t('header.search.action.fullscreen_exit')
        : t('header.search.action.fullscreen_enter'),
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
      score += i === last + 1
        ? 3
        : 1
      if (i === 0) {
        score += 3
      }
      last = i
      qi++
    }
  }
  return qi === ql.length
    ? { score, indices }
    : null
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
        return {
          score: onFull.score + 10,
          indices: [
            ...new Set(onFull.indices.map(i => idx.fullMap[i]!)),
          ],
        }
      }
    }
  }
  const extra = fuzzy(q, `${item.keywords ?? ''} ${item.subtitle ?? ''}`)
  return extra
    ? { score: extra.score, indices: [] }
    : null
}

function matchAndSort(items: PaletteItem[], q: string): PaletteView[] {
  const scored: { view: PaletteView, score: number }[] = []
  for (const it of items) {
    const m = matchItem(it, q)
    if (m) {
      scored.push({
        view: { ...it, hl: m.indices },
        score: m.score,
      })
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
      result.push({
        key: 'recent',
        label: t('header.search.group.recent'),
        items: recentItems.value.slice(0, 6),
      })
    }
    if (favoriteItems.value.length) {
      result.push({
        key: 'favorite',
        label: t('header.search.group.favorite'),
        items: favoriteItems.value.slice(0, 6),
      })
    }
    result.push({
      key: 'action',
      label: t('header.search.group.action'),
      items: actionItems.value,
    })
    return result
  }
  const acts = matchAndSort(actionItems.value, q)
  const menus = matchAndSort(menuItems.value, q)
  if (acts.length) {
    result.push({
      key: 'action',
      label: t('header.search.group.action'),
      items: acts,
    })
  }
  if (menus.length) {
    result.push({
      key: 'menu',
      label: t('header.search.group.menu'),
      items: menus,
    })
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
    return [
      { text, hit: false },
    ]
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
  <Teleport to="body">
    <Transition name="cmdk">
      <div v-if="visible" class="cmdk-mask" @click="close">
        <div class="cmdk-panel app-glass" role="dialog" aria-modal="true" @click.stop>
          <!-- 输入框 -->
          <div class="cmdk-input">
            <span class="cmdk-input__icon" style="display: inline-flex; font-size: var(--xh-glyph-size-sm)">
              <Icon icon="lucide:search" />
            </span>
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
                  <Icon width="17" height="17" :icon="it.icon" />
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
              <span class="opacity-40" style="display: inline-flex; font-size: var(--xh-glyph-size-lg)">
                <Icon icon="lucide:search-x" />
              </span>
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
/* ===== 命令面板 ===== */
.cmdk-mask {
  position: fixed;
  inset: 0;
  z-index: 2800;
  display: flex;
  justify-content: center;
  align-items: flex-start;
  /* 面板是玻璃（.app-glass），遮罩只轻压一层（对话框遮罩的四成），压得太暗玻璃会发灰；
     遮罩本身不模糊，面板外的页面保持清晰，模糊只出现在面板身后，玻璃感才看得出来 */
  background: color-mix(in srgb, var(--xh-bg-overlay) 40%, transparent);
}

.cmdk-panel {
  width: min(92vw, 600px);
  margin-block-start: 14vh;
  max-height: 70vh;
  display: flex;
  flex-direction: column;
  border-radius: var(--xh-shape-sheet);
  overflow: hidden;
}

.cmdk-input {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
  padding: 0 var(--xh-space-3);
  height: 52px;
  flex-shrink: 0;
  border-block-end: calc(var(--xh-stroke-thin) * 1.5) solid var(--xh-border-default);
}

.cmdk-input__icon {
  color: var(--xh-fg-muted);
  flex-shrink: 0;
}

.cmdk-input__field {
  flex: 1;
  height: 100%;
  border: none;
  outline: none;
  background: transparent;
  color: var(--xh-fg-default);
  font-size: var(--xh-font-size-md);
}

.cmdk-input__field::placeholder {
  color: var(--xh-fg-muted);
}

.cmdk-esc {
  padding: var(--xh-space-0_5) var(--xh-space-1_5);
  font-size: var(--xh-font-size-xs);
  font-family: var(--xh-font-family-mono);
  color: var(--xh-fg-muted);
  background: color-mix(in srgb, var(--xh-bg-muted) 60%, transparent);
  border-radius: var(--xh-shape-control);
  flex-shrink: 0;
}

.cmdk-list {
  flex: 1;
  overflow-y: auto;
  padding: var(--xh-space-1_5);
}

.cmdk-group {
  padding: var(--xh-space-2) var(--xh-space-2) var(--xh-space-1);
  font-size: var(--xh-font-size-xs);
  font-weight: 600;
  letter-spacing: 0.04em;
  color: var(--xh-fg-muted);
}

.cmdk-item {
  display: flex;
  align-items: center;
  gap: var(--xh-space-2_5);
  width: 100%;
  padding: var(--xh-space-2) var(--xh-space-2_5);
  border: none;
  border-radius: var(--xh-shape-control);
  background: transparent;
  text-align: left;
  cursor: pointer;
  outline: none;
}

.cmdk-item.is-active {
  background: color-mix(in srgb, var(--xh-fg-brand) 12%, transparent);
}

.cmdk-item__icon {
  display: inline-flex;
  flex-shrink: 0;
  color: color-mix(in srgb, var(--xh-fg-default) 70%, transparent);
}

.cmdk-item.is-active .cmdk-item__icon {
  color: var(--xh-fg-brand);
}

.cmdk-item__title {
  flex: 1;
  min-width: 0;
  font-size: var(--xh-font-size-md);
  color: var(--xh-fg-default);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cmdk-hl {
  color: var(--xh-fg-brand);
  font-weight: 600;
}

.cmdk-item__path {
  flex-shrink: 0;
  max-width: 45%;
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.cmdk-item__tag {
  flex-shrink: 0;
  padding: var(--xh-space-0_5) var(--xh-space-1_5);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  background: color-mix(in srgb, var(--xh-bg-muted) 60%, transparent);
  border-radius: var(--xh-shape-pill);
}

.cmdk-empty {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: var(--xh-space-2);
  padding: calc(var(--xh-space-5) * 2) 0;
  font-size: var(--xh-font-size-sm);
  color: var(--xh-fg-muted);
}

.cmdk-footer {
  display: flex;
  gap: var(--xh-space-4);
  padding: var(--xh-space-2) var(--xh-space-3);
  font-size: var(--xh-font-size-xs);
  color: var(--xh-fg-muted);
  flex-shrink: 0;
}

.cmdk-footer kbd {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 16px;
  padding: var(--xh-space-0_5) var(--xh-space-1);
  margin-inline-end: 2px;
  font-family: var(--xh-font-family-mono);
  font-size: var(--xh-font-size-xs);
  background: color-mix(in srgb, var(--xh-bg-muted) 60%, transparent);
  border-radius: var(--xh-shape-inset);
}

/* 过渡 */
.cmdk-enter-active {
  transition: opacity var(--xh-motion-duration-enter) var(--xh-motion-ease-enter);
}

.cmdk-leave-active {
  transition: opacity var(--xh-motion-duration-exit) var(--xh-motion-ease-exit);
}

.cmdk-enter-from,
.cmdk-leave-to {
  opacity: 0;
}

.cmdk-enter-active .cmdk-panel {
  transition: transform var(--xh-motion-duration-move) var(--xh-motion-ease-settle);
}

.cmdk-enter-from .cmdk-panel {
  transform: translateY(-12px) scale(0.97);
}
</style>

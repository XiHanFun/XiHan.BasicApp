<script setup lang="ts">
import { XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle } from '@xihan-ui/vue'
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { useFavoritesStore } from '~/stores'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'FavoritesWidget' })

const { t, te } = useI18n()
const router = useRouter()
const favoritesStore = useFavoritesStore()

// 收藏入口：复用「收藏夹」数据（右键标签收藏的常用菜单），跨端同步
// 入口图标按位置轮换着色，只为彼此区分、不表语义，取组件库的色相色阶
const QUICK_PALETTE = [
  'var(--xh-color-blue-500)',
  'var(--xh-color-green-500)',
  'var(--xh-color-purple-500)',
  'var(--xh-color-amber-500)',
  'var(--xh-color-red-500)',
  'var(--xh-color-cyan-500)',
  'var(--xh-color-pink-500)',
  'var(--xh-color-teal-500)',
]

/** 收藏标题多为 i18n key（如 menu.user），渲染时翻译；非 key 原样展示 */
function displayTitle(title: string) {
  return te(title) ? t(title) : title
}

function resolveIcon(icon?: string | null) {
  if (!icon) {
    return 'lucide:bookmark'
  }
  return icon.includes(':') ? icon : `lucide:${icon}`
}

const links = computed(() =>
  favoritesStore.favorites.map((fav, index) => ({
    key: fav.path,
    label: displayTitle(fav.title),
    icon: resolveIcon(fav.icon),
    to: fav.path,
    color: QUICK_PALETTE[index % QUICK_PALETTE.length] ?? 'var(--xh-color-blue-500)',
  })),
)

function go(path: string) {
  router.push(path).catch(() => {})
}

onMounted(() => {
  // 跨端同步：拉取后端收藏夹覆盖本地（尽力而为，离线静默回退本地）
  void favoritesStore.hydrate()
})
</script>

<template>
  <WidgetCard icon="lucide:star" :title="t('workbench.widgets.favorites.title')">
    <!-- 开着收藏夹同步时等后端那份回来再摆：本地那份可能是别的设备改动之前的旧值 -->
    <div v-if="favoritesStore.ready && links.length" class="grid grid-cols-3 gap-1 sm:grid-cols-4">
      <button
        v-for="link in links"
        :key="link.key"
        type="button"
        class="flex flex-col items-center gap-2 rounded-lg px-2 py-3 text-center transition-all hover:-translate-y-px hover:bg-accent"
        @click="go(link.to)"
      >
        <div
          class="flex h-11 w-11 items-center justify-center rounded-xl"
          :style="{ backgroundColor: `color-mix(in oklab, ${link.color} 9%, transparent)` }"
        >
          <Icon :icon="link.icon" width="22" height="22" :style="{ color: link.color }" />
        </div>
        <span class="truncate text-xs font-medium text-foreground">{{ link.label }}</span>
      </button>
    </div>
    <div v-else-if="favoritesStore.ready" class="flex h-full items-center justify-center py-6">
      <XhEmptyStateRoot size="sm">
        <XhEmptyStateIndicator>
          <Icon icon="lucide:inbox" width="24" />
        </XhEmptyStateIndicator>
        <XhEmptyStateTitle>{{ t('workbench.dashboard.quick_empty_title') }}</XhEmptyStateTitle>
        <XhEmptyStateDescription>{{ t('workbench.dashboard.quick_empty') }}</XhEmptyStateDescription>
      </XhEmptyStateRoot>
    </div>
  </WidgetCard>
</template>

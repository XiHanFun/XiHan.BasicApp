<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { Icon } from '@iconify/vue'
import { NButton, NEmpty, NInput, NModal, NScrollbar } from 'naive-ui'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'

defineOptions({ name: 'AppGlobalSearch' })

const router = useRouter()
const { t } = useI18n()
const visible = ref(false)
const keyword = ref('')

const routeItems = computed(() => {
  const routes = router
    .getRoutes()
    .filter((item) => item.meta?.title && !item.meta?.hidden && item.path && item.name)
    .map((item) => ({
      name: String(item.name),
      path: item.path,
      title: t(String(item.meta.title), String(item.meta.title)),
      icon: item.meta.icon as string | undefined,
    }))

  const text = keyword.value.trim().toLowerCase()
  if (!text) return routes.slice(0, 20)
  return routes.filter(
    (item) =>
      item.title.toLowerCase().includes(text) ||
      item.path.toLowerCase().includes(text) ||
      item.name.toLowerCase().includes(text),
  )
})

function openSearch() {
  visible.value = true
}

function closeSearch() {
  visible.value = false
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
  <NButton
    quaternary
    size="small"
    class="justify-center sm:min-w-[180px] sm:justify-start"
    @click="openSearch"
  >
    <template #icon>
      <Icon icon="lucide:search" width="16" />
    </template>
    <span class="hidden sm:inline">搜索菜单...</span>
  </NButton>

  <NModal v-model:show="visible" preset="card" :bordered="false" class="w-[620px]">
    <div class="space-y-3">
      <NInput v-model:value="keyword" placeholder="输入菜单名称、路径或路由名" clearable />
      <NScrollbar style="max-height: 420px">
        <div v-if="routeItems.length" class="space-y-1">
          <button
            v-for="item in routeItems"
            :key="item.name"
            type="button"
            class="flex w-full items-center justify-between rounded-md px-3 py-2 text-left hover:bg-gray-100 dark:hover:bg-gray-800"
            @click="jumpTo(item.path)"
          >
            <span class="flex items-center gap-2">
              <Icon v-if="item.icon" :icon="item.icon" width="16" />
              <span>{{ item.title }}</span>
            </span>
            <span class="text-xs text-gray-400">{{ item.path }}</span>
          </button>
        </div>
        <NEmpty v-else description="未找到匹配菜单" />
      </NScrollbar>
      <div class="flex justify-end">
        <NButton size="small" @click="closeSearch">关闭</NButton>
      </div>
    </div>
  </NModal>
</template>

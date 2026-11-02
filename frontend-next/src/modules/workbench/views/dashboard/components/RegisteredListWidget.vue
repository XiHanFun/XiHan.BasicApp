<script setup lang="ts">
import type { WorkbenchListWidget } from '~/stores'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIndicator, XhEmptyStateRoot, XhEmptyStateTitle, XhTagRoot } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { Icon } from '~/iconify'
import { formatDate } from '~/utils'
import { useWidgetData } from './widget-data'
import WidgetCard from './WidgetCard.vue'

/**
 * 模块登记的列表型小组件（registerWorkbenchWidget）：标题栏是总数与「查看全部」，正文是最近几条，点一条跳到它的链接。
 * 取数中、出错、没有条目各给一个占位。
 */
defineOptions({ name: 'RegisteredListWidget' })

const props = defineProps<{ widget: WorkbenchListWidget }>()

const { t } = useI18n()
const router = useRouter()
const { data, loading, error } = useWidgetData(props.widget.dataKey)
const items = computed(() => data.value?.items ?? [])
const total = computed(() => data.value?.value ?? null)

function open(link?: string | null) {
  void router.push(link || props.widget.moreLink)
}
</script>

<template>
  <WidgetCard :icon="widget.icon" :title="t(widget.titleKey)">
    <template #badge>
      <XhTagRoot v-if="total !== null" variant="subtle" size="sm" tone="brand">
        {{ total }}
      </XhTagRoot>
    </template>
    <template #extra>
      <XhButton size="sm" variant="ghost" @click="open(widget.moreLink)">
        {{ t('workbench.widgets.view_all') }}
      </XhButton>
    </template>
    <div v-if="loading" class="list-placeholder" role="status">
      {{ t('common.loading') }}
    </div>
    <XhEmptyStateRoot v-else-if="error || !items.length" size="sm" class="list-placeholder">
      <XhEmptyStateIndicator>
        <Icon :icon="error ? 'lucide:circle-alert' : 'lucide:inbox'" width="24" height="24" />
      </XhEmptyStateIndicator>
      <XhEmptyStateTitle>{{ error ? t('common.messages.load_failed') : t(widget.emptyKey) }}</XhEmptyStateTitle>
      <XhEmptyStateDescription v-if="error">
        {{ error }}
      </XhEmptyStateDescription>
    </XhEmptyStateRoot>
    <ul v-else class="flex flex-col gap-0.5">
      <li v-for="item in items" :key="item.id">
        <button
          type="button"
          class="list-row flex w-full items-center gap-3 rounded px-2 py-1.5 text-left transition-colors hover:bg-muted"
          @click="open(item.link)"
        >
          <span class="min-w-0 flex-1 truncate text-sm text-foreground">{{ item.title }}</span>
          <span v-if="item.time" class="shrink-0 text-xs text-muted-foreground">{{ formatDate(item.time, 'MM-DD HH:mm') }}</span>
        </button>
      </li>
    </ul>
  </WidgetCard>
</template>

<style scoped>
/* 占位与内容大致同高，取数前后看板不跳 */
.list-placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-block-size: var(--xh-viewport-h-sm);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-sm);
}

/* 自写的可聚焦行用组件库同一道聚焦环（画在盒内） */
.list-row:focus-visible {
  outline: var(--xh-ring-width) solid var(--xh-ring-focus);
  outline-offset: var(--xh-ring-offset);
}
</style>

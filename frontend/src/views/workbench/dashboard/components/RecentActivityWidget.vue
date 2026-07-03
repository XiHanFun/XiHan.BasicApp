<script setup lang="ts">
import type { UserInboxItemDto } from '@/api'
import { NEmpty, NTimeline, NTimelineItem } from 'naive-ui'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { formatDate } from '~/utils'
import { loadDashboardSummary } from './summary'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'RecentActivityWidget' })

const { t } = useI18n()

// 最近动态：站内信最新消息（后端 DashboardSummary 真实数据）
const latestItems = ref<UserInboxItemDto[]>([])

function formatRelative(value?: string | null) {
  if (!value) {
    return '-'
  }
  const diff = Date.now() - new Date(value).getTime()
  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) {
    return t('workbench.dashboard.just_now')
  }
  if (minutes < 60) {
    return t('workbench.dashboard.minutes_ago', { n: minutes })
  }
  const hours = Math.floor(minutes / 60)
  if (hours < 24) {
    return t('workbench.dashboard.hours_ago', { n: hours })
  }
  const days = Math.floor(hours / 24)
  if (days < 7) {
    return t('workbench.dashboard.days_ago', { n: days })
  }
  return formatDate(value, 'MM-DD HH:mm')
}

onMounted(async () => {
  try {
    const summary = await loadDashboardSummary()
    latestItems.value = summary.inbox.latestItems ?? []
  }
  catch {
    latestItems.value = []
  }
})
</script>

<template>
  <WidgetCard icon="lucide:clock" :title="t('workbench.dashboard.recent_activity')">
    <NTimeline v-if="latestItems.length">
      <NTimelineItem
        v-for="(item, index) in latestItems"
        :key="item.basicId"
        :color="index === 0 ? '#3b82f6' : undefined"
        :time="formatRelative(item.sendTime)"
        line-type="dashed"
      >
        <div class="text-[13px] font-medium text-foreground">
          {{ item.title }}
        </div>
        <div v-if="item.content" class="mt-0.5 line-clamp-2 text-xs text-muted-foreground">
          {{ item.content }}
        </div>
      </NTimelineItem>
    </NTimeline>
    <div v-else class="flex h-full items-center justify-center py-6">
      <NEmpty :description="t('workbench.dashboard.activity_empty')" />
    </div>
  </WidgetCard>
</template>

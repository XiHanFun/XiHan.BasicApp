<script setup lang="ts">
import { XhTooltipArrow, XhTooltipContent, XhTooltipPositioner, XhTooltipRoot, XhTooltipTrigger } from '@xihan-ui/vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

defineOptions({ name: 'SyncStatusBadge' })

defineProps<{
  /** 是否已开启该类的后端同步；为 false 时在标题后展示「本地」标记 */
  synced: boolean
}>()

const { t } = useI18n()
</script>

<template>
  <XhTooltipRoot>
    <!-- 触发器渲染成 button，这里只是个状态标记，去掉按钮相：不吃指针手型、不参与提交 -->
    <XhTooltipTrigger
      class="sync-badge"
      :class="synced
        ? 'bg-[hsl(var(--primary)/0.1)] text-[hsl(var(--primary))]'
        : 'bg-foreground/5 text-foreground/50'"
    >
      <Icon :icon="synced ? 'lucide:cloud' : 'lucide:hard-drive'" width="11" height="11" />
      {{ synced ? t('preference.sync_status.synced') : t('preference.sync_status.local') }}
    </XhTooltipTrigger>
    <XhTooltipPositioner>
      <XhTooltipContent>
        {{ synced ? t('preference.sync_status.synced_tip') : t('preference.sync_status.local_tip') }}
        <XhTooltipArrow />
      </XhTooltipContent>
    </XhTooltipPositioner>
  </XhTooltipRoot>
</template>

<style scoped>
.sync-badge {
  display: inline-flex;
  gap: 3px;
  align-items: center;
  block-size: 18px;
  padding-inline: 7px;
  border-radius: var(--xh-radius-full);
  font-size: 11px;
  line-height: 1;
  white-space: nowrap;
  vertical-align: middle;
  cursor: default;
}
</style>

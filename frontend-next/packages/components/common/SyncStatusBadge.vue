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
    <!--
      触发器借这个 span：它只是个状态标记，不该是按钮。
      默认渲染出的 button 会被浮层打开时的初始焦点探测选中，一开面板提示就自动弹出来盖住内容。
    -->
    <XhTooltipTrigger as-child>
      <span
        class="sync-badge"
        :class="synced
          ? 'bg-[hsl(var(--primary)/0.1)] text-[hsl(var(--primary))]'
          : 'bg-foreground/5 text-foreground/50'"
      >
        <Icon :icon="synced ? 'lucide:cloud' : 'lucide:hard-drive'" width="11" height="11" />
        {{ synced ? t('preference.sync_status.synced') : t('preference.sync_status.local') }}
      </span>
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

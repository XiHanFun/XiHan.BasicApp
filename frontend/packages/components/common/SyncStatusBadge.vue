<script setup lang="ts">
import { NTooltip } from 'naive-ui'
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
  <NTooltip>
    <template #trigger>
      <span
        class="inline-flex items-center gap-[3px] h-[18px] px-[7px] rounded-full text-[11px] leading-none align-middle whitespace-nowrap cursor-default"
        :class="synced
          ? 'bg-[hsl(var(--primary)/0.1)] text-[hsl(var(--primary))]'
          : 'bg-foreground/5 text-foreground/50'"
      >
        <Icon :icon="synced ? 'lucide:cloud' : 'lucide:hard-drive'" width="11" height="11" />
        {{ synced ? t('preference.sync_status.synced') : t('preference.sync_status.local') }}
      </span>
    </template>
    {{ synced ? t('preference.sync_status.synced_tip') : t('preference.sync_status.local_tip') }}
  </NTooltip>
</template>

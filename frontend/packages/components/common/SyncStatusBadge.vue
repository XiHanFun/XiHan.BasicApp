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
  <NTooltip v-if="!synced">
    <template #trigger>
      <span
        class="inline-flex items-center gap-[3px] h-[18px] px-[7px] rounded-full bg-foreground/5 text-[11px] leading-none text-foreground/50 align-middle whitespace-nowrap cursor-default"
      >
        <Icon icon="lucide:hard-drive" width="11" height="11" />
        {{ t('preference.sync_status.local') }}
      </span>
    </template>
    {{ t('preference.sync_status.local_tip') }}
  </NTooltip>
</template>

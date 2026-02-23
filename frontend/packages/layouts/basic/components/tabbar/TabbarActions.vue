<script lang="ts" setup>
import { Icon } from '@iconify/vue'
import { NButton, NIcon } from 'naive-ui'

interface Props {
  showRefresh: boolean
  showMaximize: boolean
  isContentMaximized: boolean
}

defineProps<Props>()

const emit = defineEmits<{
  refresh: []
  maximize: []
}>()
</script>

<template>
  <div class="tabbar-actions flex shrink-0 items-center gap-1">
    <NButton v-if="showRefresh" quaternary circle size="tiny" @click="emit('refresh')">
      <template #icon>
        <NIcon>
          <Icon icon="lucide:rotate-cw" width="14" />
        </NIcon>
      </template>
    </NButton>
    <span v-if="showRefresh && showMaximize" class="tab-divider" />
    <NButton v-if="showMaximize" quaternary circle size="tiny" @click="emit('maximize')">
      <template #icon>
        <NIcon>
          <Icon
            :icon="isContentMaximized ? 'lucide:minimize-2' : 'lucide:maximize-2'"
            width="14"
          />
        </NIcon>
      </template>
    </NButton>
  </div>
</template>

<style scoped>
.tab-divider {
  display: inline-block;
  flex-shrink: 0;
  width: 1px;
  height: 16px;
  margin: 0 2px;
  background: var(--border-color);
  vertical-align: middle;
}
</style>

<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NButton } from 'naive-ui'

defineOptions({ name: 'PreferenceFab' })
defineProps<{ show: boolean }>()
const emit = defineEmits<{ click: [] }>()
</script>

<template>
  <div
    v-if="show"
    class="preference-fab fixed z-[80]"
    style="right: 12px; bottom: 88px"
  >
    <NButton quaternary circle size="large" title="偏好设置" @click="emit('click')">
      <template #icon>
        <Icon icon="lucide:settings-2" width="20" />
      </template>
    </NButton>
  </div>
</template>

<style scoped>
.preference-fab {
  border-radius: 9999px;
  background: color-mix(in srgb, hsl(var(--background)) 88%, transparent);
  backdrop-filter: blur(8px);
}
</style>

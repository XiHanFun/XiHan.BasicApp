<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NButton } from 'naive-ui'
import { useI18n } from 'vue-i18n'

defineOptions({ name: 'PreferenceFab' })
defineProps<{ show: boolean }>()
const emit = defineEmits<{ click: [] }>()

const { t } = useI18n()
</script>

<template>
  <div
    v-if="show"
    class="preference-fab fixed z-[80]"
    style="right: 12px; bottom: 88px"
  >
    <NButton quaternary circle size="large" :title="t('preference.drawer.title')" @click="emit('click')">
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

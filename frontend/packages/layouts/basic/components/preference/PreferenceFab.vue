<script setup lang="ts">
import { Icon } from '@iconify/vue'
import { NButton, NIcon } from 'naive-ui'
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
    <NButton
      circle
      size="large"
      type="primary"
      :title="t('preference.drawer.title')"
      @click="emit('click')"
      @mousedown.prevent
    >
      <template #icon>
        <NIcon size="20">
          <Icon icon="lucide:settings-2" />
        </NIcon>
      </template>
    </NButton>
  </div>
</template>

<style scoped>
.preference-fab {
  filter: drop-shadow(0 4px 12px hsl(var(--primary) / 0.35));
}
</style>

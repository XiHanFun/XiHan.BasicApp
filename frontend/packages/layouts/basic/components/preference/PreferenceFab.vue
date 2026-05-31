<script setup lang="ts">
import { NFloatButton, NIcon } from 'naive-ui'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

defineOptions({ name: 'PreferenceFab' })
defineProps<{ show: boolean }>()
const emit = defineEmits<{ click: [] }>()

const { t } = useI18n()
</script>

<template>
  <NFloatButton
    v-if="show"
    class="xh-preference-fab"
    type="primary"
    :right="12"
    :bottom="88"
    :width="40"
    :height="40"
    :title="t('preference.drawer.title')"
    @click="emit('click')"
  >
    <NIcon size="20">
      <Icon icon="lucide:settings-2" />
    </NIcon>
  </NFloatButton>
</template>

<style scoped>
/* 提升层级，避免被表格固定列 / 列表卡片等高 z-index 元素遮挡 */
.xh-preference-fab {
  z-index: 1000;
}
</style>

<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhButton } from '@xihan-ui/vue'
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'

defineOptions({ name: 'SidebarCollapseButton' })

const props = defineProps<{ collapsed: boolean }>()
const emit = defineEmits<{ 'update:collapsed': [value: boolean] }>()

const { t } = useI18n()

// 名字说的是按下后的动作，随状态换；顶栏那颗管整栏显隐，两者同屏，所以不复用它的文案
const label = computed(() => props.collapsed
  ? t('sidebar.expand')
  : t('sidebar.collapse'))

function toggle() {
  emit('update:collapsed', !props.collapsed)
}
</script>

<template>
  <XhButton
    variant="subtle"
    tone="neutral"
    size="sm"
    icon-only
    class="absolute bottom-2 left-3 z-10"
    :aria-label="label"
    :title="label"
    @click.stop="toggle"
  >
    <span class="icon-swap">
      <Transition name="icon-swap">
        <Icon
          :key="collapsed ? 'right' : 'left'"
          :icon="collapsed ? 'lucide:chevrons-right' : 'lucide:chevrons-left'"
          class="size-4"
        />
      </Transition>
    </span>
  </XhButton>
</template>

<script lang="ts" setup>
import type { DropdownOption } from 'naive-ui'
import { NDropdown } from 'naive-ui'

interface Props {
  show: boolean
  x: number
  y: number
  options: DropdownOption[]
}

defineProps<Props>()

const emit = defineEmits<{
  select: [key: string]
  close: []
}>()
</script>

<template>
  <NDropdown
    trigger="manual"
    :show="show"
    :x="x"
    :y="y"
    :options="options"
    @clickoutside="emit('close')"
    @select="(key) => emit('select', String(key))"
  >
    <span class="sr-only" />
  </NDropdown>
</template>

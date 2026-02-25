<script setup lang="ts">
import { NButton, NModal, NSpace } from 'naive-ui'

defineOptions({ name: 'CrudModal' })

const props = defineProps<{
  title: string
  submitLoading?: boolean
}>()

const emit = defineEmits<{
  submit: []
}>()

const show = defineModel<boolean>('show', { default: false })
</script>

<template>
  <NModal
    v-model:show="show"
    :title="props.title"
    preset="card"
    style="width: 560px"
    :auto-focus="false"
  >
    <slot />
    <template #footer>
      <NSpace justify="end">
        <NButton @click="show = false">
          取消
        </NButton>
        <NButton type="primary" :loading="props.submitLoading" @click="emit('submit')">
          确认
        </NButton>
      </NSpace>
    </template>
  </NModal>
</template>

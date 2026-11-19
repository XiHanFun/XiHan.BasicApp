<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script lang="ts" setup>
import type { PermissionWindowDraft } from '../grant-window'
import { XhFormRoot } from '@xihan-ui/vue'
import { computed, ref, useId, watch } from 'vue'
import { XEditModal } from '~/components'
import { emptyGrantWindow, isGrantWindowValid } from '../grant-window'
import GrantWindowFields from './GrantWindowFields.vue'

defineOptions({ name: 'GrantWindowDialog' })

const props = defineProps<{
  title: string
  /** 打开时的现值 */
  initial: PermissionWindowDraft
  /** 带原因：授予与拒绝有，成员指派没有 */
  withReason?: boolean
  loading?: boolean
}>()

const emit = defineEmits<{ submit: [window: PermissionWindowDraft] }>()

const show = defineModel<boolean>('show', { required: true })
const formId = useId()
const draft = ref<PermissionWindowDraft>(emptyGrantWindow())
const valid = computed(() => isGrantWindowValid(draft.value))

watch(show, (opened) => {
  if (opened)
    draft.value = { ...props.initial }
}, { immediate: true })

function onSubmit() {
  if (valid.value)
    emit('submit', { ...draft.value })
}
</script>

<template>
  <XEditModal v-model:show="show" :title="title" :loading="loading" :save-disabled="!valid" :form-id="formId">
    <XhFormRoot :id="formId" data-testid="grant-window-form" @submit="onSubmit">
      <GrantWindowFields v-model="draft" :with-reason="withReason" :disabled="loading" />
    </XhFormRoot>
  </XEditModal>
</template>

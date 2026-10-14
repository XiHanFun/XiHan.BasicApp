<script setup lang="ts">
import type { DirectoryOption } from './workflow-directory'
import { computed, onMounted, ref, watch } from 'vue'
import { XCombobox } from '~/components'
import { resolveUsers, searchUsers, toUserOption } from './workflow-directory'

/**
 * 审批人选人：远程检索当前上下文可见的启用用户，值为用户主键字符串（与受理人标识同形）。
 * 已选用户先按主键回显，再与检索结果合并，换一批候选时已选项仍念得出名字。
 */
defineOptions({ name: 'WorkflowUserPicker', inheritAttrs: false })

const props = withDefaults(defineProps<{
  value?: string | string[] | null
  multiple?: boolean
  placeholder?: string
  invalid?: boolean
}>(), {
  value: null,
  multiple: false,
  placeholder: undefined,
  invalid: undefined,
})

const emit = defineEmits<{
  'update:value': [value: string | string[] | null]
}>()

const selectedOptions = ref<DirectoryOption[]>([])
const searchOptions = ref<DirectoryOption[]>([])
const loading = ref(false)

const selectedIds = computed(() => {
  if (props.value === null || props.value === undefined)
    return []
  return Array.isArray(props.value) ? props.value : [props.value]
})

const options = computed(() => {
  const merged = new Map<string, DirectoryOption>()
  for (const option of [...selectedOptions.value, ...searchOptions.value])
    merged.set(option.value, option)
  return [...merged.values()]
})

async function syncSelected() {
  const missing = selectedIds.value.filter(id => !selectedOptions.value.some(option => option.value === id))
  if (missing.length === 0)
    return
  const users = await resolveUsers(selectedIds.value)
  selectedOptions.value = users.map(toUserOption)
}

async function onSearch(keyword: string) {
  loading.value = true
  try {
    searchOptions.value = (await searchUsers(keyword)).map(toUserOption)
  }
  finally {
    loading.value = false
  }
}

function onUpdate(value: string | number | Array<string | number> | null) {
  if (props.multiple) {
    const list = Array.isArray(value) ? value.map(item => String(item)) : []
    emit('update:value', list.length > 0 ? list : null)
    return
  }
  emit('update:value', value === null || value === undefined || Array.isArray(value) ? null : String(value))
}

watch(selectedIds, () => {
  void syncSelected()
})

onMounted(() => {
  void syncSelected()
  void onSearch('')
})
</script>

<template>
  <XCombobox
    v-bind="$attrs"
    :value="multiple ? selectedIds : (selectedIds[0] ?? null)"
    :options="options"
    :multiple="multiple"
    :loading="loading"
    :placeholder="placeholder"
    :invalid="invalid"
    clearable
    remote
    @search="onSearch"
    @update:value="onUpdate"
  />
</template>

<script setup lang="ts">
import type { DropdownOption } from 'naive-ui'
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type { PersonalView } from './useViewManager'
import { NButton, NDropdown, NIcon, NInput, NModal, NSelect, NSpace, useMessage } from 'naive-ui'
import { computed, ref } from 'vue'
import { Icon } from '~/iconify'

defineOptions({ name: 'SchemaViewManager' })

const props = defineProps<{
  /** 视图列表 */
  views: PersonalView[]
  /** 当前激活视图码 */
  activeCode?: string
}>()

const emit = defineEmits<{
  'apply': [code: string]
  'save': [name: string]
  'remove': [code: string]
  'setDefault': [code: string]
}>()

const message = useMessage()

const viewOptions = computed<SelectMixedOption[]>(() =>
  props.views.map((v): SelectMixedOption => ({
    label: v.isDefault ? `${v.name} ★` : v.name,
    value: v.code,
  })),
)

/** 当前选中视图的管理菜单 */
const manageOptions = computed<DropdownOption[]>(() => {
  if (!props.activeCode) {
    return []
  }
  return [
    { key: 'setDefault', label: '设为默认' },
    { key: 'remove', label: '删除视图' },
  ]
})

const saveModalVisible = ref(false)
const newViewName = ref('')

function onSelect(code: string) {
  emit('apply', code)
}

function openSaveModal() {
  newViewName.value = ''
  saveModalVisible.value = true
}

function confirmSave() {
  const name = newViewName.value.trim()
  if (!name) {
    message.warning('请输入方案名称')
    return
  }
  emit('save', name)
  saveModalVisible.value = false
}

function onManage(key: string) {
  if (!props.activeCode) {
    return
  }
  if (key === 'setDefault') {
    emit('setDefault', props.activeCode)
  }
  else if (key === 'remove') {
    emit('remove', props.activeCode)
  }
}
</script>

<template>
  <div class="flex gap-2 items-center">
    <NSelect
      :value="activeCode ?? null"
      :options="viewOptions"
      clearable
      placeholder="搜索方案"
      style="width: 160px"
      size="small"
      @update:value="(value) => value && onSelect(value as string)"
    />
    <NButton size="small" quaternary @click="openSaveModal">
      <template #icon>
        <NIcon><Icon icon="lucide:save" /></NIcon>
      </template>
      保存方案
    </NButton>
    <NDropdown v-if="manageOptions.length" :options="manageOptions" trigger="click" @select="onManage">
      <NButton size="small" quaternary circle aria-label="方案管理">
        <template #icon>
          <NIcon><Icon icon="lucide:more-horizontal" /></NIcon>
        </template>
      </NButton>
    </NDropdown>

    <NModal
      v-model:show="saveModalVisible"
      preset="card"
      title="保存搜索方案"
      style="width: 420px; max-width: 92vw"
      :auto-focus="false"
    >
      <NInput v-model:value="newViewName" placeholder="请输入方案名称" clearable @keyup.enter="confirmSave" />
      <template #footer>
        <NSpace justify="end">
          <NButton @click="saveModalVisible = false">
            取消
          </NButton>
          <NButton type="primary" @click="confirmSave">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

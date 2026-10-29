<script setup lang="ts">
import type { RoleParentOption } from '../role-parents'
import type { RoleRow } from '../use-roles'
import { XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XCombobox, XEditModal } from '~/components'
import { toast } from '~/composables'
import { roles as rolesApi } from '~/contracts'
import { buildRoleParentOptions, directParentIds, sameParents } from '../role-parents'

/**
 * 设置上级角色：本角色继承所选上级经启用角色可达的授权，上级的拒绝同样作用于本角色。
 * 一次提交整组直接上级（空即解除全部继承）。
 */
defineOptions({ name: 'RoleParentsDialog' })

const props = defineProps<{
  show: boolean
  role: RoleRow | null
}>()

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
  (e: 'saved'): void
}>()

/** 可选上级的数量上限，与后端分页的页大小上限一致 */
const CANDIDATE_LIMIT = 500

const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
const options = ref<RoleParentOption[]>([])
const currentParents = ref<string[]>([])
const selected = ref<string[]>([])

const title = computed(() => t('access.roles.parents.title', { name: props.role?.name ?? '' }))

/** 候选值都是字符串，多选收上来的是数组 */
function onSelect(value: unknown) {
  selected.value = Array.isArray(value) ? value.map(String) : []
}

async function load(role: RoleRow) {
  loading.value = true
  options.value = []
  currentParents.value = []
  selected.value = []
  try {
    const [candidates, hierarchy] = await Promise.all([
      rolesApi.list({ page: { index: 1, size: CANDIDATE_LIMIT }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [{ field: 'order', descending: false }] }),
      rolesApi.hierarchy({ id: role.id }),
    ])
    options.value = buildRoleParentOptions(role.id, candidates.items, hierarchy.ancestors, hierarchy.descendants, {
      descendant: t('access.roles.parents.note_descendant'),
      indirectAncestor: t('access.roles.parents.note_indirect'),
      disabledParent: t('access.roles.parents.note_disabled'),
    })
    currentParents.value = directParentIds(hierarchy.ancestors)
    selected.value = [...currentParents.value]
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('access.roles.parents.load_failed'))
  }
  finally {
    loading.value = false
  }
}

watch(
  () => [props.show, props.role?.id] as const,
  ([show]) => {
    if (show && props.role)
      void load(props.role)
  },
  { immediate: true },
)

function close() {
  emit('update:show', false)
}

async function save() {
  const role = props.role
  if (!role)
    return

  if (sameParents(currentParents.value, selected.value)) {
    close()
    return
  }

  saving.value = true
  try {
    await rolesApi.setParents({ id: role.id, parentIds: selected.value })
    toast.success(t('common.messages.save_success'))
    emit('saved')
    close()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    saving.value = false
  }
}
</script>

<template>
  <XEditModal
    :show="show"
    :title="title"
    :loading="saving"
    :save-disabled="loading"
    :width="560"
    @update:show="(value: boolean) => emit('update:show', value)"
    @save="save"
  >
    <!-- 候选与现有上级读回来之前遮住，免得空选择框被当成「没有上级」 -->
    <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
      <div class="xh-loading-stage__veil">
        <XhSpinner />
      </div>
      <XhFieldRoot>
        <XhFieldLabel>{{ t('access.roles.parents.label') }}</XhFieldLabel>
        <XhFieldControl>
          <XCombobox
            class="role-parents__picker"
            :options="options"
            :value="selected"
            :loading="loading"
            :placeholder="t('access.roles.parents.placeholder')"
            multiple
            @update:value="onSelect"
          />
        </XhFieldControl>
        <XhFieldDescription>{{ t('access.roles.parents.hint') }}</XhFieldDescription>
      </XhFieldRoot>
    </div>
  </XEditModal>
</template>

<style scoped>
/* 组合框默认按内容定宽，这里与字段一样铺满 */
.role-parents__picker {
  inline-size: 100%;
}
</style>

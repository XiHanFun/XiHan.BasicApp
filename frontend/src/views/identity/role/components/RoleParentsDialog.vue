<script setup lang="ts">
import type { RoleParentOption } from '../role-parents'
import type { RoleListItemDto } from '@/api'
import { XhFieldControl, XhFieldDescription, XhFieldLabel, XhFieldRoot, XhSpinner } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { roleApi, roleHierarchyApi } from '@/api'
import { XCombobox, XEditModal } from '~/components'
import { toast } from '~/composables'
import { buildRoleParentOptions, diffRoleParents, directParentIds } from '../role-parents'

/**
 * 设置上级角色：本角色继承所选上级经启用角色可达的授权，上级的拒绝同样作用于本角色。
 * 入口挂「设置上级」按钮；本次含解除时后端另校验「解除上级」对应的权限。
 */
defineOptions({ name: 'RoleParentsDialog' })

const props = defineProps<{
  show: boolean
  role: RoleListItemDto | null
}>()

const emit = defineEmits<{
  (e: 'update:show', value: boolean): void
  (e: 'saved'): void
}>()

/** 可选上级的数量上限，与角色选择接口的上限一致 */
const CANDIDATE_LIMIT = 500

const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
const options = ref<RoleParentOption[]>([])
const currentParents = ref<string[]>([])
const selected = ref<string[]>([])

const title = computed(() => t('identity.role.parents_title', { name: props.role?.roleName ?? '' }))

/** 候选值都是字符串，多选收上来的是数组 */
function onSelect(value: unknown) {
  selected.value = Array.isArray(value) ? value.map(String) : []
}

async function load(role: RoleListItemDto) {
  loading.value = true
  options.value = []
  currentParents.value = []
  selected.value = []
  try {
    const [candidates, ancestors, descendants] = await Promise.all([
      roleApi.enabledList({ limit: CANDIDATE_LIMIT }),
      roleHierarchyApi.ancestors(role.basicId),
      roleHierarchyApi.descendants(role.basicId),
    ])
    options.value = buildRoleParentOptions(role.basicId, candidates, ancestors, descendants, {
      descendant: t('identity.role.parent_note_descendant'),
      indirectAncestor: t('identity.role.parent_note_indirect'),
      disabledParent: t('identity.role.parent_note_disabled'),
    })
    currentParents.value = directParentIds(ancestors)
    selected.value = [...currentParents.value]
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.role.msg_load_parents_failed'))
  }
  finally {
    loading.value = false
  }
}

watch(
  () => [props.show, props.role?.basicId] as const,
  ([show]) => {
    if (show && props.role) {
      void load(props.role)
    }
  },
  { immediate: true },
)

function close() {
  emit('update:show', false)
}

async function save() {
  const role = props.role
  if (!role) {
    return
  }

  const { addParentRoleIds, removeParentRoleIds } = diffRoleParents(currentParents.value, selected.value)
  if (addParentRoleIds.length === 0 && removeParentRoleIds.length === 0) {
    close()
    return
  }

  saving.value = true
  try {
    await roleHierarchyApi.batchUpdateParents({ roleId: role.basicId, addParentRoleIds, removeParentRoleIds })
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
        <XhFieldLabel>{{ t('identity.role.label_parents') }}</XhFieldLabel>
        <XhFieldControl>
          <XCombobox
            class="role-parents__picker"
            :options="options"
            :value="selected"
            :loading="loading"
            :placeholder="t('identity.role.ph_parents')"
            multiple
            @update:value="onSelect"
          />
        </XhFieldControl>
        <XhFieldDescription>{{ t('identity.role.hint_parents') }}</XhFieldDescription>
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

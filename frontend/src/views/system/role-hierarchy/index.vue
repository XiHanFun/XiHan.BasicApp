<script setup lang="ts">
import type { VxeGridInstance, VxeGridPropTypes } from 'vxe-table'
import type {
  ApiId,
  RoleHierarchyCreateDto,
  RoleHierarchyListItemDto,
  RoleSelectItemDto,
} from '@/api'
import {
  NButton,
  NCheckbox,
  NForm,
  NFormItem,
  NIcon,
  NInput,
  NModal,
  NPopconfirm,
  NSelect,
  NSpace,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref } from 'vue'
import {
  EnableStatus,
  roleApi,
  roleHierarchyApi,
  RoleType,
} from '@/api'
import { Icon, XSystemQueryPanel } from '~/components'
import { useVxeTable } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemRoleHierarchyPage' })

type HierarchyMode = 'ancestors' | 'descendants'

interface RoleHierarchyGridResult {
  items: RoleHierarchyListItemDto[]
  total: number
}

interface RoleOption {
  isGlobal: boolean
  label: string
  roleType: RoleType
  value: ApiId
}

interface RoleHierarchyFormModel {
  ancestorId: ApiId | null
  descendantId: ApiId | null
  remark: string | null
}

const message = useMessage()
const xGrid = ref<VxeGridInstance<RoleHierarchyListItemDto>>()
const selectedRoleId = ref<ApiId | null>(null)
const roleOptions = ref<RoleOption[]>([])
const roleLoading = ref(false)
const modalVisible = ref(false)
const submitLoading = ref(false)
const hierarchyForm = ref<RoleHierarchyFormModel>(createDefaultForm())

const queryParams = reactive({
  includeSelf: true,
  keyword: '',
  mode: 'descendants' as HierarchyMode,
})

const roleFilter = reactive({
  keyword: '',
})

const modeOptions = [
  { label: '后代链', value: 'descendants' },
  { label: '祖先链', value: 'ancestors' },
]

const roleTypeOptions = [
  { label: '系统', value: RoleType.System },
  { label: '业务', value: RoleType.Business },
  { label: '自定义', value: RoleType.Custom },
]

const enableStatusOptions = [
  { label: '启用', value: EnableStatus.Enabled },
  { label: '禁用', value: EnableStatus.Disabled },
]

const modalTitle = computed(() => '新增角色继承')

function createDefaultForm(): RoleHierarchyFormModel {
  return {
    ancestorId: selectedRoleId.value,
    descendantId: null,
    remark: null,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function toRoleOption(item: RoleSelectItemDto): RoleOption {
  const scopeName = item.isGlobal ? '全局' : '租户'

  return {
    isGlobal: item.isGlobal,
    label: `[${scopeName}] ${item.roleName} (${item.roleCode})`,
    roleType: item.roleType,
    value: item.basicId,
  }
}

function mergeOptions(current: RoleOption[], next: RoleOption[]) {
  const optionMap = new Map<ApiId, RoleOption>()

  for (const option of current) {
    optionMap.set(option.value, option)
  }

  for (const option of next) {
    optionMap.set(option.value, option)
  }

  return [...optionMap.values()]
}

async function loadRoleOptions(keyword = roleFilter.keyword) {
  roleLoading.value = true
  roleFilter.keyword = keyword
  try {
    const items = await roleApi.enabledList({
      keyword: normalizeNullable(keyword),
      limit: 80,
    })
    const options = items.map(toRoleOption)
    roleOptions.value = mergeOptions(roleOptions.value, options)

    const firstRole = options[0]
    if (selectedRoleId.value === null && firstRole) {
      selectedRoleId.value = firstRole.value
      xGrid.value?.commitProxy('reload')
    }
  }
  catch {
    message.error('加载角色选项失败')
  }
  finally {
    roleLoading.value = false
  }
}

function includesKeyword(row: RoleHierarchyListItemDto, keyword: string) {
  const text = [
    row.ancestorRoleName,
    row.ancestorRoleCode,
    row.descendantRoleName,
    row.descendantRoleCode,
    row.path,
    row.remark,
  ]
    .filter(Boolean)
    .join(' ')
    .toLowerCase()

  return text.includes(keyword)
}

function filterRows(rows: RoleHierarchyListItemDto[]) {
  const keyword = normalizeNullable(queryParams.keyword)?.toLowerCase()

  return rows.filter((row) => {
    if (keyword && !includesKeyword(row, keyword)) {
      return false
    }

    return true
  })
}

function pageRows(rows: RoleHierarchyListItemDto[], page: VxeGridPropTypes.ProxyAjaxQueryPageParams) {
  const start = (page.currentPage - 1) * page.pageSize
  return rows.slice(start, start + page.pageSize)
}

async function handleQueryApi(page: VxeGridPropTypes.ProxyAjaxQueryPageParams): Promise<RoleHierarchyGridResult> {
  if (selectedRoleId.value === null) {
    return {
      items: [],
      total: 0,
    }
  }

  try {
    const rows = queryParams.mode === 'ancestors'
      ? await roleHierarchyApi.ancestors(selectedRoleId.value, queryParams.includeSelf)
      : await roleHierarchyApi.descendants(selectedRoleId.value, queryParams.includeSelf)
    const filteredRows = filterRows(rows)

    return {
      items: pageRows(filteredRows, page),
      total: filteredRows.length,
    }
  }
  catch {
    message.error('查询角色继承失败')
    return {
      items: [],
      total: 0,
    }
  }
}

const tableOptions = useVxeTable<RoleHierarchyListItemDto>(
  {
    columns: [
      { fixed: 'left', title: '序号', type: 'seq', width: 60 },
      { field: 'ancestorRoleName', minWidth: 160, showOverflow: 'tooltip', title: '祖先角色' },
      { field: 'ancestorRoleCode', minWidth: 180, showOverflow: 'tooltip', title: '祖先编码' },
      {
        field: 'ancestorRoleType',
        formatter: ({ cellValue }) => getOptionLabel(roleTypeOptions, cellValue),
        minWidth: 110,
        title: '祖先类型',
      },
      {
        field: 'ancestorStatus',
        slots: { default: 'col_ancestor_status' },
        title: '祖先状态',
        width: 100,
      },
      { field: 'descendantRoleName', minWidth: 160, showOverflow: 'tooltip', title: '后代角色' },
      { field: 'descendantRoleCode', minWidth: 180, showOverflow: 'tooltip', title: '后代编码' },
      {
        field: 'descendantRoleType',
        formatter: ({ cellValue }) => getOptionLabel(roleTypeOptions, cellValue),
        minWidth: 110,
        title: '后代类型',
      },
      {
        field: 'descendantStatus',
        slots: { default: 'col_descendant_status' },
        title: '后代状态',
        width: 100,
      },
      { field: 'depth', minWidth: 90, title: '深度' },
      { field: 'path', minWidth: 180, showOverflow: 'tooltip', title: '路径' },
      {
        field: 'createdTime',
        formatter: ({ cellValue }) => formatDate(cellValue),
        minWidth: 170,
        title: '创建时间',
      },
      {
        field: 'actions',
        fixed: 'right',
        slots: { default: 'col_actions' },
        title: '操作',
        width: 56,
      },
    ],
    id: 'sys_role_hierarchy',
    name: '角色继承',
  },
  {
    proxyConfig: {
      autoLoad: true,
      ajax: {
        query: ({ page }) => handleQueryApi(page),
      },
    },
  },
)

function handleRoleChanged() {
  xGrid.value?.commitProxy('reload')
}

function handleSearch() {
  xGrid.value?.commitProxy('reload')
}

function handleReset() {
  queryParams.includeSelf = true
  queryParams.keyword = ''
  queryParams.mode = 'descendants'
  xGrid.value?.commitProxy('reload')
}

function handleAdd() {
  hierarchyForm.value = createDefaultForm()
  modalVisible.value = true
  void loadRoleOptions()
}

function handleRoleSearch(keyword: string) {
  void loadRoleOptions(keyword)
}

function isMaintainableDescendant(roleId: ApiId | null) {
  if (roleId === null) {
    return false
  }

  const role = roleOptions.value.find(option => option.value === roleId)
  return Boolean(role && !role.isGlobal && role.roleType !== RoleType.System)
}

function validateForm() {
  if (hierarchyForm.value.ancestorId === null) {
    message.warning('请选择祖先角色')
    return false
  }

  if (hierarchyForm.value.descendantId === null) {
    message.warning('请选择后代角色')
    return false
  }

  if (hierarchyForm.value.ancestorId === hierarchyForm.value.descendantId) {
    message.warning('角色不能继承自己')
    return false
  }

  if (!isMaintainableDescendant(hierarchyForm.value.descendantId)) {
    message.warning('平台全局角色或系统角色不能作为后代角色维护')
    return false
  }

  return true
}

async function handleSubmit() {
  if (!validateForm() || hierarchyForm.value.ancestorId === null || hierarchyForm.value.descendantId === null) {
    return
  }

  submitLoading.value = true
  try {
    const createInput: RoleHierarchyCreateDto = {
      ancestorId: hierarchyForm.value.ancestorId,
      descendantId: hierarchyForm.value.descendantId,
      remark: normalizeNullable(hierarchyForm.value.remark),
    }

    await roleHierarchyApi.create(createInput)
    selectedRoleId.value = createInput.descendantId
    queryParams.mode = 'ancestors'
    message.success('保存成功')
    modalVisible.value = false
    xGrid.value?.commitProxy('reload')
  }
  catch {
    message.error('保存失败')
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: RoleHierarchyListItemDto) {
  await roleHierarchyApi.delete(row.basicId)
  message.success('角色继承已删除')
  xGrid.value?.commitProxy('reload')
}

function canDelete(row: RoleHierarchyListItemDto) {
  return row.depth === 1 && row.isDescendantGlobal !== true && row.descendantRoleType !== RoleType.System
}

void loadRoleOptions()
</script>

<template>
  <div class="flex overflow-hidden flex-col gap-2 p-3 h-full">
    <XSystemQueryPanel>
      <div class="xh-query-panel__content">
        <NSelect
          v-model:value="selectedRoleId"
          :loading="roleLoading"
          :options="roleOptions"
          filterable
          placeholder="选择角色"
          remote
          style="width: 260px"
          @search="loadRoleOptions"
          @update:value="handleRoleChanged"
        />
        <NSelect
          v-model:value="queryParams.mode"
          :options="modeOptions"
          placeholder="链路方向"
          style="width: 110px"
          @update:value="handleSearch"
        />
        <NCheckbox v-model:checked="queryParams.includeSelf" @update:checked="handleSearch">
          含自身
        </NCheckbox>
        <vxe-input
          v-model="queryParams.keyword"
          clearable
          placeholder="搜索角色/路径/备注"
          style="width: 220px"
          @keyup.enter="handleSearch"
        />
        <NButton size="small" type="primary" @click="handleSearch">
          <template #icon>
            <NIcon><Icon icon="lucide:search" /></NIcon>
          </template>
          查询
        </NButton>
        <NButton size="small" @click="handleReset">
          <template #icon>
            <NIcon><Icon icon="lucide:rotate-ccw" /></NIcon>
          </template>
          重置
        </NButton>
      </div>
    </XSystemQueryPanel>

    <vxe-card class="flex-1" style="height: 0">
      <vxe-grid ref="xGrid" v-bind="tableOptions">
        <template #toolbar_buttons>
          <NButton size="small" type="primary" @click="handleAdd">
            <template #icon>
              <NIcon><Icon icon="lucide:git-merge" /></NIcon>
            </template>
            新增继承
          </NButton>
        </template>

        <template #col_ancestor_status="{ row }">
          <NTag
            :type="row.ancestorStatus === EnableStatus.Enabled ? 'success' : 'error'"
            round
            size="small"
          >
            {{ getOptionLabel(enableStatusOptions, row.ancestorStatus) || '-' }}
          </NTag>
        </template>

        <template #col_descendant_status="{ row }">
          <NTag
            :type="row.descendantStatus === EnableStatus.Enabled ? 'success' : 'error'"
            round
            size="small"
          >
            {{ getOptionLabel(enableStatusOptions, row.descendantStatus) || '-' }}
          </NTag>
        </template>

        <template #col_actions="{ row }">
          <NPopconfirm :disabled="!canDelete(row)" @positive-click="handleDelete(row)">
            <template #trigger>
              <!-- 操作列仅图标 -->
              <NButton :disabled="!canDelete(row)" aria-label="删除" circle quaternary size="small" type="error">
                <template #icon>
                  <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                </template>
              </NButton>
            </template>
            确认删除该直接继承关系？
          </NPopconfirm>
        </template>
      </vxe-grid>
    </vxe-card>

    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 680px; max-width: 92vw"
    >
      <NForm :model="hierarchyForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem label="祖先角色" path="ancestorId">
          <NSelect
            v-model:value="hierarchyForm.ancestorId"
            :loading="roleLoading"
            :options="roleOptions"
            filterable
            placeholder="搜索并选择祖先角色"
            remote
            @focus="loadRoleOptions()"
            @search="handleRoleSearch"
          />
        </NFormItem>
        <NFormItem label="后代角色" path="descendantId">
          <NSelect
            v-model:value="hierarchyForm.descendantId"
            :loading="roleLoading"
            :options="roleOptions"
            filterable
            placeholder="搜索并选择后代角色"
            remote
            @focus="loadRoleOptions()"
            @search="handleRoleSearch"
          />
        </NFormItem>
        <NFormItem label="备注" path="remark">
          <NInput v-model:value="hierarchyForm.remark" clearable placeholder="请输入备注" />
        </NFormItem>
      </NForm>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            取消
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            保存
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </div>
</template>

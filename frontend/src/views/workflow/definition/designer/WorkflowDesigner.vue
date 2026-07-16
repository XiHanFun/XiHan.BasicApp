<script setup lang="ts">
import type { ActivityTypeMeta } from './catalog'
import type { DefinitionMeta, DesignerEdgeData, DesignerNodeData } from './transform'
import type { DiagramApi, DiagramEdgeEventPayload } from '~/diagram'
import {
  NButton,
  NCheckbox,
  NDivider,
  NDynamicInput,
  NDynamicTags,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSelect,
  NSpace,
  NSwitch,
  NTabPane,
  NTabs,
  NTag,
  useMessage,
} from 'naive-ui'
import { computed, reactive, ref, toRaw, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/components'
import { registerVueShape, XDiagram } from '~/diagram'
import ActivityNode from './ActivityNode.vue'
import { ACTIVITY_CATALOG, ACTIVITY_MAP, CATEGORY_ORDER } from './catalog'
import {
  ACTIVITY_SHAPE,
  autoLayout,
  edgeLabel,
  parseDefinition,
  portsOf,
  serializeDefinition,
  validateGraph,
} from './transform'

const props = defineProps<{
  initialJson: string
  saving?: boolean
}>()

const emit = defineEmits<{
  save: [json: string]
}>()

const { t } = useI18n()
const message = useMessage()

// 幂等注册工作流活动形状（首次进入设计器时生效）
registerVueShape({ shape: ACTIVITY_SHAPE, component: ActivityNode, width: 172, height: 64 })

// ── 画布 API 与装载 ────────────────────────────────────────────
let api: DiagramApi | null = null
let pendingJson: string | null = null

// ── 定义级元数据与原始透传对象 ─────────────────────────────────
let rawDefinition: Record<string, unknown> = {}
const meta = reactive<DefinitionMeta>({
  code: '',
  name: '',
  description: '',
  category: '',
  enableCompensation: false,
  variables: [],
})

const activeTab = ref<'design' | 'json'>('design')
const jsonText = ref('')

/** 选中节点：id + 表单副本（写回经 updateNodeData 全量替换） */
const selectedNodeId = ref<string | null>(null)
const nodeForm = ref<DesignerNodeData | null>(null)
/** 选中连线：id + 表单副本 */
const selectedEdgeId = ref<string | null>(null)
const edgeForm = ref<(DesignerEdgeData & { source: string, target: string }) | null>(null)

/** JSON 类属性的文本缓冲（键 = 属性名） */
const jsonBuffers = reactive<Record<string, string>>({})
const jsonErrors = reactive<Record<string, boolean>>({})

/**
 * 生成脱离响应式代理的纯对象快照。
 * nodeForm 是 ref → 其 .value 是 reactive 代理，structuredClone 无法克隆代理（DataCloneError）；
 * 先 toRaw 拿到底层普通对象再克隆（节点数据为纯 JSON，底层嵌套均为普通值）。
 */
function snapshot<T>(value: T): T {
  return structuredClone(toRaw(value))
}

function clearSelection() {
  if (api && selectedNodeId.value && nodeForm.value)
    api.updateNodeData(selectedNodeId.value, { ...snapshot(nodeForm.value), __selected: false })
  selectedNodeId.value = null
  nodeForm.value = null
  selectedEdgeId.value = null
  edgeForm.value = null
}

function loadFromJson(json: string): boolean {
  try {
    const parsed = parseDefinition(json)
    rawDefinition = parsed.raw
    Object.assign(meta, parsed.meta)
    if (api)
      api.load(parsed.data)
    else
      pendingJson = json
    clearSelection()
    return true
  }
  catch {
    message.error(t('workflow.designer.err_parse'))
    return false
  }
}

function onDiagramReady(readyApi: DiagramApi) {
  api = readyApi

  api.on('node:click', ({ id, data }) => {
    selectNode(id, data as DesignerNodeData)
  })
  api.on('edge:click', (payload) => {
    selectEdge(payload)
  })
  api.on('blank:click', () => {
    clearSelection()
  })
  api.on('edge:connected', (payload) => {
    // 新连线补默认业务数据并直接进入属性编辑
    const data: DesignerEdgeData = { condition: null, priority: 0, isDefault: false, name: null }
    api!.updateEdge(payload.id, { label: '', dashed: false, data })
    selectEdge({ ...payload, data })
  })
  api.on('cell:removed', ({ id }) => {
    if (selectedNodeId.value === id || selectedEdgeId.value === id) {
      selectedNodeId.value = null
      nodeForm.value = null
      selectedEdgeId.value = null
      edgeForm.value = null
    }
  })

  if (pendingJson) {
    const json = pendingJson
    pendingJson = null
    loadFromJson(json)
  }
}

watch(() => props.initialJson, (json) => {
  if (json)
    loadFromJson(json)
}, { immediate: true })

// ── 选中态 ─────────────────────────────────────────────────────
const selectedMeta = computed(() => nodeForm.value ? ACTIVITY_MAP[nodeForm.value.activityType] : undefined)

function selectNode(id: string, data: DesignerNodeData) {
  clearSelection()
  selectedNodeId.value = id
  nodeForm.value = snapshot({ ...data, __selected: true })
  commitNode()
  syncJsonBuffers()
}

function selectEdge(payload: DiagramEdgeEventPayload) {
  clearSelection()
  selectedEdgeId.value = payload.id
  const data = (payload.data ?? {}) as DesignerEdgeData
  edgeForm.value = {
    source: payload.source,
    target: payload.target,
    condition: data.condition ?? null,
    priority: data.priority ?? 0,
    isDefault: data.isDefault ?? false,
    name: data.name ?? null,
  }
}

/** 节点表单写回画布（全量替换节点数据） */
function commitNode() {
  if (api && selectedNodeId.value && nodeForm.value)
    api.updateNodeData(selectedNodeId.value, snapshot(nodeForm.value))
}

/** 连线表单写回画布（标签/虚线随业务数据联动） */
function commitEdge() {
  if (!api || !selectedEdgeId.value || !edgeForm.value)
    return
  const data: DesignerEdgeData = {
    condition: edgeForm.value.condition ?? null,
    priority: edgeForm.value.priority ?? 0,
    isDefault: edgeForm.value.isDefault ?? false,
    name: edgeForm.value.name ?? null,
  }
  api.updateEdge(selectedEdgeId.value, {
    label: edgeLabel(data),
    dashed: data.isDefault ?? false,
    data,
  })
}

// ── 节点属性编辑 ───────────────────────────────────────────────
function syncJsonBuffers() {
  for (const key of Object.keys(jsonBuffers)) {
    delete jsonBuffers[key]
    delete jsonErrors[key]
  }
  if (!nodeForm.value || !selectedMeta.value)
    return
  for (const descriptor of selectedMeta.value.props) {
    if (descriptor.input === 'json') {
      const value = nodeForm.value.properties[descriptor.key]
      jsonBuffers[descriptor.key] = value === undefined || value === null ? '' : JSON.stringify(value, null, 2)
    }
  }
}

function applyJsonBuffer(key: string) {
  if (!nodeForm.value)
    return
  const text = jsonBuffers[key]?.trim()
  if (!text) {
    delete nodeForm.value.properties[key]
    jsonErrors[key] = false
    commitNode()
    return
  }
  try {
    nodeForm.value.properties[key] = JSON.parse(text)
    jsonErrors[key] = false
    commitNode()
  }
  catch {
    jsonErrors[key] = true
  }
}

function setNodeProp(key: string, value: unknown) {
  if (!nodeForm.value)
    return
  if (value === undefined || value === null || value === '')
    delete nodeForm.value.properties[key]
  else
    nodeForm.value.properties[key] = value
  commitNode()
}

function nodeProp<T>(key: string): T | undefined {
  return nodeForm.value?.properties[key] as T | undefined
}

const retryEnabled = computed({
  get: () => Boolean(nodeForm.value?.retryPolicy),
  set: (enabled: boolean) => {
    if (!nodeForm.value)
      return
    nodeForm.value.retryPolicy = enabled
      ? { maxAttempts: 3, firstDelaySeconds: 10, backoffFactor: 2 }
      : null
    commitNode()
  },
})

function deleteSelectedNode() {
  if (api && selectedNodeId.value)
    api.removeCells([selectedNodeId.value])
}

function deleteSelectedEdge() {
  if (api && selectedEdgeId.value)
    api.removeCells([selectedEdgeId.value])
}

/** 源节点的常见结果值 → 一键填充条件表达式 */
const outcomeHints = computed(() => {
  if (!edgeForm.value || !api)
    return []
  const source = api.toData().nodes.find(node => node.id === edgeForm.value!.source)
  const sourceMeta = source ? ACTIVITY_MAP[(source.data as DesignerNodeData).activityType] : undefined
  return sourceMeta?.outcomes ?? []
})

function applyOutcomeHint(outcome: string) {
  if (!edgeForm.value)
    return
  edgeForm.value.condition = `outcome == '${outcome}'`
  commitEdge()
}

// ── 节点面板 ───────────────────────────────────────────────────
const paletteGroups = computed(() => CATEGORY_ORDER.map(category => ({
  category,
  label: t(`workflow.designer.category.${category}`),
  items: ACTIVITY_CATALOG.filter(item => item.categoryKey === category),
})))

let nodeSequence = 0
function nextNodeId(type: string): string {
  const existing = new Set(api?.toData().nodes.map(node => node.id) ?? [])
  const prefix = type.charAt(0).toLowerCase() + type.slice(1)
  let id = ''
  do {
    nodeSequence++
    id = `${prefix}_${nodeSequence}`
  } while (existing.has(id))
  return id
}

function createNode(item: ActivityTypeMeta, position: { x: number, y: number }) {
  if (!api)
    return
  api.addNode({
    id: nextNodeId(item.type),
    shape: ACTIVITY_SHAPE,
    x: position.x,
    y: position.y,
    ports: portsOf(item.type),
    data: {
      activityType: item.type,
      name: t(`workflow.designer.activity.${item.labelKey}`),
      properties: {},
      retryPolicy: null,
      timeoutSeconds: null,
      continueOnError: false,
    } satisfies DesignerNodeData,
  })
}

function onPaletteClick(item: ActivityTypeMeta) {
  const count = api?.toData().nodes.length ?? 0
  createNode(item, { x: 120 + (count % 4) * 220, y: 120 + Math.floor(count / 4) * 130 })
}

function onPaletteDragStart(event: DragEvent, item: ActivityTypeMeta) {
  event.dataTransfer?.setData('application/xihan-activity', item.type)
  if (event.dataTransfer)
    event.dataTransfer.effectAllowed = 'move'
}

function onCanvasDrop(event: DragEvent) {
  const type = event.dataTransfer?.getData('application/xihan-activity')
  if (!type || !ACTIVITY_MAP[type] || !api)
    return
  createNode(ACTIVITY_MAP[type], api.clientToLocal(event.clientX, event.clientY))
}

// ── JSON 视图 ──────────────────────────────────────────────────
function currentJson(): string {
  if (!api)
    return props.initialJson
  const data = api.toData()
  // 序列化前剥离设计器态标记
  for (const node of data.nodes) {
    if (node.data)
      delete node.data.__selected
  }
  return serializeDefinition(rawDefinition, meta, data)
}

watch(activeTab, (tab) => {
  if (tab === 'json')
    jsonText.value = currentJson()
})

function applyJsonToCanvas() {
  if (loadFromJson(jsonText.value)) {
    activeTab.value = 'design'
    message.success(t('workflow.designer.msg_json_applied'))
  }
}

// ── 保存 ───────────────────────────────────────────────────────
function handleSave() {
  if (activeTab.value === 'json' && !loadFromJson(jsonText.value))
    return
  if (!api)
    return

  const data = api.toData()
  for (const node of data.nodes) {
    if (node.data)
      delete node.data.__selected
  }
  const errors = validateGraph(meta, data)
  if (errors.length > 0) {
    message.warning(t(`workflow.designer.err_${errors[0]}`))
    return
  }
  emit('save', serializeDefinition(rawDefinition, meta, data))
}

function handleAutoLayout() {
  if (!api)
    return
  const data = api.toData()
  autoLayout(data)
  api.load(data)
}
</script>

<template>
  <div class="flex h-[78vh] flex-col">
    <!-- 工具栏 -->
    <div class="mb-2 flex items-center gap-2">
      <NTag size="small" bordered>
        {{ meta.code || t('workflow.designer.untitled') }}
      </NTag>
      <span class="text-sm text-gray-500">{{ meta.name }}</span>
      <div class="flex-1" />
      <NButton size="small" @click="handleAutoLayout">
        <template #icon>
          <Icon icon="lucide:layout-dashboard" />
        </template>
        {{ t('workflow.designer.btn_layout') }}
      </NButton>
      <NButton size="small" type="primary" :loading="saving" @click="handleSave">
        <template #icon>
          <Icon icon="lucide:save" />
        </template>
        {{ t('workflow.designer.btn_save') }}
      </NButton>
    </div>

    <NTabs v-model:value="activeTab" type="segment" size="small" class="min-h-0 flex-1">
      <!-- 设计视图 -->
      <NTabPane name="design" :tab="t('workflow.designer.tab_design')">
        <!-- 明确高度：不依赖 NTabs 内部 flex 链，避免画布塌陷为零高 -->
        <div class="flex h-[68vh] min-h-0 gap-0">
          <!-- 节点面板 -->
          <div class="w-44 shrink-0 overflow-auto border-r border-gray-200 pr-2 dark:border-gray-700">
            <div v-for="group in paletteGroups" :key="group.category" class="mb-2">
              <div class="mb-1 text-xs font-medium text-gray-400">
                {{ group.label }}
              </div>
              <div
                v-for="item in group.items"
                :key="item.type"
                class="mb-1 flex cursor-grab items-center gap-2 rounded border border-gray-200 px-2 py-1.5 text-sm hover:border-primary hover:text-primary dark:border-gray-600"
                draggable="true"
                @dragstart="onPaletteDragStart($event, item)"
                @click="onPaletteClick(item)"
              >
                <Icon :icon="item.icon" class="text-sm" />
                <span class="truncate">{{ t(`workflow.designer.activity.${item.labelKey}`) }}</span>
              </div>
            </div>
          </div>

          <!-- 画布（~/diagram 封装，Shift 框选 / 滚轮缩放 / Del 删除 / Ctrl+Z 撤销） -->
          <div class="min-w-0 flex-1" @drop="onCanvasDrop" @dragover.prevent>
            <XDiagram class="h-full" @ready="onDiagramReady" />
          </div>

          <!-- 属性面板 -->
          <div class="w-80 shrink-0 overflow-auto border-l border-gray-200 pl-3 dark:border-gray-700">
            <!-- 连线属性 -->
            <template v-if="edgeForm">
              <div class="mb-2 text-sm font-medium">
                {{ t('workflow.designer.edge_panel') }}
              </div>
              <div class="mb-2 text-xs text-gray-400">
                {{ edgeForm.source }} → {{ edgeForm.target }}
              </div>
              <NForm label-placement="top" size="small">
                <NFormItem :label="t('workflow.designer.edge_condition')">
                  <NInput
                    :value="edgeForm.condition ?? ''"
                    type="textarea"
                    :autosize="{ minRows: 2, maxRows: 4 }"
                    :placeholder="t('workflow.designer.edge_condition_placeholder')"
                    @update:value="(value: string) => { edgeForm!.condition = value || null; commitEdge() }"
                  />
                </NFormItem>
                <div v-if="outcomeHints.length > 0" class="mb-2">
                  <NSpace :size="4">
                    <NTag
                      v-for="outcome in outcomeHints"
                      :key="outcome"
                      size="small"
                      class="cursor-pointer"
                      @click="applyOutcomeHint(outcome)"
                    >
                      {{ outcome }}
                    </NTag>
                  </NSpace>
                </div>
                <NFormItem :label="t('workflow.designer.edge_priority')">
                  <NInputNumber
                    :value="edgeForm.priority ?? 0"
                    class="w-full"
                    @update:value="(value: number | null) => { edgeForm!.priority = value ?? 0; commitEdge() }"
                  />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.edge_is_default')">
                  <NSwitch
                    :value="edgeForm.isDefault ?? false"
                    @update:value="(value: boolean) => { edgeForm!.isDefault = value; commitEdge() }"
                  />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.edge_name')">
                  <NInput
                    :value="edgeForm.name ?? ''"
                    @update:value="(value: string) => { edgeForm!.name = value || null; commitEdge() }"
                  />
                </NFormItem>
              </NForm>
              <NButton size="small" type="error" secondary block @click="deleteSelectedEdge">
                {{ t('workflow.designer.btn_delete_edge') }}
              </NButton>
            </template>

            <!-- 节点属性 -->
            <template v-else-if="nodeForm">
              <div class="mb-2 flex items-center gap-2 text-sm font-medium">
                <Icon :icon="selectedMeta?.icon ?? 'lucide:box'" />
                {{ selectedMeta ? t(`workflow.designer.activity.${selectedMeta.labelKey}`) : nodeForm.activityType }}
                <span class="text-xs font-normal text-gray-400">{{ selectedNodeId }}</span>
              </div>
              <NForm label-placement="top" size="small">
                <NFormItem :label="t('workflow.designer.node_name')">
                  <NInput
                    :value="nodeForm.name"
                    @update:value="(value: string) => { nodeForm!.name = value; commitNode() }"
                  />
                </NFormItem>

                <!-- 活动属性（类型驱动） -->
                <template v-for="descriptor in selectedMeta?.props ?? []" :key="descriptor.key">
                  <NFormItem :label="t(`workflow.designer.prop.${descriptor.labelKey}`)">
                    <NInput
                      v-if="descriptor.input === 'text'"
                      :value="(nodeProp<string>(descriptor.key)) ?? ''"
                      :placeholder="descriptor.placeholder"
                      @update:value="(value: string) => setNodeProp(descriptor.key, value)"
                    />
                    <NInput
                      v-else-if="descriptor.input === 'textarea'"
                      :value="(nodeProp<string>(descriptor.key)) ?? ''"
                      type="textarea"
                      :autosize="{ minRows: 3, maxRows: 8 }"
                      class="font-mono"
                      :placeholder="descriptor.placeholder"
                      @update:value="(value: string) => setNodeProp(descriptor.key, value)"
                    />
                    <NInputNumber
                      v-else-if="descriptor.input === 'number'"
                      :value="(nodeProp<number>(descriptor.key)) ?? null"
                      class="w-full"
                      @update:value="(value: number | null) => setNodeProp(descriptor.key, value)"
                    />
                    <NSwitch
                      v-else-if="descriptor.input === 'boolean'"
                      :value="(nodeProp<boolean>(descriptor.key)) ?? false"
                      @update:value="(value: boolean) => setNodeProp(descriptor.key, value)"
                    />
                    <NSelect
                      v-else-if="descriptor.input === 'select'"
                      :value="(nodeProp<string>(descriptor.key)) ?? null"
                      :options="descriptor.options"
                      clearable
                      @update:value="(value: string | null) => setNodeProp(descriptor.key, value)"
                    />
                    <NDynamicTags
                      v-else-if="descriptor.input === 'tags'"
                      :value="(nodeProp<string[]>(descriptor.key)) ?? []"
                      @update:value="(value: string[]) => setNodeProp(descriptor.key, value.length > 0 ? value : null)"
                    />
                    <div v-else-if="descriptor.input === 'json'" class="w-full">
                      <NInput
                        v-model:value="jsonBuffers[descriptor.key]"
                        type="textarea"
                        :autosize="{ minRows: 3, maxRows: 8 }"
                        class="font-mono"
                        :status="jsonErrors[descriptor.key] ? 'error' : undefined"
                        :placeholder="t('workflow.designer.json_prop_placeholder')"
                        @blur="applyJsonBuffer(descriptor.key)"
                      />
                      <div v-if="jsonErrors[descriptor.key]" class="mt-1 text-xs text-red-500">
                        {{ t('workflow.designer.err_prop_json') }}
                      </div>
                    </div>
                  </NFormItem>
                </template>

                <NDivider style="margin: 8px 0" />

                <!-- 通用执行策略 -->
                <NFormItem :label="t('workflow.designer.node_timeout')">
                  <NInputNumber
                    :value="nodeForm.timeoutSeconds ?? null"
                    class="w-full"
                    :placeholder="t('workflow.designer.node_timeout_placeholder')"
                    @update:value="(value: number | null) => { nodeForm!.timeoutSeconds = value; commitNode() }"
                  />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.node_continue_on_error')">
                  <NSwitch
                    :value="nodeForm.continueOnError ?? false"
                    @update:value="(value: boolean) => { nodeForm!.continueOnError = value; commitNode() }"
                  />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.node_retry')">
                  <NSwitch v-model:value="retryEnabled" />
                </NFormItem>
                <template v-if="nodeForm.retryPolicy">
                  <NFormItem :label="t('workflow.designer.retry_max_attempts')">
                    <NInputNumber
                      :value="nodeForm.retryPolicy.maxAttempts"
                      :min="1"
                      class="w-full"
                      @update:value="(value: number | null) => { nodeForm!.retryPolicy!.maxAttempts = value ?? 1; commitNode() }"
                    />
                  </NFormItem>
                  <NFormItem :label="t('workflow.designer.retry_first_delay')">
                    <NInputNumber
                      :value="nodeForm.retryPolicy.firstDelaySeconds"
                      :min="1"
                      class="w-full"
                      @update:value="(value: number | null) => { nodeForm!.retryPolicy!.firstDelaySeconds = value ?? 10; commitNode() }"
                    />
                  </NFormItem>
                  <NFormItem :label="t('workflow.designer.retry_backoff')">
                    <NInputNumber
                      :value="nodeForm.retryPolicy.backoffFactor"
                      :min="1"
                      :step="0.5"
                      class="w-full"
                      @update:value="(value: number | null) => { nodeForm!.retryPolicy!.backoffFactor = value ?? 2; commitNode() }"
                    />
                  </NFormItem>
                </template>
              </NForm>
              <NButton size="small" type="error" secondary block @click="deleteSelectedNode">
                {{ t('workflow.designer.btn_delete_node') }}
              </NButton>
            </template>

            <!-- 流程设置（未选中时） -->
            <template v-else>
              <div class="mb-2 text-sm font-medium">
                {{ t('workflow.designer.flow_panel') }}
              </div>
              <NForm label-placement="top" size="small">
                <NFormItem :label="t('workflow.designer.flow_code')">
                  <NInput v-model:value="meta.code" :placeholder="t('workflow.designer.flow_code_placeholder')" />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.flow_name')">
                  <NInput v-model:value="meta.name" />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.flow_category')">
                  <NInput v-model:value="meta.category" />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.flow_description')">
                  <NInput v-model:value="meta.description" type="textarea" :autosize="{ minRows: 2, maxRows: 4 }" />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.flow_compensation')">
                  <NSwitch v-model:value="meta.enableCompensation" />
                </NFormItem>
                <NFormItem :label="t('workflow.designer.flow_variables')">
                  <NDynamicInput
                    v-model:value="meta.variables"
                    :on-create="() => ({ name: '', required: false, defaultValue: null, description: null })"
                  >
                    <template #default="{ value }">
                      <div class="flex w-full items-center gap-2">
                        <NInput v-model:value="value.name" size="small" :placeholder="t('workflow.designer.variable_name')" />
                        <NCheckbox v-model:checked="value.required" size="small">
                          {{ t('workflow.designer.variable_required') }}
                        </NCheckbox>
                      </div>
                    </template>
                  </NDynamicInput>
                </NFormItem>
              </NForm>
              <div class="text-xs text-gray-400">
                {{ t('workflow.designer.flow_tip') }}
              </div>
            </template>
          </div>
        </div>
      </NTabPane>

      <!-- JSON 视图 -->
      <NTabPane name="json" :tab="t('workflow.designer.tab_json')">
        <div class="flex h-[68vh] flex-col gap-2">
          <NInput
            v-model:value="jsonText"
            type="textarea"
            class="min-h-0 flex-1 font-mono"
            :autosize="{ minRows: 24, maxRows: 24 }"
            :placeholder="t('workflow.designer.json_placeholder')"
          />
          <NButton size="small" @click="applyJsonToCanvas">
            {{ t('workflow.designer.btn_apply_json') }}
          </NButton>
        </div>
      </NTabPane>
    </NTabs>
  </div>
</template>

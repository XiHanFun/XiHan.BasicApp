<script setup lang="ts" generic="T extends GrantTransferItem">
import type { GrantTransferGroup, GrantTransferItem, GrantTransferSide } from './grant-transfer'
import {
  XhTransferGroup,
  XhTransferGroupLabel,
  XhTransferItem,
  XhTransferItemCheckbox,
  XhTransferItemDescription,
  XhTransferItemSuffix,
  XhTransferItemText,
  XhTransferList,
  XhTransferPanelCount,
  XhTransferPanelHeader,
  XhTransferPanelTitle,
  XhTransferRoot,
  XhTransferSearch,
  XhTransferSourcePanel,
  XhTransferTargetPanel,
  XhTransferToSourceTrigger,
  XhTransferToTargetTrigger,
} from '@xihan-ui/vue'
import { computed } from 'vue'

/**
 * 授权穿梭框：二元授予（有 / 无）的授权分配用它，左边可授、右边已授。
 *
 * 比单栏勾选列表强的地方在右边那一栏——「已授予」是审计时要成列读的一份清单，
 * 勾选列表里得自己把勾上的挑出来。
 *
 * 只表达「在左还是在右」一个维度。三态（允许 / 拒绝 / 未设置）拆成两个穿梭框各管一态，
 * 不要往行尾塞可点的控件：条目本身是 option、整行点击即勾选，行尾一格只放徽标与计数。
 */
defineOptions({ name: 'XGrantTransfer' })

const props = withDefaults(defineProps<{
  /** 可授予的全集 */
  items: T[]
  /** 已授予的主键；两侧的归属由它决定 */
  value: T['basicId'][]
  /** 分段（顺序由全集决定、两侧共用）；不分段时传一个无名段 */
  groups: GrantTransferGroup<T>[]
  /** 条目主文本 */
  getLabel: (item: T) => string
  /** 条目第 2 行（编码等）；同时并入检索取字 */
  getDescription?: (item: T) => string | null | undefined
  loading?: boolean
  disabled?: boolean
  sourceTitle: string
  targetTitle: string
  searchPlaceholder: string
}>(), {
  getDescription: undefined,
  loading: false,
  disabled: false,
})

const emit = defineEmits<{ 'update:value': [value: T['basicId'][]] }>()

defineSlots<{
  /** 行尾一格：只放徽标、计数这类非交互内容 */
  suffix?: (props: { item: T, side: GrantTransferSide }) => unknown
}>()

/**
 * 穿梭框的值域是字符串，条目主键未必是串（T['basicId']）。
 * 这里按字符串出入、回写时查回条目原本的主键，不把主键的类型悄悄换掉。
 */
const itemByKey = computed(() => {
  const map = new Map<string, T>()
  for (const item of props.items) {
    map.set(String(item.basicId), item)
  }
  return map
})

/** label 同时是搜索取字来源：主文本与第 2 行都拼进去，按名或按码都搜得到 */
const collection = computed(() =>
  props.items.map(item => ({
    value: String(item.basicId),
    label: [props.getLabel(item), props.getDescription?.(item)].filter(Boolean).join(' '),
  })),
)

const selectedKeys = computed({
  get: () => props.value.map(String),
  set: (next: string[]) => emit('update:value', next.flatMap((key) => {
    const item = itemByKey.value.get(key)
    return item ? [item.basicId] : []
  })),
})

const keysByGroup = computed(() => {
  const map = new Map<string, Set<string>>()
  for (const group of props.groups) {
    map.set(group.key, new Set(group.items.map(item => String(item.basicId))))
  }
  return map
})

/** 面板插槽只给出本侧当前可见的条目，分段信息回全集里查 */
function inGroup(visible: readonly { value: string }[], groupKey: string): string[] {
  const keys = keysByGroup.value.get(groupKey)
  if (!keys) {
    return []
  }
  return visible.map(item => item.value).filter(key => keys.has(key))
}

const sides: { side: GrantTransferSide, panel: typeof XhTransferSourcePanel }[] = [
  { side: 'source', panel: XhTransferSourcePanel },
  { side: 'target', panel: XhTransferTargetPanel },
]
</script>

<template>
  <XhTransferRoot
    v-model:value="selectedKeys"
    class="x-grant-transfer"
    :collection="collection"
    :loading="loading"
    :disabled="disabled"
    searchable
  >
    <template v-for="{ side, panel } in sides" :key="side">
      <component :is="panel" v-slot="{ items: visible }">
        <XhTransferPanelHeader>
          <XhTransferPanelTitle>{{ side === 'source' ? sourceTitle : targetTitle }}</XhTransferPanelTitle>
          <XhTransferPanelCount />
        </XhTransferPanelHeader>
        <XhTransferSearch :placeholder="searchPlaceholder" />
        <XhTransferList>
          <template v-for="group in groups" :key="group.key">
            <XhTransferGroup v-if="inGroup(visible, group.key).length > 0" :value="group.key">
              <XhTransferGroupLabel v-if="group.name">
                {{ group.name }}
              </XhTransferGroupLabel>
              <XhTransferItem v-for="key in inGroup(visible, group.key)" :key="key" :value="key">
                <XhTransferItemCheckbox />
                <XhTransferItemText>{{ getLabel(itemByKey.get(key)!) }}</XhTransferItemText>
                <XhTransferItemDescription v-if="getDescription?.(itemByKey.get(key)!)">
                  {{ getDescription(itemByKey.get(key)!) }}
                </XhTransferItemDescription>
                <XhTransferItemSuffix v-if="$slots.suffix">
                  <slot name="suffix" :item="itemByKey.get(key)!" :side="side" />
                </XhTransferItemSuffix>
              </XhTransferItem>
            </XhTransferGroup>
          </template>
        </XhTransferList>
      </component>
      <template v-if="side === 'source'">
        <XhTransferToTargetTrigger />
        <XhTransferToSourceTrigger />
      </template>
    </template>
  </XhTransferRoot>
</template>

<style scoped>
/*
 * 穿梭框自身的高度由内容撑出（缺省约 256px）。放进抽屉这种 flex 列里时让它吃满剩余高度，
 * 否则两栏只剩五六行、底下一大片空白，「已授予」那栏也就失去了成列审阅的意义。
 * 不在 flex 容器里时这条不起作用，仍按内容高。
 */
.x-grant-transfer {
  flex: 1;
  min-block-size: 0;
}
</style>

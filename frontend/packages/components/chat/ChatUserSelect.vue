<script setup lang="ts">
import type { SelectOption } from 'naive-ui'
import type { ChatUserPickerItem } from '~/types'
import { NSelect } from 'naive-ui'
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAppContext } from '~/stores'

defineOptions({ name: 'ChatUserSelect' })

const props = withDefaults(defineProps<{
  /** 多选（建群/加成员）；false 为单选（发起单聊） */
  multiple?: boolean
  /** 需要从候选中排除的用户ID（已在会话中的成员/自己） */
  excludeUserIds?: string[]
  placeholder?: string
}>(), {
  multiple: false,
  excludeUserIds: () => [],
  placeholder: undefined,
})

const model = defineModel<null | string | string[]>({ default: null })

const { t } = useI18n()
const appContext = useAppContext()

const loading = ref(false)
const options = ref<SelectOption[]>([])
let searchSeq = 0

const excluded = computed(() => new Set(props.excludeUserIds))

async function handleSearch(keyword: string) {
  const seq = ++searchSeq
  loading.value = true
  try {
    const items = await appContext.apis.chatApi.selectUsers(keyword, 20)
    // 仅采纳最后一次检索结果，避免慢响应覆盖新输入
    if (seq !== searchSeq) {
      return
    }
    options.value = items
      .filter(item => !excluded.value.has(item.userId))
      .map((item: ChatUserPickerItem) => ({
        label: item.nickName ? `${item.nickName}（${item.userName}）` : item.userName,
        value: item.userId,
      }))
  }
  catch {
    if (seq === searchSeq) {
      options.value = []
    }
  }
  finally {
    if (seq === searchSeq) {
      loading.value = false
    }
  }
}
</script>

<template>
  <NSelect
    v-model:value="model"
    :multiple="props.multiple"
    filterable
    remote
    clearable
    :loading="loading"
    :options="options"
    :placeholder="props.placeholder ?? t('chat.start.user_placeholder')"
    @search="handleSearch"
    @focus="() => handleSearch('')"
  />
</template>

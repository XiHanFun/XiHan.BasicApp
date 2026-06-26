<script setup lang="ts">
import { NButton, NCheckbox, NInput } from 'naive-ui'
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import WidgetCard from './WidgetCard.vue'

defineOptions({ name: 'TodoWidget' })

interface Todo {
  id: number
  text: string
  done: boolean
}

const STORAGE_KEY = 'xh:workbench:todos'
const { t } = useI18n()
const todos = ref<Todo[]>([])
const draft = ref('')

onMounted(() => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    todos.value = raw ? (JSON.parse(raw) as Todo[]) : []
  }
  catch {
    todos.value = []
  }
})

watch(todos, value => localStorage.setItem(STORAGE_KEY, JSON.stringify(value)), { deep: true })

const hasDone = computed(() => todos.value.some(item => item.done))

function add() {
  const text = draft.value.trim()
  if (!text)
    return
  todos.value.unshift({ id: Date.now(), text, done: false })
  draft.value = ''
}
function remove(id: number) {
  todos.value = todos.value.filter(item => item.id !== id)
}
function clearDone() {
  todos.value = todos.value.filter(item => !item.done)
}
</script>

<template>
  <WidgetCard icon="lucide:check-square" :title="t('workbench.widgets.todo.title')">
    <template #extra>
      <NButton v-if="hasDone" size="tiny" quaternary @click="clearDone">
        {{ t('workbench.widgets.todo_clear_done') }}
      </NButton>
    </template>
    <div class="flex h-full flex-col gap-2">
      <NInput v-model:value="draft" size="small" clearable :placeholder="t('workbench.widgets.todo_placeholder')" @keyup.enter="add" />
      <div v-if="!todos.length" class="flex flex-1 items-center justify-center text-sm text-muted-foreground">
        {{ t('workbench.widgets.todo_empty') }}
      </div>
      <div v-else class="flex min-h-0 flex-1 flex-col gap-0.5 overflow-auto">
        <div
          v-for="item in todos"
          :key="item.id"
          class="group flex items-center gap-2 rounded px-1 py-1 transition-colors hover:bg-muted"
        >
          <NCheckbox v-model:checked="item.done" />
          <span class="min-w-0 flex-1 truncate text-sm" :class="item.done ? 'text-muted-foreground line-through' : 'text-foreground'">{{ item.text }}</span>
          <button type="button" class="shrink-0 opacity-0 transition-opacity group-hover:opacity-100" @click="remove(item.id)">
            <Icon icon="lucide:x" width="14" class="text-muted-foreground hover:text-[hsl(var(--destructive))]" />
          </button>
        </div>
      </div>
    </div>
  </WidgetCard>
</template>

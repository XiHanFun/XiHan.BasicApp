import type { XhTranslationOverrides } from '@xihan-ui/vue'

/**
 * XiHan.UI 组件内建文案的中文覆盖。
 *
 * 这些文案大多只给读屏器（aria-label），少数会出现在界面上（级联/下拉的空态）。
 * 组件内建的是英文，所以 en-US 一份留空即可——不覆盖就是内建值。
 * 取值优先级：组件实例上的 translations > 这里 > 组件内建。
 */
const zhCN: XhTranslationOverrides = {
  'alert': { close: '关闭' },
  'anchor': { root: '页内导航' },
  'back-top': { trigger: '回到顶部' },
  'breadcrumb': { root: '面包屑导航' },
  'carousel': {
    root: '轮播',
    prevTrigger: '上一张',
    nextTrigger: '下一张',
    indicatorGroup: '轮播指示器',
    indicator: page => `第 ${page} 张`,
    item: (index, count) => `第 ${index} 张，共 ${count} 张`,
  },
  'cascader': { empty: '暂无数据', noMatch: '无匹配项', column: '选项列', clearTrigger: '清空' },
  'color-picker': {
    area: '色彩区域',
    areaValueText: (saturation, brightness) => `饱和度 ${saturation}%，明度 ${brightness}%`,
    channel: channel => `${channel} 通道`,
    channelValueText: (channel, value) => `${channel} ${value}`,
    input: channel => `${channel} 输入`,
    swatch: value => `色卡 ${value}`,
    swatchGroup: '预设色卡',
    eyeDropperTrigger: '取色器',
  },
  'combobox': { clearTrigger: '清空' },
  'composer': { send: '发送', stop: '停止', input: '输入消息' },
  'context-menu': { content: '右键菜单' },
  'date-picker': { startDate: '开始日期', endDate: '结束日期', presets: '快捷选项', clearTrigger: '清空' },
  'dialog': { close: '关闭' },
  'drawer': { close: '关闭' },
  'dynamic-input': {
    deleteItem: (index, count) => `删除第 ${index} 项，共 ${count} 项`,
    moveUpTrigger: (index, count) => `上移第 ${index} 项，共 ${count} 项`,
    moveDownTrigger: (index, count) => `下移第 ${index} 项，共 ${count} 项`,
  },
  'file-upload': {
    dropzone: '拖拽文件到此处，或点击选择',
    deleteItem: file => `移除 ${file.name}`,
    clearTrigger: '清空已选文件',
  },
  'float-button': { trigger: '悬浮操作' },
  'image-viewer': {
    content: '图片预览',
    close: '关闭',
    zoomIn: '放大',
    zoomOut: '缩小',
    rotateLeft: '向左旋转',
    rotateRight: '向右旋转',
    flipHorizontal: '水平翻转',
    flipVertical: '垂直翻转',
    reset: '重置',
    prev: '上一张',
    next: '下一张',
    counter: (index, count) => `第 ${index} / ${count} 张`,
  },
  'loading-bar': { root: '加载进度' },
  'log': { log: '日志' },
  'mention': { content: '提及候选', input: '输入以提及' },
  'navigation-menu': { root: '主导航' },
  'pagination': {
    root: '分页',
    prevTrigger: '上一页',
    nextTrigger: '下一页',
    item: page => `第 ${page} 页`,
  },
  'pin-input': { input: (index, length) => `第 ${index} 位，共 ${length} 位` },
  'popover': { close: '关闭' },
  'select': { clearTrigger: '清空', deleteItem: label => `移除 ${label}`, content: '选项列表' },
  'side-nav': { root: '侧边导航' },
  'spinner': { label: '加载中' },
  'tags-input': {
    deleteItem: value => `删除 ${value}`,
    editTagInput: value => `编辑 ${value}`,
    clearTrigger: '清空全部',
  },
  'text-field': { clearTrigger: '清空' },
  'thread': { scrollToBottom: '滚到底部', log: '消息列表' },
  'toast': { close: '关闭' },
  'toaster': { region: '通知' },
  'tour': { close: '结束引导', progress: (step, count) => `第 ${step} 步，共 ${count} 步` },
  'tree-select': { tree: '树形选项', clearTrigger: '清空' },
}

/** en-US 不覆盖：组件内建文案本身就是英文。 */
const enUS: XhTranslationOverrides = {}

export const xhTranslations: Record<string, XhTranslationOverrides> = {
  'zh-CN': zhCN,
  'en-US': enUS,
}

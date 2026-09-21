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
    autoplayTriggerPlay: '开始自动播放',
    autoplayTriggerPause: '停止自动播放',
    indicatorGroup: '轮播指示器',
    indicator: page => `第 ${page} 张`,
    item: (index, count) => `第 ${index} 张，共 ${count} 张`,
  },
  'cascader': {
    empty: '暂无数据',
    noMatch: '无匹配项',
    loading: '加载中',
    column: '选项列',
    searchInput: '搜索',
    searchList: '搜索结果',
    clearTrigger: '清空',
  },
  // copy 是复制钮的兜底名字：本站两处触发器都自带 aria-label，这句只给漏写的那一处
  'clipboard': { copy: '复制', copied: '已复制' },
  'code-view': { code: '代码', expand: '展开代码', collapse: '收起代码' },
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
  'combobox': { trigger: '显示候选', clearTrigger: '清空' },
  'context-menu': { content: '右键菜单' },
  'date-picker': {
    presets: '快捷选项',
    clearTrigger: '清空',
    hour: '时',
    minute: '分',
    second: '秒',
    todayDate: date => `今天，${date}`,
  },
  'date-range-picker': {
    startDate: '开始日期',
    endDate: '结束日期',
    presets: '快捷选项',
    clearTrigger: '清空',
    startRangeSelectionPrompt: '点击开始选择日期范围',
    finishRangeSelectionPrompt: '点击结束选择日期范围',
    selectedRange: (start, end) => `已选范围：${start} 至 ${end}`,
    todayDate: date => `今天，${date}`,
  },
  'dialog': { close: '关闭' },
  'drawer': { close: '关闭' },
  'field-array': {
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
  'heatmap': {
    gridLabel: '活动热力图',
    // 日期形态每格是当天计数；矩阵形态每格是作者给的值，不替它加量词
    cellLabel: details => `${details.date}：${details.count} 次`,
    matrixCellLabel: details => `${details.row} ${details.column}：${details.count}`,
    legendLabel: '活动量',
    legendLow: '少',
    legendHigh: '多',
  },
  // 只覆盖整组的读法；每一枚键的名字随平台变（Mac 念 Command/Option），交回组件库
  'kbd': { hotkey: names => `快捷键 ${names.join(' 加 ')}` },
  'image-viewer': {
    content: '图片预览',
    toolbar: '图片工具栏',
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
  'json-viewer': {
    tree: 'JSON 视图',
    text: 'JSON 原文',
    root: '根',
    objectPreview: count => `{…} ${count} 项`,
    arrayPreview: count => `[…] ${count} 项`,
    collapsedBranchLabel: (name, count) => `${name}，${count} 项`,
    moreItems: count => `… 其余 ${count} 项`,
    empty: '暂无数据',
  },
  'loading-bar': { root: '加载进度' },
  'log': { log: '日志' },
  'message-feed': {
    feed: '消息列表',
    scrollToBottom: '滚到底部',
    item: (position, size) => (size < 0 ? `第 ${position} 条消息` : `第 ${position} 条消息，共 ${size} 条`),
  },
  // menu 的 content 刻意不写：它缺省为空，浮层改由 aria-labelledby 指向触发器取名；这里一写就把触发器的名字盖掉
  'mention': { content: '提及候选', input: '输入以提及' },
  'navigation-menu': { root: '主导航' },
  'notification': { region: '通知' },
  'pagination': {
    root: '分页',
    prevTrigger: '上一页',
    nextTrigger: '下一页',
    item: page => `第 ${page} 页`,
    ellipsis: count => `还有 ${count} 页`,
    pageSizeSelect: '每页条数',
    pageSizeOption: size => `${size} 条/页`,
    // 无数据时 start 与 end 都是 0，「第 0-0 条」念出来不成句
    summary: (start, end, count) => (count === 0 ? '共 0 条' : `第 ${start}-${end} 条，共 ${count} 条`),
    jumper: '跳转到页',
  },
  'password-input': {
    visibilityTriggerShow: '显示密码',
    visibilityTriggerHide: '隐藏密码',
    capsLockOn: '大写锁定已开启',
    strengthMeter: '密码强度',
  },
  'pin-input': { input: (index, length) => `第 ${index} 位，共 ${length} 位` },
  'popover': { close: '关闭' },
  'prompt-input': { send: '发送', stop: '停止', input: '输入消息' },
  // overflowTag 不写：折叠标签显示的 +N 与语言无关
  'select': { clearTrigger: '清空', deleteItem: label => `移除 ${label}`, content: '选项列表' },
  'side-nav': { root: '侧边导航' },
  'sortable': {
    root: '可排序列表',
    // item 不覆盖：状态机把项上写着的字装进这一条，这里写了会把文字盖回 id
    itemDragTrigger: name => `拖动 ${name} 换位`,
    picked: (name, position, total) => `已拾起 ${name}，第 ${position} 位，共 ${total} 位。方向键移动，空格放下，Esc 取消`,
    moved: (name, position, total) => `已将 ${name} 移到第 ${position} 位，共 ${total} 位`,
    dropped: (name, position) => `${name} 已放到第 ${position} 位`,
    canceled: (name, position) => `已取消排序，${name} 回到第 ${position} 位`,
  },
  'spinner': { label: '加载中' },
  'splitter': {
    root: '分栏面板',
    // index 从 0 起，total 是分隔条总数；只有一条时不必报序号
    resizeTrigger: (index, total) => (total > 1 ? `调整第 ${index + 1} 块面板大小，共 ${total} 条分隔条` : '调整面板大小'),
  },
  'table': {
    // 排序钮、列宽把手、列拖拽把手都是列头里独立的图标钮、不包列名，这句是它们对读屏唯一的自述
    sort: label => `按 ${label} 排序`,
    columnResize: label => `调整 ${label} 列宽`,
    columnDrag: label => `拖动 ${label} 列换位`,
    // 全选把手默认是空的角色节点，行内的勾选框又对读屏隐藏，这是整张表选择功能唯一的入口
    selectAll: '全选所有行',
    toolbar: '表格工具栏',
    columnList: '列设置',
    columnVisibility: label => `显示 ${label} 列`,
    // 列 / 行拖动换位的读屏播报。item 不覆盖：组件库拿它把列 id 换成列名，这里写了会把列名盖回 id；
    // movedInto / droppedInto / canceledInto / rootLevel 是带容器（树）的那三句，表格不报容器，不写
    moved: (name, position, total) => `已将 ${name} 移到第 ${position} 位，共 ${total} 位`,
    dropped: (name, position) => `${name} 已放到第 ${position} 位`,
    canceled: (name, position) => `已取消移动，${name} 回到第 ${position} 位`,
    rejected: name => `${name} 不能放在这里`,
  },
  // 标签换位的读屏播报。item 不覆盖：组件库拿它把 value 换成标签文字，这里写了会盖回 value；
  // 标签是一维重排、机器不传容器，movedInto / droppedInto / canceledInto / rootLevel 永远不触发，不写
  'tabs': {
    moved: (name, position, total) => `已将 ${name} 移到第 ${position} 位，共 ${total} 位`,
    dropped: (name, position) => `${name} 已放到第 ${position} 位`,
    canceled: (name, position) => `已取消移动，${name} 回到第 ${position} 位`,
    rejected: name => `${name} 不能放在这里`,
  },
  // 关闭钮缺省是 Delete，与 select / tags-input 里同一动作同一个词
  'tag': { close: '删除' },
  'tags-input': {
    deleteItem: value => `删除 ${value}`,
    editTagInput: value => `编辑 ${value}`,
    clearTrigger: '清空全部',
  },
  'text-field': { clearTrigger: '清空' },
  'timer': {
    // 与内建同一口径：时分秒常报，天只在大于 0 时带上
    time: ({ days, hours, minutes, seconds }) =>
      `${days > 0 ? `${days} 天 ` : ''}${hours} 小时 ${minutes} 分 ${seconds} 秒`,
    start: '开始',
    pause: '暂停',
    resume: '继续',
    reset: '重置',
  },
  'toast': { close: '关闭' },
  'tour': { close: '结束引导', progress: (step, count) => `第 ${step} 步，共 ${count} 步` },
  // 节点换位的读屏播报。item 不覆盖：组件库拿它把节点 id 换成节点名，这里写了会盖回 id；
  // 树的机器每次都传容器（根层传 null），走的是带容器的三句，扁平的 moved / dropped / canceled 永远不触发，不写
  'tree': {
    movedInto: (name, into, position, total) => `已将 ${name} 移入 ${into}，第 ${position} 位，共 ${total} 位`,
    droppedInto: (name, into, position) => `${name} 已放入 ${into} 第 ${position} 位`,
    canceledInto: (name, into, position) => `已取消移动，${name} 回到 ${into} 第 ${position} 位`,
    rootLevel: '顶层',
    rejected: name => `${name} 不能放在这里`,
  },
  'tree-select': {
    tree: '树形选项',
    clearTrigger: '清空',
    empty: '暂无数据',
    loading: '加载中',
    branchError: '子节点加载失败',
    retry: '重试',
    branchEmpty: '没有子节点',
  },
}

/** en-US 不覆盖：组件内建文案本身就是英文。 */
const enUS: XhTranslationOverrides = {}

export const xhTranslations: Record<string, XhTranslationOverrides> = {
  'zh-CN': zhCN,
  'en-US': enUS,
  // 德文不另写覆盖：组件内建英文，故与 en-US 用同一份空覆盖；
  // 未登记的语言由 xhTranslationsOfCurrentLocale 按主语言回退（zh 系 zh-CN，其余 en-US）
  'de-DE': enUS,
}

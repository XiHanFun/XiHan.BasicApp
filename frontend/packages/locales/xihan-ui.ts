import type { XhTranslationOverrides } from '@xihan-ui/vue'

/** 浮动面板改尺把手的方位（n / e / s / w 与四个角），从文案函数的入参取，免得为一个类型再依赖 headless */
type FloatingPanelEdge = Parameters<NonNullable<NonNullable<XhTranslationOverrides['floating-panel']>['resizeTrigger']>>[0]

/** 浮动面板八个改尺把手的方位说法：读屏里八个把手完全相同，只能靠它区分 */
const FLOATING_PANEL_EDGES: Record<FloatingPanelEdge, string> = {
  n: '上边',
  e: '右边',
  s: '下边',
  w: '左边',
  ne: '右上角',
  nw: '左上角',
  se: '右下角',
  sw: '左下角',
}

/**
 * 各图表共有的文案：空态、加载与提示框里的「其他」上屏，其余给读屏（角色说明、图例、数据表、数据标记的可及名）
 */
const CHART_ZH = {
  chartRoleDescription: '图表',
  seriesRoleDescription: '系列',
  legendLabel: '图例',
  missingValue: '无值',
  emptyText: '暂无数据',
  loadingText: '加载中…',
  otherLabel: '其他',
  tableCaption: '数据表',
  datumLabel: details => `${details.formatted.key ?? String(details.key)}，${details.seriesName} ${details.formatted.value ?? ''}`,
} satisfies NonNullable<XhTranslationOverrides['cartesian-chart']>

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
  'cartesian-chart': {
    ...CHART_ZH,
    keyLabel: '类目',
    seriesLabel: '系列',
    valueLabel: '数值',
    sizeLabel: '大小',
    colorLabel: '颜色',
    zoomLabel: '缩放',
    zoomStartLabel: '窗口起点',
    zoomEndLabel: '窗口终点',
    referenceLabel: '参考',
    averageLabel: '平均',
    ohlcLabel: ({ open, high, low, close }) => `开 ${open}，高 ${high}，低 ${low}，收 ${close}`,
    ohlcColumns: { open: '开盘', high: '最高', low: '最低', close: '收盘' },
    boxLabel: ({ min, q1, median, q3, max }) => `最小 ${min}，下四分位 ${q1}，中位数 ${median}，上四分位 ${q3}，最大 ${max}`,
    boxColumns: { min: '最小', q1: '下四分位', median: '中位数', q3: '上四分位', max: '最大', outliers: '离群点' },
    aggregatedCaption: ({ caption, rows, ranges }) => `${caption}（${rows} 行合并为 ${ranges} 个区间）`,
    annotationSummary: items => items.map(item => `${item.label}${item.series ? `（${item.series}）` : ''}：${item.value}。`).join(''),
    summary: (model) => {
      if (!model.range || model.series.every(series => series.count === 0)) {
        return '没有数据。'
      }
      const { first, last, count } = model.range
      const extremes = model.series.flatMap(({ name, min, max }) => {
        if (!min || !max) {
          return []
        }
        return min.key === max.key && min.value === max.value
          ? [`${name}：${max.key} 为 ${max.value}。`]
          : [`${name}：最低 ${min.value}（${min.key}），最高 ${max.value}（${max.key}）。`]
      })
      return [`${model.seriesCount} 个系列，${count} 个数据点，从 ${first} 到 ${last}。`, ...extremes].join('')
    },
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
    deleteItem: label => `移除 ${label}`,
  },
  // 多选标签的删除钮与 select 同一说法（取色器、日期选择的多选标签同此）；overflowTag 不写：+N 与语言无关
  'combobox': { trigger: '显示候选', clearTrigger: '清空', deleteItem: label => `移除 ${label}` },
  'context-menu': { content: '右键菜单' },
  'date-picker': {
    presets: '快捷选项',
    clearTrigger: '清空',
    hour: '时',
    minute: '分',
    second: '秒',
    todayDate: date => `今天，${date}`,
    deleteItem: label => `移除 ${label}`,
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
  'floating-panel': {
    dragTrigger: '移动面板',
    resizeTrigger: edge => `调整${FLOATING_PANEL_EDGES[edge]}`,
    resizeValueText: ({ width, height }) => `宽 ${Math.round(width)}，高 ${Math.round(height)}`,
    windowStateTrigger: state => ({ default: '还原面板', minimized: '收拢面板', maximized: '铺满面板' })[state],
    close: '关闭',
  },
  'funnel-chart': {
    ...CHART_ZH,
    nameLabel: '阶段',
    valueLabel: '数值',
    previousLabel: '较上一阶段',
    firstLabel: '较第一阶段',
    summary: (model) => {
      if (!model.first) {
        return '没有数据。'
      }
      const head = `${model.stageCount} 个阶段，从「${model.first.name}」${model.first.value} 到「${model.last?.name ?? model.first.name}」${model.last?.value ?? model.first.value}`
      const overall = model.overall ? `，整体转化 ${model.overall}` : ''
      const steepest = model.steepest ? `。流失最多的一步：${model.steepest.from} 到 ${model.steepest.to}，转化 ${model.steepest.rate}` : ''
      return `${head}${overall}${steepest}。`
    },
  },
  'graph-chart': {
    ...CHART_ZH,
    sourceLabel: '起点',
    targetLabel: '终点',
    valueLabel: '数值',
    linkLabel: '关系',
    linksLabel: '连线',
    incomingLabel: '指向它的',
    outgoingLabel: '它指向的',
    summary: (model) => {
      const head = `${model.nodeCount} 个节点，${model.linkCount} 条连线。`
      return model.hub ? `${head}连线最多的是「${model.hub.name}」，${model.hub.degree} 条。` : head
    },
  },
  'heatmap': {
    gridLabel: '活动热力图',
    // 日期形态每格是当天计数；矩阵形态每格是作者给的值，不替它加量词
    cellLabel: details => `${details.date}：${details.count} 次`,
    matrixCellLabel: details => `${details.row} ${details.column}：${details.count}`,
    legendLabel: '活动量',
    legendLow: '少',
    legendHigh: '多',
  },
  'hierarchy-chart': {
    ...CHART_ZH,
    rootLabel: '全部',
    pathLabel: '下钻路径',
    nameLabel: '名称',
    valueLabel: '数值',
    levelLabel: level => `第 ${level} 层`,
    parentShareLabel: '占上一层',
    rootShareLabel: '占全部',
    summary: (model) => {
      const head = `「${model.root}」合计 ${model.total}，下一层 ${model.childCount} 项。`
      return model.largest ? `${head}最大的是「${model.largest.name}」，${model.largest.value}，占 ${model.largest.share}。` : head
    },
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
  // 通知卡片与轻提示是同一组件的两种预设，堆叠区读屏名与关闭钮共用这一桶
  'notification': { region: '通知', close: '关闭' },
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
  'pie-chart': {
    ...CHART_ZH,
    centerLabel: '合计',
    nameLabel: '类别',
    valueLabel: '数值',
    shareLabel: '占比',
    summary: (model) => {
      const top = model.slices[0]
      if (!top) {
        return '没有数据。'
      }
      return `${model.sliceCount} 项，合计 ${model.total}。最大的是「${top.name}」，${top.value}，占 ${top.share}。`
    },
  },
  'pin-input': { input: (index, length) => `第 ${index} 位，共 ${length} 位` },
  'popover': { close: '关闭' },
  // 分段量（仪表盘）读屏：数值后补上所在分段的名字
  'progress': { segmentValueText: ({ value, label }) => `${value}，${label}` },
  'prompt-input': { send: '发送', stop: '停止', input: '输入消息' },
  'radar-chart': {
    ...CHART_ZH,
    nameLabel: '指标',
    summary: (model) => {
      const lines = model.series
        .filter(item => item.highest && item.lowest)
        .map(item => `「${item.name}」最高是${item.highest!.indicator} ${item.highest!.value}，最低是${item.lowest!.indicator} ${item.lowest!.value}`)
      const head = `${model.seriesCount} 个系列，${model.indicatorCount} 项指标。`
      return lines.length > 0 ? `${head}${lines.join('；')}。` : head
    },
  },
  'sankey-chart': {
    ...CHART_ZH,
    datumLabel: details => `${details.seriesName}，${details.formatted.value ?? ''}`,
    sourceLabel: '来源',
    targetLabel: '去向',
    valueLabel: '流量',
    inflowLabel: '来自',
    outflowLabel: '流向',
    summary: (model) => {
      if (model.linkCount === 0) {
        return '没有数据。'
      }
      const head = `${model.nodeCount} 个节点，${model.linkCount} 条流带，合计 ${model.total}。`
      return model.largest
        ? `${head}最大的一条：${model.largest.source} 到 ${model.largest.target}，${model.largest.value}。`
        : head
    },
  },
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
  'sparkline': {
    summary: (model) => {
      if (model.count === 0) {
        return '没有数据。'
      }
      const range = `${model.count} 个值，从 ${model.first} 到 ${model.last}，最低 ${model.min}，最高 ${model.max}`
      const trend = model.direction === 'up'
        ? `，上升 ${model.change ?? ''}`
        : model.direction === 'down'
          ? `，下降 ${model.change ?? ''}`
          : model.direction === 'flat' ? '，持平' : ''
      return `${range}${trend}。`
    },
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

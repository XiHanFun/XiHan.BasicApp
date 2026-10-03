import type { Component } from 'vue'
import { markRaw } from 'vue'
import AnnouncementWidget from './AnnouncementWidget.vue'
import CategoryMixWidget from './charts/CategoryMixWidget.vue'
import ChannelShareWidget from './charts/ChannelShareWidget.vue'
import ConversionFunnelWidget from './charts/ConversionFunnelWidget.vue'
import CustomerMixWidget from './charts/CustomerMixWidget.vue'
import MaterialPriceWidget from './charts/MaterialPriceWidget.vue'
import OrderFlowWidget from './charts/OrderFlowWidget.vue'
import OrderHeatmapWidget from './charts/OrderHeatmapWidget.vue'
import ProductPerformanceWidget from './charts/ProductPerformanceWidget.vue'
import ProfitWaterfallWidget from './charts/ProfitWaterfallWidget.vue'
import SalesKpiWidget from './charts/SalesKpiWidget.vue'
import SalesTrendWidget from './charts/SalesTrendWidget.vue'
import StoreRadarWidget from './charts/StoreRadarWidget.vue'
import SupplyNetworkWidget from './charts/SupplyNetworkWidget.vue'
import TargetProgressWidget from './charts/TargetProgressWidget.vue'
import ClockWidget from './ClockWidget.vue'
import FavoritesWidget from './FavoritesWidget.vue'
import StatsWidget from './StatsWidget.vue'
import TodoWidget from './TodoWidget.vue'
import WelcomeWidget from './WelcomeWidget.vue'

/** 小组件定义：键、i18n 标题/描述键、图标、默认宽度（12 栅格）、组件、可选权限码 */
export interface WidgetDef {
  key: string
  titleKey: string
  descKey: string
  icon: string
  defaultSpan: number
  component: Component
  /** 可选权限码：声明后仅拥有该权限的用户可见/可添加此小组件（缺省人人可见） */
  permission?: string
}

function widget(key: string, i18nKey: string, icon: string, defaultSpan: number, component: Component): WidgetDef {
  return {
    key,
    titleKey: `workbench.widgets.${i18nKey}.title`,
    descKey: `workbench.widgets.${i18nKey}.desc`,
    icon,
    defaultSpan,
    component: markRaw(component),
  }
}

/**
 * 小组件登记表：新增小组件只需在此追加一项。
 * 今日统计与各图表用的是前端生成的示例数据（charts/demo-data.ts），标题旁标有「示例数据」；
 * 接入真实业务时在后端 WorkbenchQueryService 加接口，再把对应小组件换成接口取数。
 */
export const WIDGETS: WidgetDef[] = [
  widget('welcome', 'welcome', 'lucide:sparkles', 4, WelcomeWidget),
  widget('clock', 'clock', 'lucide:clock', 4, ClockWidget),
  widget('favorites', 'favorites', 'lucide:star', 4, FavoritesWidget),
  widget('todo', 'todo', 'lucide:check-square', 3, TodoWidget),
  widget('announcement', 'announcement', 'lucide:megaphone', 9, AnnouncementWidget),
  widget('stats', 'stats', 'lucide:gauge', 2, StatsWidget),
  widget('sales-kpi', 'sales_kpi', 'lucide:activity', 4, SalesKpiWidget),
  widget('channel-share', 'channel_share', 'lucide:pie-chart', 3, ChannelShareWidget),
  widget('conversion-funnel', 'conversion_funnel', 'lucide:filter', 3, ConversionFunnelWidget),
  widget('sales-trend', 'sales_trend', 'lucide:trending-up', 8, SalesTrendWidget),
  widget('order-flow', 'order_flow', 'lucide:waypoints', 4, OrderFlowWidget),
  widget('product-performance', 'product_performance', 'lucide:package', 8, ProductPerformanceWidget),
  widget('customer-mix', 'customer_mix', 'lucide:users', 4, CustomerMixWidget),
  widget('order-heatmap', 'order_heatmap', 'lucide:calendar-clock', 4, OrderHeatmapWidget),
  widget('store-radar', 'store_radar', 'lucide:radar', 4, StoreRadarWidget),
  widget('profit-waterfall', 'profit_waterfall', 'lucide:chart-column-decreasing', 4, ProfitWaterfallWidget),
  widget('category-mix', 'category_mix', 'lucide:layout-grid', 4, CategoryMixWidget),
  widget('supply-network', 'supply_network', 'lucide:share-2', 4, SupplyNetworkWidget),
  widget('material-price', 'material_price', 'lucide:chart-candlestick', 4, MaterialPriceWidget),
  widget('target-progress', 'target_progress', 'lucide:target', 12, TargetProgressWidget),
]

export const WIDGET_MAP: Record<string, WidgetDef> = Object.fromEntries(WIDGETS.map(widget => [widget.key, widget]))

/** 看板项：小组件键 + 宽度 */
export interface BoardItem {
  key: string
  span: number
}

/**
 * 默认看板，每行凑满 12 栅格：
 * 欢迎(4) 时间(4) 收藏入口(4) / 便签待办(3) 公告轮播(9) /
 * 今日统计(2) 经营指标(4) 销售构成(3) 转化漏斗(3) / 销售趋势(8) 订单流向(4) / 商品表现(8) 会员构成(4) /
 * 下单时段(4) 门店对比(4) 利润构成(4) / 品类构成(4) 供应网络(4) 原料行情(4) / 目标达成(12)
 */
export const DEFAULT_BOARD: BoardItem[] = WIDGETS.map(widget => ({ key: widget.key, span: widget.defaultSpan }))

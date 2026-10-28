import Papa from 'papaparse'

/**
 * 解析 CSV 文本为二维字符串数组（每行一个 string[]，首行通常作表头，自动跳过空行）。
 *
 * 封装 papaparse：应用层不直接依赖 papaparse，CSV 解析统一经 ~/utils 取用，
 * 第三方依赖收敛在本底层包。
 */
export function parseCsvRows(text: string): string[][] {
  const parsed = Papa.parse<string[]>(text, { skipEmptyLines: true })
  return parsed.data
}

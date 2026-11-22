#!/usr/bin/env node
// 把集成测试按类分成几片，给 CI 的库矩阵并行跑：每个用例都在新的临时库上起宿主，一片的耗时大体正比于它的用例数
// 用法：node test-shard.mjs <methods.json> <片号，从 1 起> <片数>
//   methods.json 是测试程序 `-list methods/json -noColor` 的输出：["命名空间.类.方法", …]（不加 -noColor，Linux 上 JSON 后面会多一个颜色复位符）
// 输出这一片的类全名，一行一个，交给 `--filter-class`（多个之间是或）
// 分法：类按用例数从多到少、同数按名字，依次放进当前用例最少的一片（同样少取片号小的）；只看列表本身，哪台机器算都一样
// 列表按方法计，一个理论不管有几行数据都算一个，所以各片的实际用例数会差十几个，不影响正确性
// 每个类恰好落在一片里，全部片合起来就是全部类；分到的片没有类时失败（片数比类还多），不会悄悄跑出一片空的
import fs from 'node:fs'

const [file, indexText, countText] = process.argv.slice(2)
const index = Number(indexText)
const count = Number(countText)
if (!file || !Number.isInteger(count) || count < 1 || !Number.isInteger(index) || index < 1 || index > count) {
  console.error('usage: node test-shard.mjs <methods.json> <shard 1..N> <N>')
  process.exit(64)
}

const methods = JSON.parse(fs.readFileSync(file, 'utf8'))
if (!Array.isArray(methods) || methods.length === 0 || methods.some(m => typeof m !== 'string' || !m.includes('.'))) {
  console.error(`${file} is not a non-empty list of 'Namespace.Class.Method' names`)
  process.exit(1)
}

// 方法名里没有点，最后一个点之前就是类全名（嵌套类带 +）
const sizes = new Map()
for (const method of methods) {
  const type = method.slice(0, method.lastIndexOf('.'))
  sizes.set(type, (sizes.get(type) ?? 0) + 1)
}

const shards = Array.from({ length: count }, () => ({ size: 0, types: [] }))
const ordered = [...sizes].sort(([a, x], [b, y]) => y - x || (a < b ? -1 : a > b ? 1 : 0))
for (const [type, size] of ordered) {
  const lightest = shards.reduce((best, shard) => (shard.size < best.size ? shard : best))
  lightest.types.push(type)
  lightest.size += size
}

for (const [i, shard] of shards.entries())
  console.error(`shard ${i + 1}/${count}: ${shard.types.length} classes, ${shard.size} tests`)

const mine = shards[index - 1]
if (mine.types.length === 0) {
  console.error(`shard ${index}/${count} has no test classes; use fewer shards`)
  process.exit(1)
}

console.log(mine.types.join('\n'))

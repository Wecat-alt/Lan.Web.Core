/** 显示基准 — 画面始终占用这个 Three.js 单位尺寸 */
export const DISPLAY_BASE = 25

/** 雷达原点在画面中的 Z 偏移 (负值=往南, 正值=往北), Three.js 单位 */
export const RADAR_ORIGIN_Z = 0

/** 雷达探测范围配置 (真实米数) */
export const radarConfig = {
  range: 500,  // 探测半径 (m), X/Y方向
  height: 500, // 探测高度 (m), Z方向
}

/** 显示比例: 1 真实米 = 多少 Three.js 单位 */
export function getDisplayScale() {
  return DISPLAY_BASE / radarConfig.range
}

/** 真实米 → Three.js 显示单位 */
export function toDisplay(meters) {
  return meters * getDisplayScale()
}

/** Three.js 显示单位 → 真实米 */
export function toReal(display) {
  return display / getDisplayScale()
}

/** 根据最大范围动态计算距离圈 (真实米数) */
export function computeRangeRings(maxRange, count = 7) {
  const rings = []
  for (let i = 1; i <= count; i++) {
    rings.push(Math.round((maxRange * i) / count))
  }
  return rings
}

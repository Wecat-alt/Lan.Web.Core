/**
 * 格式化小数为指定位数
 * @param {*} value - 要格式化的值
 * @param {number} digits - 小数位数，默认 6
 * @returns {string} 格式化后的字符串，无效输入返回原值
 */
export function formatDecimal(value, digits = 6) {
  if (value === null || value === undefined || value === '') return value
  const num = Number(value)
  return isNaN(num) ? value : num.toFixed(digits)
}

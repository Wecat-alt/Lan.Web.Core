<template>
  <div id="app">
    <div class="dashboard">
      <div class="header">
        <div class="title">雷视融合监测大数据平台</div>
        <div class="time">{{ currentTime }}</div>
      </div>

      <!-- 左侧面板 -->
      <div class="panel left-top">
        <div class="panel-title">数量</div>
        <div class="data-list">
          <div class="data-item">
            <span class="data-label">XXXXX</span>
            <span class="data-value">4.87</span>
          </div>
          <div class="data-item">
            <span class="data-label">XXXXX</span>
            <span class="data-value">6.4%</span>
          </div>
          <div class="data-item">
            <span class="data-label">XXXXX</span>
            <span class="data-value">+8.7%</span>
          </div>
          <div class="data-item">
            <span class="data-label">XXXXX</span>
            <span class="data-value">1.98</span>
          </div>
        </div>
      </div>

      <div class="panel left-middle">
        <div class="panel-title">分布</div>
        <div class="chart" id="populationChart"></div>
      </div>

      <div class="panel left-bottom">
        <div class="panel-title">XXXXXX</div>
        <div class="data-list">
          <div class="data-item">
            <span class="data-label">XXXXXX</span>
            <span class="data-value">32.5%</span>
            <div class="progress-bar">
              <div class="progress" style="width: 65%"></div>
            </div>
          </div>
          <div class="data-item">
            <span class="data-label">XXXXXX</span>
            <span class="data-value">28.7%</span>
            <div class="progress-bar">
              <div class="progress" style="width: 57%"></div>
            </div>
          </div>
          <div class="data-item">
            <span class="data-label">XXXXXX</span>
            <span class="data-value">15.3%</span>
            <div class="progress-bar">
              <div class="progress" style="width: 31%"></div>
            </div>
          </div>
          <div class="data-item">
            <span class="data-label">XXXXXX</span>
            <span class="data-value">12.8%</span>
            <div class="progress-bar">
              <div class="progress" style="width: 26%"></div>
            </div>
          </div>
        </div>
      </div>

      <!-- 中间地图 -->
      <div class="panel map-container">
        <div class="panel-title">湖南省区域安防应用地图</div>
        <div ref="echartsRef" class="chart" id="mapChart"></div>
      </div>

      <!-- 右侧面板 -->
      <div class="panel right-top">
        <div class="panel-title">实时数据</div>
        <div class="number-display blink">3,847<span class="unit">条</span></div>
        <div style="text-align: center; color: #a0d2ff">今日报警数量</div>

        <div class="number-display float">128<span class="unit">万</span></div>
        <div style="text-align: center; color: #a0d2ff">今日处理数量</div>
      </div>

      <div class="panel right-middle">
        <div class="panel-title">防区对比</div>
        <div class="chart" id="comparisonChart"></div>
      </div>

      <div class="panel right-bottom">
        <div class="panel-title">报警趋势</div>
        <div class="chart" id="trendChart"></div>
      </div>
    </div>
  </div>
</template>
<script setup>
import chinaMap from '@/assets/map/hunan.json'
import * as echarts from 'echarts'
import { onBeforeUnmount, onMounted, ref } from 'vue'

const echartsRef = ref(null)
const currentTime = ref('')
let clockTimer = null
let resizeHandler = null

// 更新时间
const updateTime = () => {
  const now = new Date()
  currentTime.value = now.toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  })
}

// 初始化图表
const initCharts = () => {
  // 初始化地图
  const mapChart = echarts.init(echartsRef.value)

  // 模拟湖南省地图数据
  const mapOption = {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'item',
      formatter: '{b}: {c} ',
    },
    visualMap: {
      min: 0,
      max: 8000,
      text: ['高', '低'],
      realtime: false,
      calculable: true,
      inRange: {
        color: [
          '#4575b4',
          '#74add1',
          '#abd9e9',
          '#e0f3f8',
          '#ffffbf',
          '#fee090',
          '#fdae61',
          '#f46d43',
          '#d73027',
        ],
      },
      textStyle: {
        color: '#fff',
      },
    },
    series: [
      {
        name: 'GDP',
        type: 'map',
        map: 'hunan',
        roam: true,
        emphasis: {
          label: {
            show: true,
            color: '#fff',
          },
          itemStyle: {
            areaColor: '#00c6ff',
          },
        },
        data: [
          { name: '长沙市', value: 12142.52 },
          { name: '岳阳市', value: 4001.55 },
          { name: '常德市', value: 4054.15 },
          { name: '衡阳市', value: 3840.31 },
          { name: '株洲市', value: 3420.26 },
          { name: '郴州市', value: 2770.08 },
          { name: '湘潭市', value: 2435.07 },
          { name: '邵阳市', value: 2461.53 },
          { name: '永州市', value: 2261.08 },
          { name: '益阳市', value: 2108.02 },
          { name: '娄底市', value: 1825.76 },
          { name: '怀化市', value: 1817.8 },
          { name: '张家界市', value: 1592.39 },
          { name: '湘西土家族苗族自治州', value: 1792.11 },
        ],
        nameMap: {
          长沙市: '长沙市',
          岳阳市: '岳阳市',
          常德市: '常德市',
          衡阳市: '衡阳市',
          株洲市: '株洲市',
          郴州市: '郴州市',
          湘潭市: '湘潭市',
          邵阳市: '邵阳市',
          永州市: '永州市',
          益阳市: '益阳市',
          娄底市: '娄底市',
          怀化市: '怀化市',
          张家界市: '张家界市',
          湘西土家族苗族自治州: '湘西州',
        },
      },
    ],
  }

  // 注册一个模拟的湖南地图
  echarts.registerMap('hunan', {
    geoJSON: chinaMap,
  })

  mapChart.setOption(mapOption)

  // 初始化人口分布图
  const populationChart = echarts.init(document.getElementById('populationChart'))
  const populationOption = {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'shadow',
      },
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      containLabel: true,
    },
    xAxis: {
      type: 'value',
      axisLine: {
        lineStyle: {
          color: '#4dabf7',
        },
      },
      splitLine: {
        lineStyle: {
          color: 'rgba(77, 171, 247, 0.2)',
        },
      },
    },
    yAxis: {
      type: 'category',
      data: [
        '长沙市',
        '衡阳市',
        '株洲市',
        '湘潭市',
        '邵阳市',
        '岳阳市',
        '常德市',
        '张家界',
        '益阳市',
        '郴州市',
        '永州市',
        '怀化市',
        '娄底市',
        '湘西州',
      ],
      axisLine: {
        lineStyle: {
          color: '#4dabf7',
        },
      },
    },
    series: [
      {
        name: '人口(万)',
        type: 'bar',
        data: [1004, 730, 390, 290, 730, 530, 530, 152, 385, 470, 530, 460, 380, 264],
        itemStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
            { offset: 0, color: '#00c6ff' },
            { offset: 1, color: '#0072ff' },
          ]),
        },
      },
    ],
  }
  populationChart.setOption(populationOption)

  // 初始化区域对比图
  const comparisonChart = echarts.init(document.getElementById('comparisonChart'))
  const comparisonOption = {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
      axisPointer: {
        type: 'shadow',
      },
    },
    legend: {
      data: ['报警数量1', '报警数量2', '报警数量3'],
      textStyle: {
        color: '#a0d2ff',
      },
      right: 10,
      top: 0,
    },
    radar: {
      indicator: [
        { name: '防区1', max: 100 },
        { name: '防区2', max: 100 },
        { name: '防区3', max: 100 },
        { name: '防区4', max: 100 },
        { name: '防区5', max: 100 },
        { name: '防区6', max: 100 },
      ],
      shape: 'circle',
      splitNumber: 5,
      axisName: {
        color: '#a0d2ff',
        backgroundColor: 'transparent',
        borderRadius: 3,
        padding: [3, 5],
      },
      splitLine: {
        lineStyle: {
          color: [
            'rgba(77, 171, 247, 0.1)',
            'rgba(77, 171, 247, 0.2)',
            'rgba(77, 171, 247, 0.4)',
            'rgba(77, 171, 247, 0.6)',
            'rgba(77, 171, 247, 0.8)',
            'rgba(77, 171, 247, 1)',
          ].reverse(),
        },
      },
      splitArea: {
        show: false,
      },
      axisLine: {
        lineStyle: {
          color: 'rgba(77, 171, 247, 0.5)',
        },
      },
    },
    series: [
      {
        name: '区域对比',
        type: 'radar',
        emphasis: {
          lineStyle: {
            width: 4,
          },
        },
        data: [
          {
            value: [95, 70, 65, 80, 75, 60],
            name: '报警数量1',
            symbol: 'none',
            lineStyle: {
              type: 'dashed',
            },
            areaStyle: {
              color: new echarts.graphic.RadialGradient(0.5, 0.5, 1, [
                { offset: 0, color: 'rgba(0, 198, 255, 0.7)' },
                { offset: 1, color: 'rgba(0, 198, 255, 0.1)' },
              ]),
            },
          },
          {
            value: [85, 65, 75, 70, 60, 55],
            name: '报警数量2',
            areaStyle: {
              color: new echarts.graphic.RadialGradient(0.5, 0.5, 1, [
                { offset: 0, color: 'rgba(0, 114, 255, 0.7)' },
                { offset: 1, color: 'rgba(0, 114, 255, 0.1)' },
              ]),
            },
          },
          {
            value: [75, 80, 70, 65, 70, 50],
            name: '报警数量3',
            areaStyle: {
              color: new echarts.graphic.RadialGradient(0.5, 0.5, 1, [
                { offset: 0, color: 'rgba(77, 171, 247, 0.7)' },
                { offset: 1, color: 'rgba(77, 171, 247, 0.1)' },
              ]),
            },
          },
        ],
      },
    ],
  }
  comparisonChart.setOption(comparisonOption)

  // 初始化趋势图
  const trendChart = echarts.init(document.getElementById('trendChart'))
  const trendOption = {
    backgroundColor: 'transparent',
    tooltip: {
      trigger: 'axis',
    },
    legend: {
      data: ['相机', '雷达'],
      textStyle: {
        color: '#a0d2ff',
      },
      top: 0,
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      top: '20%',
      containLabel: true,
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月'],
      axisLine: {
        lineStyle: {
          color: '#4dabf7',
        },
      },
    },
    yAxis: {
      type: 'value',
      axisLine: {
        lineStyle: {
          color: '#4dabf7',
        },
      },
      splitLine: {
        lineStyle: {
          color: 'rgba(77, 171, 247, 0.2)',
        },
      },
    },
    series: [
      {
        name: '相机',
        type: 'line',
        stack: '总量',
        smooth: true,
        lineStyle: {
          width: 3,
          color: '#00c6ff',
        },
        symbol: 'circle',
        symbolSize: 8,
        itemStyle: {
          color: '#00c6ff',
        },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(0, 198, 255, 0.5)' },
            { offset: 1, color: 'rgba(0, 198, 255, 0.1)' },
          ]),
        },
        data: [6.2, 6.5, 6.8, 7.1, 7.0, 6.9, 6.8, 6.7, 6.6, 6.5, 6.4, 6.4],
      },
      {
        name: '雷达',
        type: 'line',
        stack: '总量',
        smooth: true,
        lineStyle: {
          width: 3,
          color: '#0072ff',
        },
        symbol: 'circle',
        symbolSize: 8,
        itemStyle: {
          color: '#0072ff',
        },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(0, 114, 255, 0.5)' },
            { offset: 1, color: 'rgba(0, 114, 255, 0.1)' },
          ]),
        },
        data: [5.8, 6.0, 6.3, 6.7, 6.5, 6.4, 6.2, 6.0, 5.9, 5.8, 5.7, 5.7],
      },
    ],
  }
  trendChart.setOption(trendOption)

  // 窗口大小改变时重置图表大小
  resizeHandler = function () {
    mapChart.resize()
    populationChart.resize()
    comparisonChart.resize()
    trendChart.resize()
  }

  window.addEventListener('resize', resizeHandler)
}

onMounted(() => {
  updateTime()
  clockTimer = window.setInterval(updateTime, 1000)
  initCharts()
})

onBeforeUnmount(() => {
  if (clockTimer) {
    window.clearInterval(clockTimer)
    clockTimer = null
  }

  if (resizeHandler) {
    window.removeEventListener('resize', resizeHandler)
    resizeHandler = null
  }
})
</script>

<style>
* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
  font-family: 'Microsoft YaHei', sans-serif;
}

body {
  background-color: #0a1a35;
  color: #fff;
  overflow: hidden;
}

.dashboard {
  width: 100vw;
  height: 100vh;
  padding: 20px;
  display: grid;
  grid-template-columns: 1fr 2fr 1fr;
  grid-template-rows: 80px 1fr 1fr 1fr;
  grid-gap: 20px;
  background:
    radial-gradient(circle at 20% 30%, rgba(30, 60, 120, 0.5) 0%, transparent 40%),
    radial-gradient(circle at 80% 70%, rgba(30, 120, 180, 0.4) 0%, transparent 40%);
  position: relative;
}

/* 网格线背景 */
.dashboard::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-image:
    linear-gradient(rgba(20, 80, 150, 0.1) 1px, transparent 1px),
    linear-gradient(90deg, rgba(20, 80, 150, 0.1) 1px, transparent 1px);
  background-size: 50px 50px;
  pointer-events: none;
  z-index: 0;
}

.header {
  grid-column: 1 / 4;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 20px;
  background: rgba(16, 35, 70, 0.7);
  border-radius: 10px;
  box-shadow: 0 0 20px rgba(0, 150, 255, 0.3);
  border: 1px solid rgba(0, 150, 255, 0.5);
  position: relative;
  z-index: 1;
}

.title {
  font-size: 32px;
  font-weight: bold;
  background: linear-gradient(90deg, #00c6ff, #0072ff);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  text-shadow: 0 0 10px rgba(0, 198, 255, 0.5);
}

.time {
  font-size: 20px;
  color: #4dabf7;
}

.panel {
  background: rgba(16, 35, 70, 0.7);
  border-radius: 10px;
  padding: 15px;
  box-shadow: 0 0 15px rgba(0, 150, 255, 0.2);
  border: 1px solid rgba(0, 150, 255, 0.3);
  position: relative;
  overflow: hidden;
  z-index: 1;
}

.panel::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 3px;
  background: linear-gradient(90deg, #00c6ff, #0072ff);
}

.panel-title {
  font-size: 18px;
  margin-bottom: 15px;
  color: #4dabf7;
  display: flex;
  align-items: center;
}

.panel-title::before {
  content: '';
  display: inline-block;
  width: 4px;
  height: 16px;
  background: #0072ff;
  margin-right: 8px;
  border-radius: 2px;
}

.map-container {
  grid-column: 2;
  grid-row: 2 / 5;
}

.left-top {
  grid-column: 1;
  grid-row: 2;
}

.left-middle {
  grid-column: 1;
  grid-row: 3;
}

.left-bottom {
  grid-column: 1;
  grid-row: 4;
}

.right-top {
  grid-column: 3;
  grid-row: 2;
}

.right-middle {
  grid-column: 3;
  grid-row: 3;
}

.right-bottom {
  grid-column: 3;
  grid-row: 4;
}

.chart {
  width: 100%;
  height: calc(100% - 40px);
}

.data-item {
  display: flex;
  justify-content: space-between;
  padding: 10px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.data-label {
  color: #a0d2ff;
}

.data-value {
  color: #00c6ff;
  font-weight: bold;
}

.progress-bar {
  height: 6px;
  background: rgba(255, 255, 255, 0.1);
  border-radius: 3px;
  margin-top: 5px;
  overflow: hidden;
}

.progress {
  height: 100%;
  background: linear-gradient(90deg, #00c6ff, #0072ff);
  border-radius: 3px;
}

.number-display {
  font-size: 24px;
  font-weight: bold;
  color: #00c6ff;
  text-align: center;
  margin: 10px 0;
}

.unit {
  font-size: 14px;
  color: #a0d2ff;
}

/* 闪烁动画 */
@keyframes blink {
  0%,
  100% {
    opacity: 1;
  }
  50% {
    opacity: 0.5;
  }
}

.blink {
  animation: blink 2s infinite;
}

/* 浮动动画 */
@keyframes float {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-5px);
  }
}

.float {
  animation: float 3s ease-in-out infinite;
}
</style>

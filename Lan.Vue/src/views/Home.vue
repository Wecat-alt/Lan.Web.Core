<template>
  <div>
    <!-- 统计卡片 -->
    <el-row :gutter="20" class="statistics-row">
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" style="text-align: center">
          <div style="color: #409eff; font-size: 24px; margin-bottom: 10px">254</div>
          <div style="color: #909399; font-size: 14px">今日访问量</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" style="text-align: center">
          <div style="color: #67c23a; font-size: 24px; margin-bottom: 10px">3,689</div>
          <div style="color: #909399; font-size: 14px">雷达数量</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" style="text-align: center">
          <div style="color: #e6a23c; font-size: 24px; margin-bottom: 10px">890</div>
          <div style="color: #909399; font-size: 14px">今日报警总数</div>
        </el-card>
      </el-col>
      <el-col :xs="24" :sm="12" :md="6">
        <el-card shadow="hover" style="text-align: center">
          <div style="color: #f56c6c; font-size: 24px; margin-bottom: 10px">92.5%</div>
          <div style="color: #909399; font-size: 14px">转化率</div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 图表区域 -->
    <el-row :gutter="20">
      <!-- 柱状图 -->
      <el-col :xs="24" :lg="12">
        <div class="chart-container">
          <div class="chart-title">趋势图</div>
          <div id="barChart" class="chart"></div>
        </div>
      </el-col>

      <!-- 折线图 -->
      <el-col :xs="24" :lg="12">
        <div class="chart-container">
          <div class="chart-title">趋势图</div>
          <div id="lineChart" class="chart"></div>
        </div>
      </el-col>
    </el-row>

    <el-row :gutter="20">
      <!-- 饼图 -->
      <el-col :xs="24" :lg="12">
        <div class="chart-container">
          <div class="chart-title">趋势图</div>
          <div id="pieChart" class="chart"></div>
        </div>
      </el-col>

      <!-- 雷达图 -->
      <el-col :xs="24" :lg="12">
        <div class="chart-container">
          <div class="chart-title">趋势图</div>
          <div id="radarChart" class="chart"></div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup>
import * as echarts from 'echarts'
import { onBeforeUnmount, onMounted } from 'vue'

let resizeHandler = null

const initCharts = () => {
  // 柱状图
  const barChart = echarts.init(document.getElementById('barChart'))
  const barOption = {
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
      type: 'category',
      data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月', '8月', '9月', '10月', '11月', '12月'],
    },
    yAxis: {
      type: 'value',
    },
    series: [
      {
        data: [120, 200, 150, 80, 70, 110, 130, 180, 210, 240, 190, 160],
        type: 'bar',
        itemStyle: {
          color: '#409EFF',
        },
      },
    ],
  }
  barChart.setOption(barOption)

  // 折线图
  const lineChart = echarts.init(document.getElementById('lineChart'))
  const lineOption = {
    tooltip: {
      trigger: 'axis',
    },
    grid: {
      left: '3%',
      right: '4%',
      bottom: '3%',
      containLabel: true,
    },
    xAxis: {
      type: 'category',
      boundaryGap: false,
      data: ['1月', '2月', '3月', '4月', '5月', '6月', '7月'],
    },
    yAxis: {
      type: 'value',
    },
    series: [
      {
        data: [820, 932, 901, 934, 1290, 1330, 1420],
        type: 'line',
        smooth: true,
        areaStyle: {
          color: {
            type: 'linear',
            x: 0,
            y: 0,
            x2: 0,
            y2: 1,
            colorStops: [
              {
                offset: 0,
                color: 'rgba(64, 158, 255, 0.5)',
              },
              {
                offset: 1,
                color: 'rgba(64, 158, 255, 0)',
              },
            ],
          },
        },
        lineStyle: {
          color: '#409EFF',
        },
      },
    ],
  }
  lineChart.setOption(lineOption)

  // 饼图
  const pieChart = echarts.init(document.getElementById('pieChart'))
  const pieOption = {
    tooltip: {
      trigger: 'item',
    },
    legend: {
      orient: 'vertical',
      right: 10,
      top: 'center',
    },
    series: [
      {
        name: '产品类别',
        type: 'pie',
        radius: ['40%', '70%'],
        avoidLabelOverlap: false,
        itemStyle: {
          borderRadius: 10,
          borderColor: '#fff',
          borderWidth: 2,
        },
        label: {
          show: false,
          position: 'center',
        },
        emphasis: {
          label: {
            show: true,
            fontSize: '18',
            fontWeight: 'bold',
          },
        },
        labelLine: {
          show: false,
        },
        data: [
          { value: 1048, name: '电子产品' },
          { value: 735, name: '家居用品' },
          { value: 580, name: '服装鞋帽' },
          { value: 484, name: '食品饮料' },
          { value: 300, name: '其他' },
        ],
      },
    ],
  }
  pieChart.setOption(pieOption)

  // 雷达图
  const radarChart = echarts.init(document.getElementById('radarChart'))
  const radarOption = {
    tooltip: {
      trigger: 'item',
    },
    radar: {
      indicator: [
        { name: '性能', max: 100 },
        { name: '价格', max: 100 },
        { name: '设计', max: 100 },
        { name: '易用性', max: 100 },
        { name: '可靠性', max: 100 },
      ],
    },
    series: [
      {
        type: 'radar',
        data: [
          {
            value: [90, 85, 70, 75, 95],
            name: '产品A',
            areaStyle: {
              color: 'rgba(64, 158, 255, 0.5)',
            },
            lineStyle: {
              color: '#409EFF',
            },
          },
          {
            value: [70, 75, 90, 80, 85],
            name: '产品B',
            areaStyle: {
              color: 'rgba(103, 194, 58, 0.5)',
            },
            lineStyle: {
              color: '#67C23A',
            },
          },
        ],
      },
    ],
  }
  radarChart.setOption(radarOption)

  // 响应式调整
  resizeHandler = function () {
    barChart.resize()
    lineChart.resize()
    pieChart.resize()
    radarChart.resize()
  }

  window.addEventListener('resize', resizeHandler)
}

onMounted(() => {
  initCharts()
})

onBeforeUnmount(() => {
  if (resizeHandler) {
    window.removeEventListener('resize', resizeHandler)
    resizeHandler = null
  }
})
</script>

<style>
/* * {
      margin: 0;
      padding: 0;
      box-sizing: border-box;
    }
    body, html {
      height: 100%;
      font-family: 'Helvetica Neue', Helvetica, 'PingFang SC', 'Hiragino Sans GB', 'Microsoft YaHei', Arial, sans-serif;
      background-color: #f5f7fa;
    }
    #app {
      height: 100%;
    }
    .dashboard-container {
      height: 100%;
      display: flex;
      flex-direction: column;
    }
    .header {
      background-color: #fff;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      padding: 0 20px;
      z-index: 1000;
    }
    .main-container {
      flex: 1;
      padding: 20px;
      overflow: auto;
    }
    .statistics-row {
      margin-bottom: 20px;
    }
    .chart-container {
      background: #fff;
      border-radius: 4px;
      padding: 20px;
      box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
      margin-bottom: 20px;
      height: 400px;
    }
    .chart-title {
      font-size: 16px;
      font-weight: bold;
      margin-bottom: 15px;
      color: #303133;
    }
    .chart {
      width: 100%;
      height: 340px;
    }
    .el-row {
      margin-bottom: 20px;
    }
    .el-col {
      border-radius: 4px;
    }
    .grid-content {
      border-radius: 4px;
      min-height: 36px;
    } */
</style>

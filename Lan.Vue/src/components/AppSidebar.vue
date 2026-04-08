<template>
  <el-menu default-active="realtime_map" mode="horizontal" @select="handleMenuSelect">
    <!-- <el-menu-item index="screen">
        <span>大屏展示</span>
      </el-menu-item> -->
    <!-- <el-menu-item index="user">
        <span>人员管理</span>
      </el-menu-item> -->
    <el-menu-item index="wizard">
      <span>{{ $t('nav.wizard') }}</span>
    </el-menu-item>
    <!-- <el-sub-menu index="3">
      <template #title>{{ $t('nav.userManagement') }}</template>
      <el-menu-item index="user">
        <span>{{ $t('nav.userManagement') }}</span>
      </el-menu-item>
    </el-sub-menu> -->
    <el-sub-menu index="0">
      <template #title>{{ $t('nav.device') }}</template>
      <el-menu-item index="defencearea">
        <span>{{ $t('nav.zone') }}</span>
      </el-menu-item>
      <el-menu-item index="camera">
        <span>{{ $t('nav.camera') }}</span>
      </el-menu-item>
      <el-menu-item index="radar">
        <span>{{ $t('nav.radar') }}</span>
      </el-menu-item>
    </el-sub-menu>

    <el-menu-item index="calibration">
      <span>{{ $t('nav.calibration') }}</span>
    </el-menu-item>

    <el-menu-item index="realtime_map">
      <span>{{ $t('nav.gis') }}</span>
    </el-menu-item>

    <el-menu-item index="alarm">
      <span>{{ $t('nav.alarm') }}</span>
    </el-menu-item>

    <el-sub-menu index="2">
      <template #title>{{ $t('nav.settings') }}</template>
      <el-menu-item index="config">
        <span>{{ $t('nav.settings') }}</span>
      </el-menu-item>
      <el-menu-item index="autoMap">
        <span>{{ $t('nav.autoMap') }}</span>
      </el-menu-item>
      <el-menu-item index="livePreview">
        <span>{{ $t('nav.LivePreview') }}</span>
      </el-menu-item>
      <!-- <el-menu-item index="defencearea">
        <span>{{ $t('nav.pop_up') }}</span>
      </el-menu-item> -->
    </el-sub-menu>
  </el-menu>
</template>

<script>
import { defineComponent, onMounted } from 'vue'

export default defineComponent({
  name: 'AppSidebar',
  components: {},
  emits: ['menu-change'],
  setup(props, { emit }) {
    onMounted(() => {
      localStorage.removeItem('wizard')
    })

    const handleMenuSelect = (index) => {
      if (index === 'wizard') {
        localStorage.setItem('wizard', 'radar')

        emit('menu-change', 'radar')
        return
      }

      localStorage.removeItem('wizard')

      emit('menu-change', index)
    }

    return {
      handleMenuSelect,
    }
  },
})
</script>

<style scoped></style>

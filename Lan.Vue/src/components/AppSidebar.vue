<template>
  <el-menu :default-active="defaultActive" mode="horizontal" @select="handleMenuSelect">
    <!-- <el-menu-item index="screen">
        <span>大屏展示</span>
      </el-menu-item> -->
    <!-- <el-menu-item index="user">
        <span>人员管理</span>
      </el-menu-item> -->
    <el-menu-item v-if="canAccessMenu('wizard')" index="wizard">
      <span>{{ $t('nav.wizard') }}</span>
    </el-menu-item>
    <!-- <el-sub-menu index="3">
      <template #title>{{ $t('nav.userManagement') }}</template>
      <el-menu-item index="user">
        <span>{{ $t('nav.userManagement') }}</span>
      </el-menu-item>
    </el-sub-menu> -->
    <el-sub-menu
      v-if="canAccessMenu('defencearea') || canAccessMenu('camera') || canAccessMenu('radar')"
      index="0"
    >
      <template #title>{{ $t('nav.device') }}</template>
      <el-menu-item v-if="canAccessMenu('defencearea')" index="defencearea">
        <span>{{ $t('nav.zone') }}</span>
      </el-menu-item>
      <el-menu-item v-if="canAccessMenu('camera')" index="camera">
        <span>{{ $t('nav.camera') }}</span>
      </el-menu-item>
      <el-menu-item v-if="canAccessMenu('radar')" index="radar">
        <span>{{ $t('nav.radar') }}</span>
      </el-menu-item>
    </el-sub-menu>

    <el-menu-item v-if="canAccessMenu('calibration')" index="calibration">
      <span>{{ $t('nav.calibration') }}</span>
    </el-menu-item>

    <el-menu-item v-if="canAccessMenu('realtime_map')" index="realtime_map">
      <span>{{ $t('nav.gis') }}</span>
    </el-menu-item>

    <el-menu-item v-if="canAccessMenu('alarm')" index="alarm">
      <span>{{ $t('nav.alarm') }}</span>
    </el-menu-item>

    <el-sub-menu
      v-if="canAccessMenu('config') || canAccessMenu('autoMap') || canAccessMenu('livePreview')"
      index="2"
    >
      <template #title>{{ $t('nav.settings') }}</template>
      <el-menu-item v-if="canAccessMenu('config')" index="config">
        <span>{{ $t('nav.settings') }}</span>
      </el-menu-item>
      <el-menu-item v-if="canAccessMenu('autoMap')" index="autoMap">
        <span>{{ $t('nav.autoMap') }}</span>
      </el-menu-item>
      <el-menu-item v-if="canAccessMenu('livePreview')" index="livePreview">
        <span>{{ $t('nav.LivePreview') }}</span>
      </el-menu-item>
      <!-- <el-menu-item index="defencearea">
        <span>{{ $t('nav.pop_up') }}</span>
      </el-menu-item> -->
    </el-sub-menu>
  </el-menu>
</template>

<script>
import { canAccessMenu } from '@/utils/permission'
import { computed, defineComponent, onMounted } from 'vue'

export default defineComponent({
  name: 'AppSidebar',
  components: {},
  emits: ['menu-change'],
  setup(props, { emit }) {
    const defaultCandidates = [
      'realtime_map',
      'wizard',
      'radar',
      'camera',
      'defencearea',
      'alarm',
      'calibration',
      'config',
      'autoMap',
      'livePreview',
    ]

    const defaultActive = computed(() => {
      const first = defaultCandidates.find((item) => canAccessMenu(item))
      return first || ''
    })

    onMounted(() => {
      localStorage.removeItem('wizard')
    })

    const handleMenuSelect = (index) => {
      if (!canAccessMenu(index) && index !== 'wizard') {
        return
      }

      if (index === 'wizard') {
        localStorage.setItem('wizard', 'radar')

        emit('menu-change', 'radar')
        return
      }

      localStorage.removeItem('wizard')

      emit('menu-change', index)
    }

    return {
      canAccessMenu,
      defaultActive,
      handleMenuSelect,
    }
  },
})
</script>

<style scoped></style>

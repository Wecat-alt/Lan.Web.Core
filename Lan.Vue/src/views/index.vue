<template>
  <el-container class="app-shell">
    <el-header class="app-header">
      <div class="header-content">
        <div class="logo-con">
          <img src="/images/login_logo.png" class="top-logo" />
          <label>{{ $t('common.system_title') }}</label>
        </div>
        <app-sidebar @menu-change="handleMenuChange" />
        <div class="user-info">
          <!-- <div class="user-avatar">A</div> -->

          <a href="/play.rar" download="play.rar">Download</a>
          <el-icon><Avatar /></el-icon>
          <span>{{ username }}</span>
          <LanguageSwitcher />
        </div>
      </div>
    </el-header>
    <el-main class="app-main">
      <component :is="currentView" />
    </el-main>
    <!-- <el-footer class="app-footer">
          <div class="footer-content">
            <p>@@@.</p>
          </div>
        </el-footer> -->
  </el-container>
</template>

<script>
import { canAccessMenu, getAuthProfile } from '@/utils/permission'
import { ensureSignalRConnection, setSignalRReceiveEnabled } from '@/utils/signalRUtils'
import { defineComponent, getCurrentInstance, onBeforeUnmount, onMounted, shallowRef } from 'vue'

import AppSidebar from '../components/AppSidebar.vue'
import Home from '../views/Home.vue'

import calibration from '@/views/device/calibration/index'
import camera from '@/views/device/camera/index'
import defencearea from '@/views/device/defencearea/index'
import radar from '@/views/device/radar/index'
import alarm from '@/views/query/alarm_index'

import livePreview from '@/views/livePreview/index.vue'
import autoMap from '@/views/map/AutoMap.vue'
import realtime_map from '@/views/map/realtime_map'
import config from '@/views/system/config/index'
import user from '@/views/system/user/index'

import LanguageSwitcher from '../components/LanguageSwitch.vue'

//import { useI18n } from 'vue-i18n'

export default defineComponent({
  name: 'App',
  components: {
    AppSidebar,
    LanguageSwitcher,
  },
  setup() {
    const { proxy } = getCurrentInstance()
    //const { t } = useI18n()
    const views = {
      home: Home,
      defencearea: defencearea,
      camera: camera,
      radar: radar,
      wizard: radar,
      realtime_map: realtime_map,
      autoMap: autoMap,
      livePreview: livePreview,
      alarm: alarm,
      config: config,
      user: user,
      calibration: calibration,

      LanguageSwitcher: LanguageSwitcher,
    }

    const menuCandidates = [
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

    const resolveFirstAllowedMenu = () => {
      const first = menuCandidates.find((item) => canAccessMenu(item))
      return first || 'realtime_map'
    }

    const currentView = shallowRef(views[resolveFirstAllowedMenu()] || realtime_map)
    const signalRApi = window.__APP_CONFIG__.VITE_SIGNALR_URL

    const updateSignalRReceiveState = (viewKey) => {
      // 主页不消费实时数据，业务菜单开启消费
      setSignalRReceiveEnabled(viewKey !== 'home')
    }

    const authProfile = getAuthProfile()
    const username = authProfile.username || 'user'

    const handleMenuChange = (view) => {
      if (!canAccessMenu(view) && view !== 'wizard') {
        proxy.$modal && proxy.$modal.msgError && proxy.$modal.msgError('无权限访问该菜单')
        return
      }

      // wizard flow: use localStorage wizard key to control next step
      const wizardStep = localStorage.getItem('wizard')
      // 打开 livePreview 为单独窗口
      if (view === 'livePreview') {
        window.open('/livePreview', '_blank')
        return
      }

      if (view === 'radar' && wizardStep) {
        currentView.value = views['radar'] || realtime_map
        updateSignalRReceiveState('radar')
        return
      }

      currentView.value = views[view] || realtime_map
      updateSignalRReceiveState(view)
    }

    const wizardHandler = (e) => {
      const next = e && e.detail ? e.detail : null
      if (next && views[next]) {
        currentView.value = views[next]
      }
    }

    onMounted(() => {
      if (signalRApi) {
        ensureSignalRConnection({ api: signalRApi })
      }

      updateSignalRReceiveState(resolveFirstAllowedMenu())
      window.addEventListener('wizard-next', wizardHandler)
    })

    onBeforeUnmount(() => {
      window.removeEventListener('wizard-next', wizardHandler)
    })

    return {
      currentView,
      username,
      handleMenuChange,
    }
  },
})
</script>

<style lang="scss">
.app-shell {
  height: 100vh;
  overflow: hidden;
}

.app-main {
  min-height: 0;
  overflow: auto;
  box-sizing: border-box;
}
</style>

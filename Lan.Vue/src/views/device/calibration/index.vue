<template>
  <div class="calibration-page">
    <!-- ══════════ 向导模式步骤条 ══════════ -->
    <div v-if="isWizard" class="wizard-banner">
      <el-steps :active="3" align-center finish-status="success">
        <el-step :title="$t('nav.radar')" />
        <el-step :title="$t('nav.camera')" />
        <el-step :title="$t('nav.zone')" />
        <el-step :title="$t('nav.calibration')" />
      </el-steps>
      <p class="wizard-banner-desc">第 4 步：选择防区和相机，完成校准后进入 GIS 地图</p>
      <div class="wizard-banner-actions">
        <el-button @click="wizardExit">退出向导</el-button>
        <el-button type="primary" @click="wizardFinish">完成，进入 GIS 地图</el-button>
      </div>
    </div>

    <section class="toolbar-card">
      <el-form
        :model="queryParams"
        label-position="right"
        inline
        ref="queryRef"
        v-show="showSearch"
        class="filter-form"
        @submit.prevent
      >
        <el-form-item :label="$t('common.zone')" prop="defenceareaId">
          <el-select
            v-model="queryParams.defenceareaId"
            clearable
            class="filter-select"
            @change="handleDefenceareaChange"
          >
            <el-option
              v-for="dict in defenceareaOptions"
              :key="dict.id"
              :label="dict.name"
              :value="dict.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('common.camera')" prop="cameraId">
          <el-select
            v-model="queryParams.cameraId"
            clearable
            class="filter-select"
            @change="cameraChange"
          >
            <el-option
              v-for="dict in cameraOptions"
              :key="dict.id"
              :label="dict.ip"
              :value="dict.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item :label="$t('radar.ip')" prop="radarIp">
          <el-select
            v-model="queryParams.radarIp"
            clearable
            class="filter-select"
            @change="handleRadarChange"
          >
            <el-option
              v-for="dict in radarOptions"
              :key="dict.ip"
              :label="dict.ip"
              :value="dict.ip"
            />
          </el-select>
        </el-form-item>
        <el-form-item class="filter-actions">
          <el-button plain @click="handleQuery">{{ $t('common.search') }}</el-button>
          <el-button type="primary" @click="submit">{{ $t('common.save') }}</el-button>
          <el-button plain @click="openUrl">{{ $t('common.preview') }}</el-button>
        </el-form-item>
      </el-form>
    </section>

    <el-row :gutter="20" class="calibration-layout">
      <el-col :xl="15" :lg="14" :md="24" :sm="24" :xs="24" class="left-column">
        <section class="panel-card canvas-panel">
          <div class="div-canvas calibration-canvas">
            <span class="canvas-corner canvas-corner--tl"></span>
            <span class="canvas-corner canvas-corner--tr"></span>
            <span class="canvas-corner canvas-corner--bl"></span>
            <span class="canvas-corner canvas-corner--br"></span>
            <canvas id="imgCanvas" ref="canvaxbox"></canvas>
            <canvas id="pointCanvas" ref="pointCanvas" class="pointCanvas"></canvas>
            <canvas id="radarCanvas" ref="radarCanvas"></canvas>
          </div>
        </section>
      </el-col>

      <el-col :xl="9" :lg="10" :md="24" :sm="24" :xs="24" class="right-column">
        <section class="panel-card info-panel">
          <div class="panel-head panel-head--action">
            <div>
              <div class="con-title">{{ $t('common.camera') }}</div>
            </div>
            <el-button class="tip-w tip-w--inline" @click="stopTarget()">{{
              buttonText
            }}</el-button>
          </div>

          <div class="camera-meta">
            <div class="meta-card">
              <div class="meta-label">{{ $t('camera.id') }}</div>
              <div class="meta-value">{{ form.id || '--' }}</div>
            </div>
            <div class="meta-card">
              <div class="meta-label">{{ $t('camera.ip') }}</div>
              <div class="meta-value">{{ form.ip || '--' }}</div>
            </div>
          </div>

          <div class="inform-con calibration-side-body">
            <el-form
              label-width="140"
              ref="formRef"
              :model="form"
              :rules="rules"
              class="camera-form"
            >
              <div class="form-section-block">
                <div class="form-section-title">{{ $t('calibration.basicParameters') }}</div>
                <el-row :gutter="16">
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('camera.minViewAngle')">
                      <el-input v-model="form.minViewAngle" placeholder="" class="field-input" />
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('camera.maxViewAngle')">
                      <el-input v-model="form.maxViewAngle" placeholder="" class="field-input" />
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('camera.viewAngle2X')">
                      <el-input v-model="form.viewAngle2X" placeholder="" class="field-input" />
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('camera.cameraHeight')">
                      <el-input v-model="form.cameraHeight" placeholder="" class="field-input" />
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('camera.maxZoomRatio')">
                      <el-input v-model="form.maxZoomRatio" placeholder="" class="field-input" />
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('camera.maxZoom')">
                      <el-input v-model="form.maxZoom" placeholder="" class="field-input" />
                    </el-form-item>
                  </el-col>
                </el-row>
              </div>

              <div class="form-section-block form-section-block--accent">
                <div class="form-section-title">
                  {{ $t('calibration.gimbalCalibrationParameters') }}
                </div>
                <el-row :gutter="16">
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.p_Left_mid')">
                      <div class="pair-inputs">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.panLeft"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.panMiddle"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip
                            class="box-item"
                            content="L = Left;M = Middle/Mid"
                            placement="top"
                          >
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.p_Left_mid_angle')">
                      <div class="pair-inputs">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.panLeftAngle"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.panMiddleAngle"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip
                            class="box-item"
                            content="L = Left;M = Middle/Mid;Ang = Angle;"
                            placement="top"
                          >
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.p_Mid_right')">
                      <div class="pair-inputs">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.panMiddle2"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.panRight"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip
                            class="box-item"
                            content="M = Middle/Mid;R = Right;"
                            placement="top"
                          >
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.p_Mid_right_angle')">
                      <div class="pair-inputs">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.panMiddle2Angle"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.panRightAngle"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip
                            class="box-item"
                            content="M = Middle/Mid;R = Right;Ang = Angle"
                            placement="top"
                          >
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.t_Top_bottom')">
                      <div class="pair-inputs">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.tiltTop"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.tiltBottom"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip
                            class="box-item"
                            content="T = Top;B = Bottom;"
                            placement="top"
                          >
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.t_Top_bottom_angle')">
                      <div class="pair-inputs">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.tiltTopAngle"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.tiltBottomAngle"
                            placeholder=""
                            class="field-input field-input--pair"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip
                            class="box-item"
                            content="T = Top;B = Bottom;Ang = Angle"
                            placement="top"
                          >
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                </el-row>

                <el-row :gutter="16">
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.minZoomPT')">
                      <div class="pair-inputs pair-inputs--zoom">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.minZoomPan"
                            placeholder=""
                            class="field-input field-input--pair field-input--pair-wide"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.minZoomTilt"
                            placeholder=""
                            class="field-input field-input--pair field-input--pair-wide"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip class="box-item" content="Pan-Tilt;" placement="top">
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                          <el-button
                            class="no-border-btn inline-icon-btn"
                            @click="submitMinPT"
                            icon="Search"
                          ></el-button>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                  <el-col :xs="24" :sm="12">
                    <el-form-item :label="$t('calibration.maxZoomPT')">
                      <div class="pair-inputs pair-inputs--zoom">
                        <div class="pair-field-group">
                          <el-input
                            v-model="form.maxZoomPan"
                            placeholder=""
                            class="field-input field-input--pair field-input--pair-wide"
                          />
                          <span class="pair-arrow">→</span>
                          <el-input
                            v-model="form.maxZoomTilt"
                            placeholder=""
                            class="field-input field-input--pair field-input--pair-wide"
                          />
                        </div>
                        <div class="pair-actions">
                          <el-tooltip class="box-item" content="Pan-Tilt;" placement="top">
                            <el-button
                              class="no-border-btn inline-icon-btn"
                              icon="InfoFilled"
                            ></el-button>
                          </el-tooltip>
                          <el-button
                            class="no-border-btn inline-icon-btn"
                            @click="submitMaxPT"
                            icon="Search"
                          ></el-button>
                        </div>
                      </div>
                    </el-form-item>
                  </el-col>
                </el-row>
              </div>
            </el-form>
          </div>
        </section>

        <section class="panel-card ptz-panel">
          <div class="con-title">PTZ</div>
          <div class="ptz-panel-body">
            <el-row :gutter="14" class="ptz-layout">
              <el-col :xs="24" :sm="11">
                <div class="ptz-card ptz-card--control">
                  <PTZ @childEvent1="handleChildBegin" @childEvent2="handleChildStop" />
                  <el-slider v-model="speed" class="ptz-speed" step="0.1" max="1" />
                </div>
              </el-col>
              <el-col :xs="24" :sm="13">
                <div class="ptz-card">
                  <el-form class="coordinate-form coordinate-form--compact" label-position="top">
                    <el-row :gutter="10">
                      <el-col :xs="12" :sm="12">
                        <el-form-item :label="$t('common.cameraX')">
                          <el-input-number
                            v-model="cameraPointX"
                            :precision="2"
                            :step="0.5"
                            :min="-100"
                            :max="100"
                            class="coordinate-input"
                            @change="btnCamerarPointAngle"
                          />
                        </el-form-item>
                      </el-col>
                      <el-col :xs="12" :sm="12">
                        <el-form-item :label="$t('common.cameraY')">
                          <el-input-number
                            v-model="cameraPointY"
                            :precision="2"
                            :step="0.5"
                            :min="-100"
                            :max="100"
                            class="coordinate-input"
                            @change="btnCamerarPointAngle"
                          />
                        </el-form-item>
                      </el-col>
                      <el-col :xs="12" :sm="12">
                        <el-form-item :label="$t('common.cameraPoint')">
                          <el-input-number
                            v-model="camerarPointAngle"
                            :precision="2"
                            :step="0.01"
                            :min="-1"
                            :max="1"
                            class="coordinate-input"
                            @change="btnCamerarPointAngle"
                          />
                        </el-form-item>
                      </el-col>
                      <el-col :xs="12" :sm="12">
                        <el-form-item :label="$t('common.calibration')">
                          <div class="calibration-distance calibration-distance--compact">
                            <el-input v-model="jz" class="distance-input"></el-input>
                            <span class="unit-text">m</span>
                            <el-button
                              :disabled="isBtnDisabled"
                              size="small"
                              type="success"
                              class="action-btn"
                              @click="AddCalibration"
                            >
                              {{ $t('common.calibration') }}
                            </el-button>
                          </div>
                        </el-form-item>
                      </el-col>
                    </el-row>
                  </el-form>
                </div>
              </el-col>
            </el-row>
          </div>
        </section>
      </el-col>
    </el-row>

    <LocalPlayerWindow
      v-model="previewVisible"
      :title="$t('gis.cameraPreview')"
      :win-options="previewWinOptions"
      :initial-rect="previewRect"
      @closed="handlePreviewClosed"
    />
  </div>
</template>

<script setup>
import { getCurrentInstance, onBeforeUnmount, onMounted, reactive, ref, toRefs } from 'vue'

defineOptions({
  name: 'DeviceCalibration',
})

import { sendPTZ, sendStop } from '@/api/base/ptz'
import {
  addCalibration,
  getCalibrationBy,
  setCalibrationTrack,
  updateCalibration,
} from '@/api/device/calibration'
import {
  getCamera,
  getCameraByDefenceAreaId,
  getMaxZoomPTEdit,
  getMinZoomPTEdit,
  updateCamera,
} from '@/api/device/camera'
import { allDefencearea } from '@/api/device/defencearea.js'
import { listRadarByAreaId } from '@/api/device/radar'
import LocalPlayerWindow from '@/components/LocalPlayerWindow.vue'
import { initSignalR, setSignalRReceiveEnabled } from '@/utils/signalRUtils'
import PTZ from '@/views/components/PTZ'
import { ElMessage, ElMessageBox } from 'element-plus'

const { proxy } = getCurrentInstance()
const defenceareaOptions = ref([])
const cameraOptions = ref([])
const radarOptions = ref([])
const speed = ref(0.3)
const isWizard = ref(localStorage.getItem('wizard') === 'calibration')
const state = reactive({
  form: {},
  rules: {
    id: [
      {
        required: true,
        message: 'Id不能为空',
        trigger: 'blur',
        type: 'number',
      },
    ],
    ip: [{ required: true, message: 'IP不能为空', trigger: 'blur' }],
    port: [
      {
        required: true,
        message: '端口不能为空',
        trigger: 'blur',
        type: 'number',
      },
    ],
  },
})

const formRef = ref()
const { form, rules } = toRefs(state)

const formCC = ref()
const isBtnDisabled = ref(false)
const queryRef = ref()
const buttonText = ref(proxy.$t('camera.track_status1'))

const isTrack = ref()
const showSearch = ref(true)
const queryParams = reactive({
  defenceareaId: undefined,
  cameraId: undefined,
  cameraIp: '',
  radarIp: '',
})
const jz = ref('30')
const camerarPointAngle = ref(0)
const cameraPointX = ref(0)
const cameraPointY = ref(0)
const previewVisible = ref(false)
const previewRect = Object.freeze({
  left: 0,
  top: 0,
  width: 550,
  height: 360,
})
const previewWinOptions = ref({
  crossVisible: 0,
})
const activePreviewKey = ref('')

let canvas = ref(null)
let ctx = ref(null)

let imgCanvas = ref(null)
let imgCtx = ref(null)

let radarCanvas = ref(null)
let radarCtx = ref(null)

let connection = ref(null)
let unsubscribeSignalR = null
// 长链接数据接口
let longLinkApi = window.__APP_CONFIG__.VITE_SIGNALR_URL
// 长链接接受数据
let longLinkMsg = 'ReceiveTargetData'
let longLinkSendMsg = 'ReceiveTargetData'

onMounted(() => {
  canvas.value = document.getElementById('pointCanvas')
  ctx.value = canvas.value.getContext('2d')

  imgCanvas.value = document.getElementById('imgCanvas')
  imgCtx.value = imgCanvas.value.getContext('2d')

  radarCanvas.value = document.getElementById('radarCanvas')
  radarCtx.value = radarCanvas.value.getContext('2d')

  initPoints(longLinkApi, longLinkMsg, longLinkSendMsg)
  initImgCanvas()
  initRadar()
})
let AddCalibration = async function () {
  if (form.value.ip == undefined) {
    isBtnDisabled.value = false
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }
  isBtnDisabled.value = true
  formCC.value = {
    CameraIp: form.value.ip,
    DefenceareaId: queryParams.defenceareaId,
    CalibrationDistance: jz,
    CameraPointX: 0,
    CameraPointY: 0,
    CameraHeight: 2,
    CamerarPointAngle: 0,
  }

  await addCalibration(formCC.value).then((res) => {
    if (res.data.code !== 200) {
      proxy.$modal.msgError(proxy.$t('message.calibrationFailed'))
      return
    }
    ElMessageBox.alert(
      res.data.data + ' ' + proxy.$t('message.calibrationSuccess'),
      proxy.$t('common.calibration'),
      {
        type: 'success',
      },
    )
  })
  if (queryParams.cameraIp && queryParams.defenceareaId !== undefined) {
    getCalibration(queryParams.cameraIp, queryParams.defenceareaId)
  }
  isBtnDisabled.value = false
}

// 若处于向导模式，校准完成后跳转到 GIS（realtime_map）并结束向导
function maybeFinishWizardAfterCalibration() {
  if (localStorage.getItem('wizard') === 'calibration') {
    localStorage.removeItem('wizard')
    window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'realtime_map' }))
  }
}

// 在 AddCalibration 成功后调用以跳转
const originalAddCalibration = AddCalibration
AddCalibration = async function () {
  await originalAddCalibration()
  maybeFinishWizardAfterCalibration()
}

// ══════════ 向导控制 ══════════
function wizardFinish() {
  try { localStorage.removeItem('wizard') } catch (e) {}
  try { window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'realtime_map' })) } catch (e) {}
}
function wizardExit() {
  try { localStorage.removeItem('wizard') } catch (e) {}
  try { window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'realtime_map' })) } catch (e) {}
}

function stopTarget() {
  if (form.value.id == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }

  const currentTrack = Number(isTrack.value ?? form.value.isTrack ?? 0)
  const is_Track = currentTrack === 1 ? 0 : 1

  setCalibrationTrack(queryParams.cameraId, is_Track).then((res) => {
    const { status } = res
    if (status == 200) {
      isTrack.value = is_Track
      form.value.isTrack = is_Track
      if (is_Track == 1) buttonText.value = proxy.$t('camera.track_status0')
      else buttonText.value = proxy.$t('camera.track_status1')
    }
  })
}

function handleChildBegin(data) {
  if (form.value.ip == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
  }
  sendPTZ(data, form.value.ip, speed.value).then(() => {})
}
function handleChildStop() {
  sendStop(form.value.ip).then(() => {})
}
function handall() {
  allDefencearea().then((response) => {
    if (response.data.data.length == 0) {
      return
    }
    defenceareaOptions.value = response.data.data
    queryParams.defenceareaId = response.data.data[0].id
    handCamera()
  })
}

handall()

function handCamera() {
  queryParams.cameraId = ''
  cameraOptions.value = []
  radarOptions.value = []

  const defenceareaId = queryParams.defenceareaId
  getCameraByDefenceAreaId(defenceareaId).then((response) => {
    console.log('获取到的相机数据：', response.data.data)
    cameraOptions.value = response.data.data
    queryParams.cameraId = response.data.data[0].id
    queryParams.cameraIp = response.data.data[0].ip
    getList(queryParams.cameraId)
    getCalibration(queryParams.cameraIp, queryParams.defenceareaId)
  })

  // 加载该防区的雷达列表
  listRadarByAreaId(defenceareaId).then((response) => {
    radarOptions.value = response.data.data || []
    if (radarOptions.value.length > 0) {
      queryParams.radarIp = radarOptions.value[0].ip
    }
    console.log('获取到的雷达列表：', radarOptions.value)
  })
}

const handleDefenceareaChange = async (defenceareaId) => {
  queryParams.cameraId = ''
  cameraOptions.value = []
  radarOptions.value = []

  getCameraByDefenceAreaId(defenceareaId).then((response) => {
    console.log('获取到的相机数据：', response.data.data)
    cameraOptions.value = response.data.data
    queryParams.cameraId = response.data.data[0].id
    queryParams.cameraIp = response.data.data[0].ip
    getList(queryParams.cameraId)
    getCalibration(queryParams.cameraIp, queryParams.defenceareaId)
  })

  // 加载该防区的雷达列表
  listRadarByAreaId(defenceareaId).then((response) => {
    radarOptions.value = response.data.data || []
    if (radarOptions.value.length > 0) {
      queryParams.radarIp = radarOptions.value[0].ip
    }
    console.log('获取到的雷达列表：', radarOptions.value)
  })
}

function handleRadarChange(radarIp) {
  queryParams.radarIp = radarIp || ''
  // 切换雷达时清除现有点
  points = []
  console.log('[Calibration] 切换到雷达:', radarIp)
}
function cameraChange(cameraId) {
  getList(cameraId)
  getCalibration(queryParams.cameraIp, queryParams.defenceareaId)
}
function handleQuery() {
  if (form.value.id == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }
  isBtnDisabled.value = false
  getList(queryParams.cameraId)
  getCalibration(queryParams.cameraIp, queryParams.defenceareaId)
}

function getList(cameraId) {
  getCamera(cameraId).then((res) => {
    const { code, data } = res.data
    if (code == 200) {
      isTrack.value = Number(data.isTrack ?? 0)
      if (data.isTrack == 1) buttonText.value = proxy.$t('camera.track_status0')
      else buttonText.value = proxy.$t('camera.track_status1')

      form.value = {
        ...data,
      }
    }
  })
}

const getCalibration = (cameraIp, zoneId) => {
  getCalibrationBy(cameraIp, zoneId).then((res) => {
    const { code, data } = res.data
    if (code == 200) {
      camerarPointAngle.value = data.camerarPointAngle
      jz.value = data.calibrationDistance
      cameraPointX.value = data.cameraPointX
      cameraPointY.value = data.cameraPointY
    }
  })
}
function btnCamerarPointAngle() {
  if (form.value.id == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }

  const cameraIp = queryParams.cameraIp
  const defenceareaId = queryParams.defenceareaId
  const calibrationDistance = jz.value
  const cX = cameraPointX.value
  const cY = cameraPointY.value
  const cAngle = camerarPointAngle.value

  updateCalibration(cameraIp, defenceareaId, calibrationDistance, cX, cY, cAngle).then(() => {
    proxy.$modal.msgSuccess(proxy.$t('message.success'))
  })
}

async function openUrl() {
  if (form.value.id == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }

  if (!form.value.cameraURL) {
    ElMessage.error('当前相机未配置 RTSP 地址')
    return
  }

  const nextPreviewKey = [
    String(form.value.id ?? ''),
    form.value.ip || '',
    form.value.cameraURL || '',
    form.value.username || '',
  ]
    .filter(Boolean)
    .join('|')

  if (!nextPreviewKey) {
    return
  }

  if (previewVisible.value && activePreviewKey.value === nextPreviewKey) {
    previewVisible.value = false
    activePreviewKey.value = ''
    previewWinOptions.value = {
      ...previewWinOptions.value,
      crossVisible: 0,
    }
    return
  }

  previewWinOptions.value = {
    winId: `alarmVideo-${form.value.id}`,
    rtspUrl: form.value.cameraURL || '',
    username: form.value.username || '',
    password: form.value.password || '',
    deviceId: String(form.value.id ?? 0),
    channelId: Number(form.value.channelId ?? 0),
    crossVisible: 1,
  }
  activePreviewKey.value = nextPreviewKey
  previewVisible.value = true
}

function handlePreviewClosed() {
  previewVisible.value = false
  activePreviewKey.value = ''
  previewWinOptions.value = {
    ...previewWinOptions.value,
    crossVisible: 0,
  }
}

function initImgCanvas() {
  canvas.value.width = 1200
  canvas.value.height = 1200

  imgCanvas.value.width = 1200
  imgCanvas.value.height = 1200

  radarCanvas.value.width = 1200
  radarCanvas.value.height = 1200

  let img = document.createElement('img')
  img.src = 'map.jpg'
  img.onload = () => {
    imgCtx.value.drawImage(img, 0, 0, 1200, 1200)
  }
}
function initRadar() {
  radarCtx.value.save()
  radarCtx.value.beginPath()
  radarCtx.value.moveTo(500, 700)
  radarCtx.value.arc(500, 700, (500 / 250) * 280, (225 * Math.PI) / 180, (315 * Math.PI) / 180)
  radarCtx.value.closePath()
  radarCtx.value.lineWidth = '2'
  radarCtx.value.fillStyle = 'rgba(200, 200, 9, 0.2)'
  radarCtx.value.strokeStyle = 'red'
  radarCtx.value.stroke()
  radarCtx.value.fill()
  radarCtx.value.restore()

  // 启动探测波纹动画
  initRadarRipple()
}

// ══════════ 探测波纹动画 ══════════
let rippleAnimationId = null
let activeRipples = []
const RIPPLE_COUNT = 3
const RIPPLE_INTERVAL = 2000 // 每2秒发射一条
const RIPPLE_LIFETIME = 6000 // 每条存活6秒，保持3条同时可见
const SECTOR_CX = 500
const SECTOR_CY = 700
const SECTOR_R = (500 / 250) * 280
const SECTOR_A1 = (225 * Math.PI) / 180
const SECTOR_A2 = (315 * Math.PI) / 180

function initRadarRipple() {
  const now = performance.now()
  activeRipples = []
  // 用过去时初始化，按正常间隔分布，立即显示 3 条处于不同进度的波纹
  for (let i = 0; i < RIPPLE_COUNT; i++) {
    activeRipples.push({ startTime: now - (RIPPLE_COUNT - 1 - i) * RIPPLE_INTERVAL })
  }
  // startTime: now-4000, now-2000, now → 对应 67%、33%、0% 进度

  let lastEmitTime = now

  function drawFrame(now) {
    // 发射新波纹（自然淘汰由 filter 中的 RIPPLE_LIFETIME 控制）
    if (now - lastEmitTime >= RIPPLE_INTERVAL) {
      activeRipples.push({ startTime: now })
      lastEmitTime = now
    }

    const canvas = radarCanvas.value
    const ctx = radarCtx.value
    ctx.clearRect(0, 0, canvas.width, canvas.height)

    // 扇形填充 + 描边
    ctx.save()
    ctx.beginPath()
    ctx.moveTo(SECTOR_CX, SECTOR_CY)
    ctx.arc(SECTOR_CX, SECTOR_CY, SECTOR_R, SECTOR_A1, SECTOR_A2)
    ctx.closePath()
    ctx.fillStyle = 'rgba(200, 200, 9, 0.2)'
    ctx.fill()
    ctx.lineWidth = 2
    ctx.strokeStyle = 'red'
    ctx.stroke()
    ctx.restore()

    // 探测波纹
    activeRipples = activeRipples.filter((r) => {
      const elapsed = now - r.startTime
      if (elapsed > RIPPLE_LIFETIME) return false
      const progress = elapsed / RIPPLE_LIFETIME
      const radius = progress * SECTOR_R
      if (radius < 1) return true // 太小不画，但保留
      const opacity = 1 - progress
      ctx.save()
      ctx.beginPath()
      ctx.arc(SECTOR_CX, SECTOR_CY, radius, SECTOR_A1, SECTOR_A2)
      ctx.strokeStyle = `rgba(36, 180, 3, ${opacity.toFixed(2)})`
      ctx.lineWidth = 3
      ctx.stroke()
      ctx.restore()
      return true
    })

    rippleAnimationId = requestAnimationFrame(drawFrame)
  }

  rippleAnimationId = requestAnimationFrame(drawFrame)
}

let points = []
const POINT_LIFETIME = 3000 // 每个点显示3秒
const TARGET_COLORS = {
  人: { fill: '#22c55e', stroke: '#16a34a' },  // 绿色
  车: { fill: '#3b82f6', stroke: '#2563eb' },  // 蓝色
  未知: { fill: '#6b7280', stroke: '#4b5563' }, // 灰色
}

function mapTargetType(typeNum) {
  if (typeNum === 0 || typeNum === 1) return '人'
  if (typeNum === 2 || typeNum === 3) return '车'
  return '未知'
}

function initPoints(longLinkApi, acceptMsg, longLinkSendMsg) {
  initSignalR({
    api: longLinkApi,
    acceptMsg,
    sendMsg: longLinkSendMsg,
    onAcceptMessage: (res) => {
      const serverData = JSON.parse(res)
      // 帧级批量：服务端发送的是数组
      const targets = Array.isArray(serverData) ? serverData : [serverData]

      targets.forEach((data) => {
        // 按雷达IP过滤
        if (queryParams.radarIp && data.radarIp && data.radarIp !== queryParams.radarIp) {
          return
        }

        const x = parseFloat(data.axesX) + 500
        const y = 700 - parseFloat(data.axesY)
        const typeName = mapTargetType(data.targetType)
        points.push({
          x,
          y,
          targetId: data.targetId,
          targetType: typeName,
          distance: data.distance ? parseFloat(data.distance).toFixed(1) : '--',
          azimuth: data.azimuthAngle ? parseFloat(data.azimuthAngle).toFixed(1) : '--',
          timestamp: Date.now(),
        })

        if (points.length > 50) points.shift()
      })
    },
  }).then(({ connection: sharedConnection, unsubscribe }) => {
    connection.value = sharedConnection
    unsubscribeSignalR = unsubscribe
    setSignalRReceiveEnabled(true)
  })

  animate()
}
// 兼容老浏览器的圆角矩形绘制
function roundRect(ctx, x, y, w, h, r) {
  ctx.beginPath()
  ctx.moveTo(x + r, y)
  ctx.lineTo(x + w - r, y)
  ctx.arcTo(x + w, y, x + w, y + r, r)
  ctx.lineTo(x + w, y + h - r)
  ctx.arcTo(x + w, y + h, x + w - r, y + h, r)
  ctx.lineTo(x + r, y + h)
  ctx.arcTo(x, y + h, x, y + h - r, r)
  ctx.lineTo(x, y + r)
  ctx.arcTo(x, y, x + r, y, r)
  ctx.closePath()
  ctx.fill()
}

function animate() {
  const ctx2d = ctx.value
  ctx2d.clearRect(0, 0, canvas.value.width, canvas.value.height)

  const now = Date.now()
  points = points.filter((point) => now - point.timestamp < POINT_LIFETIME)

  points.forEach((point) => {
    const elapsed = now - point.timestamp
    const alpha = Math.max(0.4, 1 - elapsed / POINT_LIFETIME)

    ctx2d.save()
    ctx2d.globalAlpha = alpha

    // 实心圆点
    ctx2d.beginPath()
    ctx2d.arc(point.x, point.y, 4, 0, 2 * Math.PI)
    ctx2d.fillStyle = '#ef4444'
    ctx2d.fill()
    ctx2d.strokeStyle = '#dc2626'
    ctx2d.lineWidth = 1
    ctx2d.stroke()

    // 信息标签: "ID:123 150m 45°"
    const label = `ID:${point.targetId} ${point.distance}m ${point.azimuth}°`
    const fontSize = 11
    ctx2d.font = `${fontSize}px monospace`
    const labelW = ctx2d.measureText(label).width + 10
    const labelH = 16
    // 标签放在目标点右上方，拉开距离
    const offsetX = 22
    const offsetY = -38
    const labelX = point.x + offsetX
    const labelY = point.y + offsetY

    // 指向线
    ctx2d.strokeStyle = 'rgba(255, 255, 255, 0.5)'
    ctx2d.lineWidth = 1
    ctx2d.setLineDash([3, 2])
    ctx2d.beginPath()
    ctx2d.moveTo(point.x, point.y)
    ctx2d.lineTo(labelX, labelY + labelH / 2)
    ctx2d.stroke()
    ctx2d.setLineDash([])

    // 标签背景（兼容老浏览器的圆角矩形）
    ctx2d.fillStyle = 'rgba(0, 0, 0, 0.75)'
    roundRect(ctx2d, labelX, labelY, labelW, labelH, 3)

    // 标签文字
    ctx2d.fillStyle = '#fff'
    ctx2d.textBaseline = 'middle'
    ctx2d.fillText(label, labelX + 5, labelY + labelH / 2)

    ctx2d.restore()
  })

  requestAnimationFrame(animate)
}
function submitMinPT() {
  if (form.value.id == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }
  getMinZoomPTEdit(form.value.id, form.value.ip).then(() => {
    getList(queryParams.cameraId)
  })
}
function submitMaxPT() {
  if (form.value.id == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }
  getMaxZoomPTEdit(form.value.id, form.value.ip).then(() => {
    getList(queryParams.cameraId)
  })
}
function submit() {
  if (form.value.id == undefined) {
    ElMessageBox.alert(proxy.$t('zone.select_camera'), proxy.$t('common.ptz_Control'), {
      type: 'error',
    })
    return
  }

  form.value.isTrack = isTrack.value
  updateCamera(form.value).then((response) => {
    if (response.data.code == 200) {
      ElMessageBox.alert('Modified successfully', 'save', { type: 'success' })
    }
  })
}

onBeforeUnmount(() => {
  if (typeof unsubscribeSignalR === 'function') {
    unsubscribeSignalR()
    unsubscribeSignalR = null
  }
  if (rippleAnimationId !== null) {
    cancelAnimationFrame(rippleAnimationId)
    rippleAnimationId = null
  }
})
</script>
<style lang="scss" scoped>
.calibration-page {
  padding: 12px 0 12px;
}

.toolbar-card,
.panel-card {
  background: #ffffff;
  border: 1px solid rgba(15, 23, 42, 0.06);
  border-radius: 16px;
  box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
}

.toolbar-card {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 16px;
  padding: 8px 16px 0;
  margin-bottom: 16px;
  flex-wrap: wrap;
}

.toolbar-title {
  min-width: 220px;
}

.filter-form {
  flex: 1;
  display: flex;
  justify-content: flex-end;
  align-items: center;
  flex-wrap: wrap;
}

.filter-select {
  width: 180px;
}

.filter-actions {
  margin-left: auto;
}

.toolbar-card :deep(.el-form-item) {
  margin-bottom: 8px;
}

.toolbar-card :deep(.el-form-item__label) {
  line-height: 30px;
}

.toolbar-card :deep(.el-input__wrapper),
.toolbar-card :deep(.el-select__wrapper) {
  min-height: 30px;
}

.toolbar-card :deep(.el-button) {
  min-height: 30px;
  padding-top: 6px;
  padding-bottom: 6px;
}

.calibration-layout {
  align-items: stretch;
}

.left-column,
.right-column {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.right-column {
  position: relative;
}

.panel-card {
  padding: 16px 18px 18px;
  height: 100%;
}

.info-panel {
  height: auto;
  flex: 0 0 auto;
  align-self: stretch;
  padding-bottom: 10px;
}

.ptz-panel {
  height: auto;
  flex: 0 0 auto;
  align-self: stretch;
  padding: 12px 14px 14px;
}

.panel-head {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 10px;
}

.panel-head--action {
  align-items: center;
  min-height: auto;
}

.canvas-panel .panel-head {
  align-items: center;
  margin-bottom: 8px;
}

.canvas-panel :deep(.con-title) {
  padding-top: 4px;
  padding-bottom: 4px;
  padding-left: 28px;
  line-height: 1.2;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
}

.calibration-canvas {
  min-height: 520px;
  height: calc(100%);
  border: 1px solid rgba(230, 162, 60, 0.35);
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.6);
  background-image:
    linear-gradient(rgba(255, 255, 255, 0.16) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.16) 1px, transparent 1px);
  background-size: 36px 36px;
}

.canvas-corner {
  position: absolute;
  width: 24px;
  height: 24px;
  z-index: 2;
  border-color: rgba(230, 162, 60, 0.9);
}

.canvas-corner--tl {
  top: 12px;
  left: 12px;
  border-top: 2px solid;
  border-left: 2px solid;
}

.canvas-corner--tr {
  top: 12px;
  right: 12px;
  border-top: 2px solid;
  border-right: 2px solid;
}

.canvas-corner--bl {
  bottom: 56px;
  left: 12px;
  border-bottom: 2px solid;
  border-left: 2px solid;
}

.canvas-corner--br {
  bottom: 56px;
  right: 12px;
  border-bottom: 2px solid;
  border-right: 2px solid;
}

.tip-w--inline {
  position: static;
  right: auto;
  top: auto;
  margin-left: 12px;
  flex-shrink: 0;
  min-height: 26px;
  padding: 4px 10px;
}

.camera-meta {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
  margin-bottom: 12px;
}

.info-panel,
.ptz-panel {
  background: #f8fafc;
  border-color: rgba(15, 23, 42, 0.08);
  box-shadow: 0 6px 18px rgba(15, 23, 42, 0.06);
}

.info-panel .panel-head,
.ptz-panel .con-title {
  margin-bottom: 12px;
}

.info-panel :deep(.con-title),
.ptz-panel :deep(.con-title) {
  padding-top: 4px;
  padding-bottom: 4px;
  padding-left: 28px;
  line-height: 1.2;
}

.ptz-panel .con-title {
  margin-bottom: 6px;
}

.info-panel .panel-head {
  padding-bottom: 6px;
  border-bottom: 1px solid rgba(15, 23, 42, 0.08);
}

.meta-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 12px;
  border: 1px solid rgba(15, 23, 42, 0.06);
  background: linear-gradient(180deg, #f8fafc 0%, #ffffff 100%);
}

.meta-label {
  font-size: 12px;
  color: #909399;
  flex-shrink: 0;
}

.meta-value {
  font-size: 14px;
  font-weight: 600;
  color: #303133;
  text-align: right;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.calibration-side-body {
  padding: 0;
}

.camera-form {
  padding: 0 2px;
}

.form-section-block {
  padding: 10px 10px 2px;
  margin-bottom: 10px;
  border-radius: 12px;
  border: 1px solid rgba(15, 23, 42, 0.06);
  background: #ffffff;
}

.camera-form > .form-section-block:last-child {
  margin-bottom: 0;
}

.form-section-block--accent {
  background: linear-gradient(180deg, rgba(36, 180, 3, 0.03) 0%, #ffffff 100%);
}

.form-section-title {
  margin: 0 0 8px;
  padding-left: 10px;
  border-left: 2px solid rgba(36, 180, 3, 0.75);
  font-size: 12px;
  font-weight: 700;
  color: #606266;
  letter-spacing: 0.02em;
}

.field-input {
  width: 100%;
}

.field-input--pair {
  width: auto;
  min-width: 0;
  flex: 1 1 0;
}

.field-input--pair-wide {
  min-width: 51px;
}

.pair-inputs {
  display: flex;
  width: 100%;
  align-items: center;
  gap: 6px;
  flex-wrap: nowrap;
}

.pair-field-group {
  display: flex;
  align-items: center;
  gap: 6px;
  flex: 1;
  min-width: 0;
}

.pair-actions {
  display: flex;
  align-items: center;
  gap: 2px;
  flex-shrink: 0;
}

.pair-inputs--zoom .pair-actions {
  margin-left: 2px;
}

.pair-arrow {
  color: #909399;
  font-size: 12px;
}

.inline-icon-btn {
  margin-left: 0;
  width: 24px;
  min-width: 24px;
  padding: 0;
  flex-shrink: 0;
}

.pair-inputs :deep(.el-tooltip__trigger) {
  display: inline-flex;
  flex-shrink: 0;
}

.ptz-panel-body {
  padding-top: 0;
}

.ptz-layout {
  align-items: stretch;
}

.ptz-panel :deep(.el-row) {
  --el-row-gutter: 10px !important;
}

.ptz-card {
  height: 100%;
  padding: 8px 10px;
  border-radius: 12px;
  border: 1px solid rgba(15, 23, 42, 0.08);
  background: #ffffff;
}

.ptz-card--control {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;
}

.ptz-speed {
  width: 100%;
  max-width: 190px;
}

.coordinate-input {
  width: 100%;
}

.calibration-distance {
  display: flex;
  align-items: center;
  gap: 4px;
  flex-wrap: wrap;
}

.distance-input {
  width: 72px;
}

.unit-text {
  color: #909399;
}

.action-btn {
  min-width: 88px;
}

.coordinate-form--compact {
  padding-top: 0;
}

.calibration-distance--compact {
  flex-wrap: nowrap;
  align-items: center;
}

.filter-form :deep(.el-form-item),
.camera-form :deep(.el-form-item),
.coordinate-form :deep(.el-form-item) {
  margin-bottom: 10px;
}

.filter-form :deep(.el-form-item__label),
.camera-form :deep(.el-form-item__label),
.coordinate-form :deep(.el-form-item__label) {
  font-weight: 600;
  color: #606266;
}

.camera-form :deep(.el-form-item__label) {
  padding-right: 8px;
}

.camera-form :deep(.el-form-item__content) {
  min-height: 34px;
}

.camera-form :deep(.el-input__wrapper),
.coordinate-form :deep(.el-input__wrapper),
.distance-input :deep(.el-input__wrapper) {
  padding-top: 1px;
  padding-bottom: 1px;
  box-shadow: 0 0 0 1px rgba(15, 23, 42, 0.08) inset;
}

.coordinate-form :deep(.el-form-item__label) {
  width: 84px !important;
  padding-right: 6px;
}

.coordinate-form :deep(.el-form-item__content) {
  min-height: 30px;
  margin-left: 84px !important;
}

.coordinate-form :deep(.el-input-number),
.coordinate-form :deep(.el-input),
.distance-input :deep(.el-input) {
  width: 100%;
}

.coordinate-form--compact :deep(.el-form-item__label) {
  width: auto !important;
  padding-right: 0;
  margin-bottom: 4px;
  line-height: 1.2;
  font-size: 12px;
}

.coordinate-form--compact :deep(.el-form-item__content) {
  margin-left: 0 !important;
  min-height: 28px;
}

.ptz-panel :deep(.con-title) {
  padding-top: 2px;
  padding-bottom: 2px;
}

.ptz-panel :deep(.el-slider) {
  margin: 0;
}

.coordinate-form :deep(.el-form-item) {
  margin-bottom: 8px;
}

.coordinate-form--compact :deep(.el-row) {
  row-gap: 2px;
}

.coordinate-form :deep(.el-input-number .el-input__wrapper),
.coordinate-form :deep(.el-input .el-input__wrapper),
.distance-input :deep(.el-input__wrapper) {
  min-height: 30px;
}

.coordinate-form--compact :deep(.el-input-number .el-input__wrapper),
.coordinate-form--compact :deep(.el-input .el-input__wrapper),
.coordinate-form--compact .distance-input :deep(.el-input__wrapper) {
  min-height: 28px;
}

.ptz-card--control :deep(.ptz-con) {
  padding: 0;
}

.ptz-card--control :deep(.ptz-row) {
  justify-content: center;
  gap: 6px;
  margin-bottom: 6px;
}

.ptz-card--control :deep(.ptz-row:last-child) {
  margin-bottom: 0;
}

.ptz-card--control :deep(.ptz-btn) {
  width: 32px;
  height: 32px;
  padding: 0;
}

.ptz-card--control :deep(.ptz-btn i) {
  font-size: 15px;
}

.ptz-card--control :deep(.ptz-btn-Zoom) {
  margin: 2px 6px 0;
}

.ptz-card--control :deep(.ptz-btn-Zoom i) {
  font-size: 18px;
}

.ptz-card--control :deep(.ptz-row:last-child > div) {
  width: 72px !important;
}

@media (max-width: 1440px) {
  .camera-form {
    padding: 0;
  }

  .pair-inputs {
    gap: 6px;
  }
}

@media (max-width: 992px) {
  .toolbar-card {
    padding-bottom: 10px;
  }

  .filter-form {
    justify-content: flex-start;
  }

  .tip-w--inline {
    margin-left: 0;
  }

  .calibration-canvas {
    min-height: 420px;
    height: 60vh;
  }
}

@media (max-width: 768px) {
  .panel-head,
  .panel-head--action {
    flex-direction: column;
    align-items: flex-start;
  }

  .camera-meta {
    grid-template-columns: 1fr;
  }

  .pair-inputs {
    flex-wrap: wrap;
  }

  .pair-field-group {
    width: 100%;
  }

  .pair-actions {
    width: 100%;
    justify-content: flex-end;
  }

  .field-input--pair {
    width: 100%;
  }

  .filter-select,
  .distance-input {
    width: 100%;
  }

  .calibration-distance--compact {
    flex-wrap: wrap;
  }
}

/* ══════════ 向导步骤条 ══════════ */
.wizard-banner {
  max-width: 900px;
  margin: 0 auto 16px;
  padding: 20px 24px 14px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.04);
}
.wizard-banner-desc {
  margin: 12px 0 0;
  text-align: center;
  font-size: 14px;
  color: #909399;
}
.wizard-banner-actions {
  display: flex;
  justify-content: center;
  gap: 12px;
  margin-top: 12px;
}
</style>

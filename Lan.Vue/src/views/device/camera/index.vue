<template>
  <div>
    <el-form
      :model="queryParams"
      label-position="right"
      inline
      ref="queryRef"
      v-show="showSearch"
      @submit.prevent
      style="text-align: left"
    >
      <el-form-item :label="$t('camera.ip')" prop="ip">
        <el-input
          v-model="queryParams.ip"
          clearable
          style="width: 240px"
          @keyup.enter="handleQuery"
        />
      </el-form-item>
      <el-form-item>
        <el-button icon="search" type="primary" @click="handleQuery">{{
          $t('common.search')
        }}</el-button>
      </el-form-item>
    </el-form>

    <el-row :gutter="10" class="mb8">
      <el-col :span="1.5">
        <el-button type="primary" @click="handleAdd" v-hasPermi="['system:post:add']">{{
          $t('common.add')
        }}</el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="success" :disabled="single" @click="handleUpdate">
          {{ $t('common.edit') }}
        </el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="danger" :disabled="multiple" @click="handleDelete">
          {{ $t('common.delete') }}
        </el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="success" @click="handleSecrch">
          {{ $t('common.probeUnicast') }}
        </el-button>
      </el-col>
    </el-row>

    <el-table
      v-loading="loading"
      :data="dataList"
      border
      stripe
      header-cell-class-name="el-table-header-cell"
      highlight-current-row
      @selection-change="handleSelectionChange"
    >
      <el-table-column type="selection" :selectable="selectable" width="55" />
      <el-table-column prop="id" :label="$t('camera.id')" align="center" width="120" />
      <el-table-column
        prop="name"
        :label="$t('camera.name')"
        align="center"
        :show-overflow-tooltip="true"
        width="150"
      />
      <!-- <el-table-column prop="name" label="绑定防区" align="center"  /> -->
      <el-table-column
        prop="ip"
        :label="$t('camera.ip')"
        width="130"
        align="center"
        :show-overflow-tooltip="true"
      >
        <template #default="scope">
          <el-button
            class="camera-ip-link"
            link
            type="primary"
            @click.stop="openLocalPlayerPreview(scope.row)"
          >
            {{ scope.row.ip }}
          </el-button>
        </template>
      </el-table-column>
      <el-table-column
        prop="username"
        :label="$t('camera.username')"
        align="center"
        :show-overflow-tooltip="true"
        width="120"
      />
      <el-table-column
        prop="password"
        :label="$t('camera.password')"
        align="center"
        :show-overflow-tooltip="true"
        width="120"
      />
      <el-table-column :label="$t('common.online_offline')" width="140" align="center">
        <template #default="scope">
          <div class="status-cell">
            <span class="pulse-dot" :class="scope.row.online ? 'online' : 'offline'"></span>
            <span :class="scope.row.online ? 'online-text' : 'offline-text'">
              {{ scope.row.online ? $t('common.online') : $t('common.offline') }}
            </span>
          </div>
        </template>
      </el-table-column>

      <el-table-column
        prop="manufacturer"
        :label="$t('camera.manufacturer')"
        align="center"
        :show-overflow-tooltip="true"
        width="180"
      />
      <el-table-column
        prop="deviceTypeName"
        :label="$t('camera.deviceTypeName')"
        align="center"
        :show-overflow-tooltip="true"
        width="200"
      />

      <el-table-column
        prop="cameraHeight"
        :label="$t('camera.cameraHeight')"
        align="center"
        :show-overflow-tooltip="true"
        width="120"
      />
      <el-table-column
        prop="cameraURL"
        :label="$t('camera.cameraURL')"
        align="center"
        :show-overflow-tooltip="true"
        width="240"
      />
      <el-table-column :label="$t('camera.isTrack')" align="center" prop="isTrack" width="120">
        <template #default="scope">
          <el-tag :type="scope.row.isTrack == 1 ? 'success' : 'danger'">
            {{ scope.row.isTrack == 1 ? $t('camera.track_status1') : $t('camera.track_status0') }}
          </el-tag>
        </template>
      </el-table-column>

      <el-table-column :label="$t('radar.actions')" align="center">
        <template #default="scope">
          <el-button
            type="success"
            size="small"
            icon="edit"
            :title="$t('common.edit')"
            v-hasPermi="['defencearea:edit']"
            @click="handleUpdate(scope.row)"
          ></el-button>
          <el-button
            type="danger"
            size="small"
            icon="delete"
            :title="$t('common.delete')"
            v-hasPermi="['defencearea:delete']"
            @click="handleDelete(scope.row)"
          ></el-button>
        </template>
      </el-table-column>
    </el-table>

    <LocalPlayerWindow
      v-model="previewVisible"
      :title="$t('gis.cameraPreview')"
      :win-options="previewWinOptions"
      :initial-rect="previewRect"
      @closed="handlePreviewClosed"
    />

    <!-- 添加或修改对话框 -->
    <el-dialog
      :title="title"
      :lock-scroll="false"
      v-model="open"
      width="1200px"
      :close-on-click-modal="false"
    >
      <el-form ref="formRef" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="20">
          <el-col :lg="6">
            <el-form-item :label="$t('camera.id')" prop="id">
              <el-input-number
                v-model.number="form.id"
                controls-position="right"
                :disabled="true"
              />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.name')" prop="name">
              <el-input v-model="form.name" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.sys_dict_data_size')" prop="sys_dict_data_size">
              <el-select
                v-model="form.sys_dict_data_size"
                clearable
                style="width: 240px"
                @change="handleselect"
              >
                <el-option
                  v-for="dict in cameramfrOptions"
                  :key="dict.dictValue"
                  :label="dict.dictLabel"
                  :value="dict.dictValue"
                />
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :lg="6"> </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.ip')" prop="ip">
              <el-input v-model="form.ip" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.port')" prop="port">
              <el-input v-model.number="form.port" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.username')" prop="username">
              <el-input v-model="form.username" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.password')" prop="password">
              <el-input v-model="form.password" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item
              :label="$t('camera.cameraHeight')"
              prop="cameraHeight"
              class="custom-red-form"
            >
              <el-input v-model="form.cameraHeight" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.minViewAngle')" prop="minViewAngle">
              <el-input v-model="form.minViewAngle" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.viewAngle2X')" prop="viewAngle2X">
              <el-input v-model="form.viewAngle2X" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.maxViewAngle')" prop="maxViewAngle">
              <el-input v-model="form.maxViewAngle" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.maxZoom')" prop="maxZoom">
              <el-input v-model.number="form.maxZoom" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.maxZoomRatio')" prop="maxZoomRatio">
              <el-input v-model="form.maxZoomRatio" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.counterclockwise')">
              <el-radio-group v-model="form.counterclockwise">
                <el-radio
                  v-for="dict in clockwise_status"
                  :key="dict.dictValue"
                  :label="parseInt(dict.dictValue)"
                >
                  {{
                    dict.dictValue === '1'
                      ? $t('camera.counterclockwise1')
                      : $t('camera.counterclockwise0')
                  }}
                </el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.isTrack')">
              <el-radio-group v-model="form.isTrack">
                <el-radio
                  v-for="dict in isTrackOptions"
                  :key="dict.dictValue"
                  :label="parseInt(dict.dictValue)"
                >
                  {{
                    dict.dictValue === '1' ? $t('camera.track_status1') : $t('camera.track_status0')
                  }}
                </el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.panLeft')" prop="panLeft">
              <el-input v-model="form.panLeft" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.panMiddle')" prop="panMiddle">
              <el-input v-model="form.panMiddle" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.panLeftAngle')" prop="panLeftAngle">
              <el-input v-model="form.panLeftAngle" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.panMiddleAngle')" prop="panMiddleAngle">
              <el-input v-model="form.panMiddleAngle" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.panMiddle2')" prop="panMiddle2">
              <el-input v-model="form.panMiddle2" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.panRight')" prop="panRight">
              <el-input v-model="form.panRight" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.panMiddle2Angle')" prop="panMiddle2Angle">
              <el-input v-model="form.panMiddle2Angle" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.panRightAngle')" prop="panRightAngle">
              <el-input v-model="form.panRightAngle" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.tiltTop')" prop="tiltTop">
              <el-input v-model="form.tiltTop" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.tiltBottom')" prop="tiltBottom">
              <el-input v-model="form.tiltBottom" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.tiltTopAngle')" prop="tiltTopAngle">
              <el-input v-model="form.tiltTopAngle" />
            </el-form-item>
          </el-col>
          <el-col :lg="6">
            <el-form-item :label="$t('camera.tiltBottomAngle')" prop="tiltBottomAngle">
              <el-input v-model="form.tiltBottomAngle" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.minZoomPan')" prop="minZoomPan">
              <el-input v-model="form.minZoomPan" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.minZoomTilt')" prop="minZoomTilt">
              <el-input v-model="form.minZoomTilt" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.maxZoomPan')" prop="maxZoomPan">
              <el-input v-model="form.maxZoomPan" />
            </el-form-item>
          </el-col>

          <el-col :lg="6">
            <el-form-item :label="$t('camera.maxZoomTilt')" prop="maxZoomTilt">
              <el-input v-model="form.maxZoomTilt" />
            </el-form-item>
          </el-col>

          <el-col :lg="24">
            <el-form-item :label="$t('camera.cameraURL')" prop="cameraURL">
              <el-input v-model="form.cameraURL" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer v-if="opertype != 3">
        <el-button text @click="cancel">{{ $t('common.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm">{{ $t('common.save') }}</el-button>
      </template>
    </el-dialog>
    <!-- 搜索弹窗（独立） -->
    <el-dialog
      :title="searchTitle"
      v-model="searchOpen"
      width="400px"
      :close-on-click-modal="false"
    >
      <el-form :model="searchForm" label-width="100px">
        <el-row :gutter="24">
          <el-col :lg="20">
            <el-form-item :label="$t('camera.ip')">
              <el-input v-model="searchForm.ip" clearable />
            </el-form-item>

            <el-form-item :label="$t('camera.port')">
              <el-input v-model.number="searchForm.port" />
            </el-form-item>
            <el-form-item :label="$t('camera.username')">
              <el-input v-model="searchForm.username" clearable />
            </el-form-item>

            <el-form-item :label="$t('camera.password')">
              <el-input v-model="searchForm.password" clearable />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer>
        <el-button text @click="searchOpen = false">{{ $t('common.cancel') }}</el-button>
        <el-button type="primary" @click="applySearch">{{ $t('common.confirm') }}</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script>
import { ref } from 'vue'

import { getTrackParameter } from '@/api/base/trackparameter'
import {
  addCamera,
  delCamera,
  getCamera,
  listCamera,
  probeUnicast,
  updateCamera,
} from '@/api/device/camera'
import LocalPlayerWindow from '@/components/LocalPlayerWindow.vue'

export default {
  name: 'camera',
  components: {
    LocalPlayerWindow,
  },
  data() {
    return {
      // 遮罩层
      loading: true,
      // 选中数组
      ids: [],
      // 非单个禁用
      single: true,
      // 非多个禁用
      multiple: true,
      // 显示搜索条件
      showSearch: true,
      // 总条数
      total: 0,
      // 表格数据
      dataList: [],
      // 弹出层标题
      title: '',
      // 是否显示弹出层
      open: false,
      // 状态数据字典
      statusOptions: [],
      // 查询参数
      queryParams: {
        pageNum: 1,
        pageSize: 10,
        sort: 'id',
        sortType: 'asc',
        ip: undefined,
      },
      // 表单参数
      form: {},
      // 搜索弹窗控制
      searchOpen: false,
      searchTitle: '',
      previewVisible: false,
      previewRect: Object.freeze({
        left: 0,
        top: 0,
        width: 550,
        height: 360,
      }),
      previewWinOptions: {},
      activePreviewKey: '',
      // 搜索表单（独立弹窗使用）
      searchForm: {
        ip: '',
        username: 'admin',
        password: 'rvssp100',
        port: 80,
      },
      // 表单校验
      rules: {
        name: [
          {
            required: true,
            message: this.$t('validation.name'),
            trigger: 'blur',
          },
        ],
        ip: [
          {
            required: true,
            pattern:
              /^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/,
            message: this.$t('validation.ip'),
            trigger: 'blur',
          },
        ],
        port: [
          {
            required: true,
            message: this.$t('validation.port'),
            trigger: 'blur',
          },
        ],
        username: [
          {
            required: true,
            message: this.$t('validation.username_null'),
            trigger: 'blur',
          },
          {
            pattern: /^\S*$/,
            message: this.$t('validation.username'),
            trigger: 'blur',
          },
        ],
        password: [
          {
            required: true,
            message: this.$t('validation.password_null'),
            trigger: 'blur',
          },
          {
            pattern: /^\S*$/,
            message: this.$t('validation.password'),
            trigger: 'blur',
          },
        ],
      },

      cameramfrOptions: ref([]),
      clockwise_status: ref([]),
      isTrackOptions: ref([]),
    }
  },
  created() {
    this.getList()
    this.getDicts('device_status').then((response) => {
      this.statusOptions = response.data.data
    })

    this.getDicts('camera_mfr').then((response) => {
      this.cameramfrOptions = response.data.data
    })

    this.getDicts('clockwise_status').then((response) => {
      this.clockwise_status = response.data.data
    })

    this.getDicts('is_track').then((response) => {
      this.isTrackOptions = response.data.data
    })
    try {
      if (localStorage.getItem('wizard') === 'camera') {
        this.handleAdd()
      }
    } catch (e) {}
  },
  beforeRouteLeave(to, from, next) {
    this.closePreview()
    next()
  },
  beforeUnmount() {
    this.closePreview()
  },
  methods: {
    getList() {
      this.loading = true
      listCamera(this.queryParams).then((res) => {
        if (res.data.code == 200) {
          this.dataList = res.data.data
          //total.value = data.totalNum
          this.loading = false
        }
      })
    },
    mounted() {
      this.searchTitle = this.$t('camera.search')
    },
    cancel() {
      this.open = false
      this.reset()
    },
    // 表单重置
    reset() {
      this.form = {
        id: undefined,
        name: null,
        bindingAreaId: null,
        ip: null,
        port: 80,
        username: null,
        password: null,
        cameraHeight: 2,

        trackparid: null,
        trackMode: 1,
        minViewAngle: 0,
        viewAngle2X: 0,
        maxViewAngle: 0,
        maxZoom: 0,
        maxZoomRatio: 0,

        panLeft: '0',
        panMiddle: '0',
        panLeftAngle: '0',
        panMiddleAngle: '0',

        panMiddle2: '0',
        panRight: '0',
        panMiddle2Angle: '0',
        panRightAngle: '0',

        tiltTop: '0',
        tiltBottom: '0',
        tiltTopAngle: '0',
        tiltBottomAngle: '0',

        cameraURL: ' ',

        minZoomPan: '1',
        minZoomTilt: '-1',
        maxZoomPan: '-15.0',
        maxZoomTilt: '90',

        counterclockwise: 0,
        isTrack: 1,

        cameraId: 0,
        sourceToken: '',
      }
      this.resetForm('formRef')
    },
    handleQuery() {
      this.queryParams.pageNum = 1
      this.getList()
    },
    resetQuery() {
      this.resetForm('queryForm')
      this.handleQuery()
    },
    handleSelectionChange(selection) {
      this.ids = selection.map((item) => item.id)
      this.single = selection.length != 1
      this.multiple = !selection.length
    },
    buildPreviewWinId(previewKey, row) {
      if (row?.id !== undefined && row?.id !== null) {
        return `alarmVideo-${row.id}`
      }

      return `alarmVideo-${btoa(unescape(encodeURIComponent(previewKey))).replace(/=+$/g, '')}`
    },
    openLocalPlayerPreview(row) {
      const normalizedCameraIp = row?.ip || ''
      const normalizedCameraUrl = row?.cameraURL || ''
      const normalizedUserName = row?.username || ''
      const nextPreviewKey = [
        String(row?.id ?? ''),
        normalizedCameraIp,
        normalizedCameraUrl,
        normalizedUserName,
      ]
        .filter(Boolean)
        .join('|')

      if (!normalizedCameraUrl) {
        this.$modal.msgError('当前相机未配置 RTSP 地址')
        return
      }

      if (!nextPreviewKey) {
        return
      }

      if (this.previewVisible && this.activePreviewKey === nextPreviewKey) {
        this.closePreview()
        return
      }

      this.previewWinOptions = {
        winId: this.buildPreviewWinId(nextPreviewKey, row),
        rtspUrl: normalizedCameraUrl,
        username: normalizedUserName,
        password: row?.password || '',
        deviceId: String(row?.id ?? 0),
        channelId: Number(row?.channelId ?? 0),
      }
      this.activePreviewKey = nextPreviewKey
      this.previewVisible = true
    },
    closePreview() {
      this.previewVisible = false
      this.activePreviewKey = ''
      this.previewWinOptions = {}
    },
    handlePreviewClosed() {
      this.closePreview()
    },
    handleAdd() {
      this.reset()
      this.open = true
      this.title = this.$t('common.add')
      this._isWizard = localStorage.getItem('wizard') === 'camera'
    },
    handleUpdate(row) {
      this.reset()
      const Ids = row.id || this.ids
      getCamera(Ids).then((response) => {
        this.form = response.data.data
        this.open = true
        this.title = this.$t('common.edit')
      })
    },
    submitForm: function () {
      this.$refs['formRef'].validate((valid) => {
        if (valid) {
          if (this.form.id != undefined) {
            this.form.trackMode = 1
            updateCamera(this.form).then((response) => {
              this.$modal.msgSuccess(this.$t('message.editSuccess'))
              this.open = false
              this.getList()
            })
          } else {
            this.form.trackMode = 1
            this.form.bindingAreaId = -1

            addCamera(this.form).then((res) => {
              if (res.data.code == 200) {
                this.$modal.msgSuccess(this.$t('message.addSuccess'))
                this.open = false
                this.getList()
                if (this._isWizard) {
                  try {
                    localStorage.setItem('wizard', 'defencearea')
                  } catch (e) {}
                  try {
                    window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'defencearea' }))
                  } catch (e) {}
                }
              } else if (res.data.code == 102) {
                this.$modal.msgError(this.$t('message.cameraDATA_REPEAT'))
              } else if (res.data.code == 101) {
                this.$modal.msgError(this.$t('message.cameraPARAM_ERROR'))
              }
            })
          }
        }
      })
    },
    handleDelete(row) {
      const Ids = row.id || this.ids
      this.$confirm(this.$t('message.deleteConfirm', { id: Ids }), this.$t('common.warning'), {
        confirmButtonText: this.$t('common.confirm'),
        cancelButtonText: this.$t('common.cancel'),
        type: 'warning',
      })
        .then(function () {
          return delCamera(Ids)
        })
        .then(() => {
          this.getList()
          this.$modal.msgSuccess(this.$t('message.deleteSuccess'))
        })
    },
    handleselect() {
      getTrackParameter(this.form.sys_dict_data_size).then((response) => {
        this.form.trackparid = response.data.data.id
        this.form.trackMode = response.data.data.trackMode
        this.form.minViewAngle = response.data.data.minViewAngle
        this.form.viewAngle2X = response.data.data.viewAngle2X
        this.form.maxViewAngle = response.data.data.maxViewAngle
        this.form.maxZoom = response.data.data.maxZoom
        this.form.maxZoomRatio = response.data.data.maxZoomRatio

        this.form.panLeft = response.data.data.panLeft
        this.form.panMiddle = response.data.data.panMiddle
        this.form.panLeftAngle = response.data.data.panLeftAngle
        this.form.panMiddleAngle = response.data.data.panMiddleAngle

        this.form.panMiddle2 = response.data.data.panMiddle2
        this.form.panRight = response.data.data.panRight
        this.form.panMiddle2Angle = response.data.data.panMiddle2Angle
        this.form.panRightAngle = response.data.data.panRightAngle

        this.form.tiltTop = response.data.data.tiltTop
        this.form.tiltBottom = response.data.data.tiltBottom
        this.form.tiltTopAngle = response.data.data.tiltTopAngle
        this.form.tiltBottomAngle = response.data.data.tiltBottomAngle

        this.form.minZoomPan = response.data.data.minZoomPan
        this.form.minZoomTilt = response.data.data.minZoomTilt
        this.form.maxZoomPan = response.data.data.maxZoomPan
        this.form.maxZoomTilt = response.data.data.maxZoomTilt

        this.form.counterclockwise = response.data.data.counterclockwise
      })
    },
    handleSecrch() {
      this.searchOpen = true
      this.searchTitle = this.$t('common.search')
    },
    async applySearch() {
      // 将搜索表单的值写入查询参数并执行查询

      let form = {
        ip: this.searchForm.ip,
        port: this.searchForm.port,
        userName: this.searchForm.username,
        password: this.searchForm.password,
      }
      await probeUnicast(form).then((res) => {
        this.$modal.msgSuccess(this.$t('message.addSuccess'))
      })

      this.searchOpen = false
      this.getList()
    },
  },
}
</script>
<style scoped>
.camera-ip-link {
  padding: 0;
  font-weight: 500;
}

.camera-ip-link:hover {
  opacity: 0.88;
}

.custom-red-form {
  --el-text-color-regular: red;
}

.status-cell {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
}

.pulse-dot {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  animation: pulse 2s infinite;
}

.pulse-dot.online {
  background-color: #67c23a;
}

.pulse-dot.offline {
  background-color: #f56c6c;
}

.online-text {
  color: #67c23a;
}

.offline-text {
  color: #f56c6c;
}

@keyframes pulse {
  0% {
    opacity: 1;
  }
  50% {
    opacity: 1;
  }
  100% {
    opacity: 1;
  }
}
</style>

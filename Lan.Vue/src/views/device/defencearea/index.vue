<template>
  <div>
    <!-- ══════════ 向导模式 ══════════ -->
    <div v-if="_isWizard" class="wizard-layout">
      <div class="wizard-steps">
        <el-steps :active="2" align-center finish-status="success">
          <el-step :title="$t('nav.radar')" />
          <el-step :title="$t('nav.camera')" />
          <el-step :title="$t('nav.zone')" />
          <el-step :title="$t('nav.calibration')" />
        </el-steps>
      </div>
      <div class="wizard-body">
        <div class="wizard-header">
          <h3>第 3 步：{{ $t('common.add') }}{{ $t('nav.zone') }}</h3>
          <p class="wizard-desc">请创建防区并绑定雷达和相机</p>
        </div>
        <el-form ref="wizardFormRef" :model="form" :rules="rules" label-width="140px" class="wizard-form">
          <el-row :gutter="20">
            <el-col :lg="12">
              <el-form-item :label="$t('zone.name')" prop="name">
                <el-input v-model="form.name" placeholder="周界防区1" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.defenceRadius')" prop="defenceRadius">
                <el-input v-model="form.defenceRadius" placeholder="500" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('zone.latitude')">
                <el-input v-model="form.latitude" placeholder="39.904200" @blur="form.latitude = formatDecimal(form.latitude)" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('zone.longitude')">
                <el-input v-model="form.longitude" placeholder="116.407400" @blur="form.longitude = formatDecimal(form.longitude)" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('zone.defenceEnable')">
                <el-radio-group v-model="form.defenceEnable">
                  <el-radio v-for="dict in statusOptions" :key="dict.dictValue" :label="parseInt(dict.dictValue)">
                    {{ dict.dictValue === '1' ? $t('common.enabled') : $t('common.disabled') }}
                  </el-radio>
                </el-radio-group>
              </el-form-item>
            </el-col>
          </el-row>
        </el-form>
        <div class="wizard-actions">
          <el-button @click="wizardExit">{{ $t('common.cancel') }}</el-button>
          <el-button @click="wizardSkip">跳过</el-button>
          <el-button type="primary" @click="wizardSubmit" :loading="wizardSubmitting">提交并继续</el-button>
        </div>
      </div>
    </div>

    <!-- ══════════ 普通模式 ══════════ -->
    <template v-else>
    <!-- 搜索区域 -->
    <el-form
      :model="queryParams"
      label-position="right"
      inline
      ref="queryRef"
      v-show="showSearch"
      @submit.prevent
      style="text-align: left"
    >
      <el-form-item :label="$t('zone.name')" prop="name">
        <el-input
          v-model="queryParams.name"
          clearable
          style="width: 240px"
          @keyup.enter="handleQuery"
        />
      </el-form-item>
      <el-form-item>
        <el-button icon="search" type="primary" @click="handleQuery">{{
          $t('common.search')
        }}</el-button>
        <el-button icon="refresh" @click="resetQuery">{{ $t('common.reset') }}</el-button>
      </el-form-item>
    </el-form>

    <!-- 工具区域 -->
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
        <el-button type="success" @click="handleDefenceEnable(1)">{{
          $t('radar.arming')
        }}</el-button>
      </el-col>
      <el-col :span="1.5">
        <el-button type="success" @click="handleDefenceEnable(0)">{{
          $t('radar.disarm')
        }}</el-button>
      </el-col>
    </el-row>

    <!-- 表格 -->
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
      <el-table-column prop="id" :label="$t('zone.id')" align="center" width="100" />
      <el-table-column
        prop="name"
        :label="$t('zone.name')"
        align="center"
        :show-overflow-tooltip="true"
      />
      <el-table-column
        prop="defenceRadius"
        :label="$t('zone.defenceRadius')"
        align="center"
        :show-overflow-tooltip="true"
      />
      <el-table-column
        :label="$t('zone.latitude')"
        align="center"
        :show-overflow-tooltip="true"
      >
        <template #default="scope">
          {{ Number(scope.row.latitude).toFixed(6) }}
        </template>
      </el-table-column>
      <el-table-column
        :label="$t('zone.longitude')"
        align="center"
        :show-overflow-tooltip="true"
      >
        <template #default="scope">
          {{ Number(scope.row.longitude).toFixed(6) }}
        </template>
      </el-table-column>
      <el-table-column :label="$t('zone.defenceEnable')" align="center" prop="defenceEnable">
        <template #default="scope">
          <el-tag :type="scope.row.defenceEnable === 1 ? 'success' : 'danger'">
            {{ scope.row.defenceEnable === 1 ? $t('zone.arming') : $t('zone.disarm') }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column :label="$t('radar.actions')" align="center" width="230">
        <template #default="scope">
          <el-button
            type="primary"
            size="small"
            icon="connection"
            :title="$t('zone.bindDevice')"
            @click="handleBind(scope.row)"
          >{{ $t('zone.bindDevice') }}</el-button>
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

    <pagination
      :total="total"
      v-model:page="queryParams.pageNum"
      v-model:limit="queryParams.pageSize"
      @pagination="getList"
    />

    <!-- 添加/修改对话框 -->
    <el-dialog :title="title" :lock-scroll="false" v-model="open" :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
        <el-row :gutter="20">
          <el-col :lg="12" v-if="opertype != 1">
            <el-form-item :label="$t('zone.id')" prop="id">
              <el-input-number
                v-model.number="form.id"
                controls-position="right"
                :disabled="true"
              />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('zone.name')" prop="name">
              <el-input v-model="form.name" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('zone.defenceRadius')" prop="defenceRadius">
              <el-input v-model="form.defenceRadius" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('zone.latitude')" prop="latitude">
              <el-input v-model="form.latitude" @blur="form.latitude = formatDecimal(form.latitude)" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('zone.longitude')" prop="longitude">
              <el-input v-model="form.longitude" @blur="form.longitude = formatDecimal(form.longitude)" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('zone.defenceEnable')">
              <el-radio-group v-model="form.defenceEnable">
                <el-radio
                  v-for="dict in statusOptions"
                  :key="dict.dictValue"
                  :label="parseInt(dict.dictValue)"
                >
                  {{ dict.dictValue === '1' ? $t('zone.arming') : $t('zone.disarm') }}
                </el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <template #footer v-if="opertype != 3">
        <el-button text @click="cancel">{{ $t('common.cancel') }}</el-button>
        <el-button type="primary" @click="submitForm">{{ $t('common.save') }}</el-button>
      </template>
    </el-dialog>
    </template>

    <!-- 绑定设备对话框 -->
    <el-dialog
      :title="$t('zone.bindDevice') + ' - ' + bindForm.areaName"
      :lock-scroll="false"
      v-model="bindOpen"
      :close-on-click-modal="false"
      width="750px"
      class="bind-dialog"
    >
      <el-divider content-position="left">
        {{ $t('zone.bindCamera') }}
      </el-divider>
      <el-transfer
        v-model="bindForm.cameraIds"
        :data="cameraTransferData"
        :titles="[$t('zone.availableDevice'), $t('zone.selectedDevice')]"
        :button-texts="[$t('common.delete'), $t('common.add')]"
      />

      <el-divider content-position="left" style="margin-top: 24px">
        {{ $t('zone.bindRadar') }}
      </el-divider>
      <el-transfer
        v-model="bindForm.radarIds"
        :data="radarTransferData"
        :titles="[$t('zone.availableDevice'), $t('zone.selectedDevice')]"
        :button-texts="[$t('common.delete'), $t('common.add')]"
      />

      <template #footer>
        <el-button text @click="onBindCancel">{{ $t('common.cancel') }}</el-button>
        <el-button type="primary" @click="submitBind" :loading="bindSubmitting">
          {{ _isWizard ? '保存并继续' : $t('common.save') }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script>
import { getConfigKey } from '@/api/config/config'
import { formatDecimal } from '@/utils/format'
import {
  addDefencearea,
  bindDevices,
  delDefencearea,
  enableDefencearea,
  getDefencearea,
  listDefencearea,
  updateDefencearea,
} from '@/api/device/defencearea.js'

export default {
  name: 'defencearea',
  data() {
    return {
      loading: true,
      ids: [],
      single: true,
      multiple: true,
      showSearch: true,
      total: 0,
      dataList: [],
      title: '',
      open: false,
      statusOptions: [],
      queryParams: {
        pageNum: 1,
        pageSize: 10,
        sort: 'id',
        sortType: 'asc',
        ip: undefined,
      },
      form: {},
      rules: {
        name: [
          { required: true, message: this.$t('validation.name'), trigger: 'blur' },
        ],
        port: [
          { required: true, message: this.$t('validation.port'), trigger: 'blur' },
        ],
        latitude: [
          {
            required: true,
            pattern: /^-?\d*\.?\d{0,6}$/,
            message: this.$t('validation.latitude'),
            trigger: 'blur',
          },
        ],
        longitude: [
          {
            required: true,
            pattern: /^-?\d*\.?\d{0,6}$/,
            message: this.$t('validation.longitude'),
            trigger: 'blur',
          },
        ],
      },
      // 绑定对话框
      bindOpen: false,
      bindSubmitting: false,
      bindForm: {
        areaId: 0,
        areaName: '',
        cameraIds: [],
        radarIds: [],
      },
      cameraTransferData: [],
      radarTransferData: [],
      // 向导模式
      _isWizard: false,
      wizardSubmitting: false,
    }
  },
  created() {
    this.getList()
    this.getDicts('defence_status').then((response) => {
      this.statusOptions = response.data.data
    })
    // 检测向导模式
    try {
      if (localStorage.getItem('wizard') === 'defencearea') {
        this._isWizard = true
        this.reset()
        this.fillMapCenterToForm()
      }
    } catch (e) {}
  },
  methods: {
    getList() {
      this.loading = true
      listDefencearea(this.queryParams).then((res) => {
        if (res.data.code == 200) {
          this.dataList = res.data.data
          this.loading = false
        }
      })
    },
    cancel() {
      this.open = false
      this.reset()
    },
    reset() {
      this.form = {
        id: undefined,
        name: '',
        defenceEnable: 0,
        defenceEnableName: '撤防',
        defenceRadius: 50,
        latitude: '0.000000',
        longitude: '0.000000',
      }
      this.resetForm('formRef')
    },
    handleQuery() {
      this.queryParams.pageNum = 1
      this.getList()
    },
    resetQuery() {
      this.resetForm('queryRef')
      this.handleQuery()
    },
    handleSelectionChange(selection) {
      this.ids = selection.map((item) => item.id)
      this.single = selection.length != 1
      this.multiple = !selection.length
    },
    fillMapCenterToForm() {
      getConfigKey('mapCenter').then((response) => {
        const mapCenter = response?.data?.data
        if (!mapCenter) return
        const [latitude, longitude] = String(mapCenter)
          .split(',')
          .map((item) => item.trim())
        if (latitude !== undefined && latitude !== '') {
          this.form.latitude = this.formatDecimal(latitude)
        }
        if (longitude !== undefined && longitude !== '') {
          this.form.longitude = this.formatDecimal(longitude)
        }
      })
    },
    handleAdd() {
      this.reset()
      this.open = true
      this.title = this.$t('common.add')
      this.fillMapCenterToForm()
    },
    handleUpdate(row) {
      this.reset()
      const Ids = row.id || this.ids
      getDefencearea(Ids).then((response) => {
        const { data } = response
        this.form.id = data.data.defencearea.id
        this.form.name = data.data.defencearea.name
        this.form.defenceEnable = data.data.defencearea.defenceEnable
        this.form.defenceRadius = data.data.defencearea.defenceRadius
        this.form.latitude = data.data.defencearea.latitude
        this.form.longitude = data.data.defencearea.longitude
        this.form.latitude = this.formatDecimal(this.form.latitude)
        this.form.longitude = this.formatDecimal(this.form.longitude)
        this.open = true
        this.title = this.$t('common.edit')
      })
    },
    submitForm: function () {
      const formRefName = this._isWizard ? 'wizardFormRef' : 'formRef'
      this.$refs[formRefName].validate((valid) => {
        if (this.form.defenceEnable == 1) this.form.defenceEnableName = '布防'
        else this.form.defenceEnableName = '撤防'

        if (valid) {
          if (this.form.id != undefined) {
            updateDefencearea(this.form).then((res) => {
              if (res.data.code == 200) {
                this.$modal.msgSuccess(this.$t('message.editSuccess'))
                this.open = false
                this.getList()
              } else if (res.data.code == 102) {
                this.$modal.msgError(this.$t('message.zoneDATA_REPEAT'))
              }
            })
          } else {
            addDefencearea(this.form).then((res) => {
              if (res.data.code == 200) {
                this.$modal.msgSuccess(this.$t('message.addSuccess'))
                this.open = false
                this.getList()
              } else if (res.data.code == 102) {
                this.$modal.msgError(this.$t('message.zoneDATA_REPEAT'))
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
          return delDefencearea(Ids)
        })
        .then(() => {
          this.getList()
          this.$modal.msgSuccess(this.$t('message.deleteSuccess'))
        })
    },
    handleDefenceEnable(e) {
      enableDefencearea(e).then((res) => {
        if (res.data.code == 200) {
          this.getList()
          if (e == 1) this.$modal.msgSuccess(this.$t('message.all_arming'))
          else this.$modal.msgSuccess(this.$t('message.all_disarm'))
        }
      })
    },

    // ══════════ 绑定设备 ══════════
    handleBind(row) {
      this.bindForm = {
        areaId: row.id,
        areaName: row.name,
        cameraIds: [],
        radarIds: [],
      }
      this.cameraTransferData = []
      this.radarTransferData = []

      getDefencearea(row.id).then((response) => {
        const { data } = response
        const allCameras = data.data.cameras || []
        const allRadars = data.data.radars || []
        const boundCameraIds = data.data.cameraIds || []
        const boundRadarIds = data.data.radarIds || []

        this.cameraTransferData = allCameras.map((cam) => ({
          key: cam.id,
          label: cam.ip + (cam.name ? ' (' + cam.name + ')' : ''),
          disabled: cam.bindingAreaId > 0 && cam.bindingAreaId !== row.id,
        }))

        this.radarTransferData = allRadars.map((radar) => ({
          key: radar.id,
          label: radar.ip,
          disabled: radar.bindingAreaId > 0 && radar.bindingAreaId !== row.id,
        }))

        this.bindForm.cameraIds = [...boundCameraIds]
        this.bindForm.radarIds = [...boundRadarIds]

        this.bindOpen = true
      })
    },

    submitBind() {
      this.bindSubmitting = true
      bindDevices({
        defenceAreaId: this.bindForm.areaId,
        cameraIds: this.bindForm.cameraIds,
        radarIds: this.bindForm.radarIds,
      })
        .then((res) => {
          if (res.data.code == 200) {
            this.$modal.msgSuccess(this.$t('message.editSuccess'))
            this.bindOpen = false
            this.getList()
            // 向导模式：绑定完成后跳转到标定
            if (this._isWizard) {
              this.wizardGoNext()
            }
          }
        })
        .finally(() => {
          this.bindSubmitting = false
        })
    },

    onBindCancel() {
      this.bindOpen = false
      // 向导模式下取消绑定，仍然可以继续下一步
    },

    // ══════════ 向导方法 ══════════
    wizardSubmit() {
      const formRef = this.$refs['wizardFormRef']
      if (!formRef) return
      formRef.validate((valid) => {
        if (this.form.defenceEnable == 1) this.form.defenceEnableName = '布防'
        else this.form.defenceEnableName = '撤防'

        if (valid) {
          this.wizardSubmitting = true
          addDefencearea(this.form).then((res) => {
            if (res.data.code == 200) {
              this.$modal.msgSuccess(this.$t('message.addSuccess'))
              this.getList()
              // 拿到新创建的防区ID，打开绑定对话框
              const newId = res.data.data
              this.handleBind({ id: newId, name: this.form.name })
            } else if (res.data.code == 102) {
              this.$modal.msgError(this.$t('message.zoneDATA_REPEAT'))
            }
          }).finally(() => {
            this.wizardSubmitting = false
          })
        }
      })
    },
    wizardSkip() {
      this.wizardGoNext()
    },
    wizardExit() {
      try { localStorage.removeItem('wizard') } catch (e) {}
      try { window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'realtime_map' })) } catch (e) {}
    },
    wizardGoNext() {
      try { localStorage.setItem('wizard', 'calibration') } catch (e) {}
      try { window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'calibration' })) } catch (e) {}
    },

    formatDecimal,
  },
}
</script>

<style scoped>
/* 向导布局 */
.wizard-layout { min-height: 100vh; background: #f5f7fa; padding: 24px 32px; }
.wizard-steps { max-width: 800px; margin: 0 auto 32px; padding: 24px; background: #fff; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,0.04); }
.wizard-body { max-width: 800px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 32px; box-shadow: 0 2px 12px rgba(0,0,0,0.04); }
.wizard-header { margin-bottom: 24px; padding-bottom: 16px; border-bottom: 1px solid #ebeef5; }
.wizard-header h3 { margin: 0 0 8px; font-size: 18px; color: #303133; }
.wizard-desc { margin: 0; font-size: 14px; color: #909399; }
.wizard-form { margin-bottom: 24px; }
.wizard-actions { display: flex; justify-content: flex-end; gap: 12px; padding-top: 20px; border-top: 1px solid #ebeef5; }
</style>

<style>
.bind-dialog .el-transfer-panel .el-transfer-panel__body {
  height: 160px !important;
}
</style>

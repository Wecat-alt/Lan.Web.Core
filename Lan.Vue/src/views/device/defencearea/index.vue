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
              <el-form-item :label="$t('common.camera')">
                <el-select v-model="form.cameraId" clearable class="w-select">
                  <el-option v-for="dict in cameraOptions" :key="dict.id" :label="dict.ip" :value="dict.id" />
                </el-select>
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.ip')">
                <el-select v-model="form.radarId" clearable class="w-select">
                  <el-option v-for="dict in radarOptions" :key="dict.id" :label="dict.ip" :value="dict.id" />
                </el-select>
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
      <el-table-column prop="id" :label="$t('zone.id')" align="center" />
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
    <pagination
      :total="total"
      v-model:page="queryParams.pageNum"
      v-model:limit="queryParams.pageSize"
      @pagination="getList"
    />

    <!-- 添加或修改对话框 -->
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

          <!-- defence_status -->
          <el-col :lg="12">
            <el-form-item :label="$t('zone.defenceEnable')">
              <!-- <el-input v-model.number="form.defenceEnable" /> -->
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

          <el-col :lg="24">
            <el-form-item :label="$t('common.camera')">
              <el-select
                v-model="form.cameraIds"
                multiple
                :placeholder="$t('zone.select_camera')"
                style="width: 100%"
                @change="selectCamera($event)"
              >
                <el-option
                  v-for="item in cameraOptions"
                  :key="item.id"
                  :label="item.ip"
                  :value="item.id"
                  :disabled="item.status == 0"
                >
                  <span style="float: left">{{ item.ip }}</span>
                  <span style="float: right">{{ item.id }}</span>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :lg="24">
            <el-form-item :label="$t('common.radar')">
              <el-select
                v-model="form.radarIds"
                multiple
                :placeholder="$t('zone.select_radar')"
                style="width: 100%"
                @change="selectRadar($event)"
              >
                <el-option
                  v-for="item in radarOptions"
                  :key="item.id"
                  :label="item.ip"
                  :value="item.id"
                  :disabled="item.status == 0"
                >
                  <span style="float: left">{{ item.ip }}</span>
                  <span style="float: right">{{ item.id }}</span>
                </el-option>
              </el-select>
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
  </div>
</template>

<script>
import { getConfigKey } from '@/api/config/config'
import { formatDecimal } from '@/utils/format'
import {
  getCameraRepetitionJudgmentAdd,
  getCameraRepetitionJudgmentEdit,
} from '@/api/device/camera'
import {
  addDefencearea,
  delDefencearea,
  enableDefencearea,
  getDefencearea,
  listDefencearea,
  updateDefencearea,
} from '@/api/device/defencearea.js'
import { getRadarRepetitionJudgmentAdd, getRadarRepetitionJudgmentEdit } from '@/api/device/radar'
import { ElMessageBox } from 'element-plus'

export default {
  name: 'defencearea',
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
      cameraOptions: ref([]),
      radarOptions: ref([]),
      // 表单参数
      form: {},
      // 表单校验
      rules: {
        name: [
          {
            required: true,
            message: this.$t('validation.name'),
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
      // 向导模式标记
      _isWizard: false,
      wizardSubmitting: false,
    }
  },
  created() {
    this.getList()
    this.getDicts('defence_status').then((response) => {
      this.statusOptions = response.data.data
      try {
        if (localStorage.getItem('wizard') === 'defencearea') {
          this.handleAdd()
        }
      } catch (e) {}
    })
    getDefencearea(0).then((response) => {
      this.cameraOptions = response.data.data.cameras
      this.radarOptions = response.data.data.radars
    })
  },
  methods: {
    getList() {
      this.loading = true
      listDefencearea(this.queryParams).then((res) => {
        if (res.data.code == 200) {
          this.dataList = res.data.data
          //total.value = data.totalNum
          this.loading = false
        }
      })
    },
    cancel() {
      this.open = false
      this.reset()
    },
    // 表单重置
    reset() {
      this.form = {
        id: undefined,
        name: '',
        defenceEnable: 0,
        defenceEnableName: '撤防',
        defenceRadius: 50,
        latitude: '0.000000',
        longitude: '0.000000',

        cameraIds: [],
        radarIds: [],
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
        if (!mapCenter) {
          return
        }

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
      this._isWizard = localStorage.getItem('wizard') === 'defencearea'
      if (this._isWizard) {
        this.title = this.$t('common.add') + ' - ' + this.$t('nav.zone')
        this.fillMapCenterToForm()
      } else {
        this.open = true
        this.title = this.$t('common.add')
        this.fillMapCenterToForm()
      }
    },
    handleUpdate(row) {
      this.reset()
      const Ids = row.id || this.ids
      getDefencearea(Ids).then((response) => {
        const { code, data } = response

        this.form.id = data.data.defencearea.id
        this.form.name = data.data.defencearea.name
        this.form.defenceEnable = data.data.defencearea.defenceEnable
        this.form.defenceRadius = data.data.defencearea.defenceRadius
        this.form.cameraIds = data.data.cameraIds
        this.form.radarIds = data.data.radarIds
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
                if (this._isWizard) {
                  try {
                    localStorage.setItem('wizard', 'calibration')
                  } catch (e) {}
                  try {
                    window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'calibration' }))
                  } catch (e) {}
                }
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
    selectCamera(e) {
      if (this.form.id != undefined) {
        let form = {
          bindingAreaId: this.form.id,
          cameraIds: this.form.cameraIds,
        }
        //要排除自己
        getCameraRepetitionJudgmentEdit(form).then((res) => {
          if (res.data.code == 104) {
            if (res.data.msg != '') {
              ElMessageBox.alert(
                res.data.msg + this.$t('message.zone_tip'),
                this.$t('common.hint'),
                { type: 'warning' },
              )
            }
          }
        })
      } else {
        let form = { cameraIds: this.form.cameraIds }
        getCameraRepetitionJudgmentAdd(form).then((res) => {
          if (res.data.code == 104) {
            if (res.data.msg != '') {
              ElMessageBox.alert(
                res.data.msg + this.$t('message.zone_tip'),
                this.$t('common.hint'),
                { type: 'warning' },
              )
            }
          }
        })
      }
      this.$forceUpdate()
    },
    selectRadar(e) {
      if (this.form.id != undefined) {
        let form = {
          bindingAreaId: this.form.id,
          radarIds: this.form.radarIds,
        }
        //要排除自己
        getRadarRepetitionJudgmentEdit(form).then((res) => {
          if (res.data.code == 104) {
            if (res.data.msg != '') {
              ElMessageBox.alert(
                res.data.msg + this.$t('message.zone_tip'),
                this.$t('common.hint'),
                { type: 'warning' },
              )
            }
          }
        })
      } else {
        let form = { radarIds: this.form.radarIds }
        getRadarRepetitionJudgmentAdd(form).then((res) => {
          if (res.data.code == 104) {
            if (res.data.msg != '') {
              ElMessageBox.alert(
                res.data.msg + this.$t('message.zone_tip'),
                this.$t('common.hint'),
                { type: 'warning' },
              )
            }
          }
        })
      }
      this.$forceUpdate()
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
    // ══════════ 向导方法 ══════════
    wizardSubmit() {
      this.wizardSubmitting = true
      this.submitForm()
      setTimeout(() => { this.wizardSubmitting = false }, 3000)
    },
    wizardSkip() {
      try { localStorage.setItem('wizard', 'calibration') } catch (e) {}
      try { window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'calibration' })) } catch (e) {}
    },
    wizardExit() {
      try { localStorage.removeItem('wizard') } catch (e) {}
      try { window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'realtime_map' })) } catch (e) {}
    },
    formatDecimal,
  },
}
</script>

<style scoped>
/* ══════════ 向导布局 ══════════ */
.wizard-layout { min-height: 100vh; background: #f5f7fa; padding: 24px 32px; }
.wizard-steps { max-width: 800px; margin: 0 auto 32px; padding: 24px; background: #fff; border-radius: 12px; box-shadow: 0 2px 12px rgba(0,0,0,0.04); }
.wizard-body { max-width: 800px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 32px; box-shadow: 0 2px 12px rgba(0,0,0,0.04); }
.wizard-header { margin-bottom: 24px; padding-bottom: 16px; border-bottom: 1px solid #ebeef5; }
.wizard-header h3 { margin: 0 0 8px; font-size: 18px; color: #303133; }
.wizard-desc { margin: 0; font-size: 14px; color: #909399; }
.wizard-form { margin-bottom: 24px; }
.wizard-actions { display: flex; justify-content: flex-end; gap: 12px; padding-top: 20px; border-top: 1px solid #ebeef5; }
</style>

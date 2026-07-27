<template>
  <div>
    <!-- ══════════ 向导模式 ══════════ -->
    <div v-if="_isWizard" class="wizard-layout">
      <div class="wizard-steps">
        <el-steps :active="0" align-center finish-status="success">
          <el-step :title="$t('nav.radar')" />
          <el-step :title="$t('nav.camera')" />
          <el-step :title="$t('nav.zone')" />
          <el-step :title="$t('nav.calibration')" />
        </el-steps>
      </div>

      <div class="wizard-body">
        <div class="wizard-header">
          <h3>第 1 步：{{ $t('common.add') }}{{ $t('nav.radar') }}</h3>
          <p class="wizard-desc">请添加至少一个雷达设备，填写 IP、经纬度和探测参数</p>
        </div>

        <el-form ref="wizardFormRef" :model="form" :rules="rules" label-width="140px" class="wizard-form">
          <el-row :gutter="20">
            <el-col :lg="12">
              <el-form-item :label="$t('radar.ip')" prop="ip">
                <el-input v-model="form.ip" placeholder="192.168.1.100" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.port')" prop="port">
                <el-input v-model.number="form.port" :disabled="true" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.latitude')" prop="latitude">
                <el-input v-model="form.latitude" placeholder="39.904200" @blur="form.latitude = formatDecimal(form.latitude)" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.longitude')" prop="longitude">
                <el-input v-model="form.longitude" placeholder="116.407400" @blur="form.longitude = formatDecimal(form.longitude)" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.northDeviationAngle')" prop="northDeviationAngle">
                <el-input v-model="form.northDeviationAngle" placeholder="0.000000" @blur="form.northDeviationAngle = formatDecimal(form.northDeviationAngle)" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.defenceRadius')" prop="defenceRadius">
                <el-input v-model="form.defenceRadius" placeholder="500" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.defenceAngle')" prop="defenceAngle">
                <el-input v-model="form.defenceAngle" placeholder="90" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.radarType')" prop="radarType">
                <el-input v-model="form.radarType" placeholder="NSR-100" />
              </el-form-item>
            </el-col>
            <el-col :lg="12">
              <el-form-item :label="$t('radar.status')">
                <el-radio-group v-model="form.status">
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
          <el-button type="primary" @click="wizardSubmit" :loading="wizardSubmitting">
            提交并继续
          </el-button>
        </div>
      </div>
    </div>

    <!-- ══════════ 普通模式 ══════════ -->
    <template v-else>
    <!-- <el-form :model="queryParams" label-position="right" inline ref="queryRef" v-show="showSearch" @submit.prevent>
        <el-form-item label="雷达IP" prop="ip">
          <el-input v-model="queryParams.ip" placeholder="请输入雷达IP" clearable style="width: 240px"
            @keyup.enter="handleQuery" />
        </el-form-item>

        <el-form-item>
          <el-button icon="search" type="primary" @click="handleQuery">{{ $t('btn.search') }}</el-button>
          <el-button icon="refresh" @click="resetQuery">{{ $t('btn.reset') }}</el-button>
        </el-form-item>
      </el-form> -->

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
        <el-button icon="search" type="primary" @click="handleQuery">{{
          $t('common.refresh')
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
      <el-table-column prop="id" :label="$t('radar.id')" align="center" fixed />
      <el-table-column
        prop="ip"
        :label="$t('radar.ip')"
        width="180"
        align="center"
        :show-overflow-tooltip="true"
      />

      <el-table-column :label="$t('radar.latitude')" width="140" align="center">
        <template #default="scope">
          {{ Number(scope.row.latitude).toFixed(6) }}
        </template>
      </el-table-column>
      <el-table-column :label="$t('radar.longitude')" width="140" align="center">
        <template #default="scope">
          {{ Number(scope.row.longitude).toFixed(6) }}
        </template>
      </el-table-column>
      <el-table-column
        :label="$t('radar.northDeviationAngle')"
        align="center"
      >
        <template #default="scope">
          {{ Number(scope.row.northDeviationAngle).toFixed(6) }}
        </template>
      </el-table-column>

      <el-table-column prop="defenceRadius" :label="$t('radar.defenceRadius')" align="center" />
      <el-table-column prop="defenceAngle" :label="$t('radar.defenceAngle')" align="center" />
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
      <el-table-column prop="radarType" :label="$t('radar.radarType')" align="center" />
      <el-table-column prop="port" :label="$t('radar.port')" width="120" align="center" />
      <el-table-column :label="$t('radar.status')" align="center" prop="status">
        <template #default="scope">
          <el-tag :type="scope.row.status === 1 ? 'success' : 'danger'">
            {{ scope.row.status === 1 ? $t('common.enabled') : $t('common.disabled') }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column :label="$t('radar.actions')" width="160">
        <template #default="scope">
          <el-button
            type="success"
            size="small"
            icon="edit"
            :title="$t('common.edit')"
            @click="handleUpdate(scope.row)"
          ></el-button>
          <el-button
            type="danger"
            size="small"
            icon="delete"
            :title="$t('common.delete')"
            @click="handleDelete(scope.row)"
          ></el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- <pagination v-show="total > 0" :total="total" :page.sync="queryParams.pageNum" :limit.sync="queryParams.pageSize" @pagination="getList" /> -->

    <!-- 添加或修改对话框 -->
    <el-dialog :title="title" v-model="open" append-to-body :close-on-click-modal="false">
      <el-form ref="formRef" :model="form" :rules="rules" label-width="120px">
        <el-row :gutter="20">
          <el-col :lg="12" v-if="opertype != 1">
            <el-form-item :label="$t('radar.id')" prop="id">
              <el-input-number
                v-model.number="form.id"
                controls-position="right"
                :disabled="true"
              />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('radar.ip')" prop="ip">
              <el-input v-model="form.ip" :disabled="form.id != undefined" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('radar.port')" prop="port">
              <el-input v-model.number="form.port" :disabled="true" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('radar.northDeviationAngle')" prop="northDeviationAngle">
              <el-input v-model="form.northDeviationAngle" @blur="form.northDeviationAngle = formatDecimal(form.northDeviationAngle)" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('radar.latitude')" prop="latitude">
              <el-input v-model="form.latitude" @blur="form.latitude = formatDecimal(form.latitude)" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('radar.longitude')" prop="longitude">
              <el-input v-model="form.longitude" @blur="form.longitude = formatDecimal(form.longitude)" />
            </el-form-item>
          </el-col>

          <el-col :lg="12">
            <el-form-item :label="$t('radar.defenceRadius')" prop="defenceRadius">
              <el-input v-model="form.defenceRadius" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('radar.defenceAngle')" prop="defenceAngle">
              <el-input v-model="form.defenceAngle" />
            </el-form-item>
          </el-col>

          <el-col :lg="12">
            <el-form-item :label="$t('radar.radarType')" prop="radarType">
              <el-input v-model="form.radarType" />
            </el-form-item>
          </el-col>

          <el-col :lg="12">
            <el-form-item :label="$t('radar.status')">
              <el-radio-group v-model="form.status">
                <el-radio
                  v-for="dict in statusOptions"
                  :key="dict.dictValue"
                  :label="parseInt(dict.dictValue)"
                >
                  <!-- {{dict.dictLabel }} -->
                  {{ dict.dictValue === '1' ? $t('common.enabled') : $t('common.disabled') }}
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
  </div>
</template>

<script>
import { getConfigKey } from '@/api/config/config'
import { formatDecimal } from '@/utils/format'
import { addRadar, delRadar, getRadar, listRadar, updateRadar } from '@/api/device/radar'

export default {
  name: 'radar',
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
      // 表单校验
      rules: {
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
        northDeviationAngle: [
          {
            required: true,
            pattern: /^-?\d*\.?\d{0,6}$/,
            message: this.$t('validation.northDeviationAngle'),
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
    this.getDicts('device_status').then((response) => {
      this.statusOptions = response.data.data
    })
    // 如果处于向导模式并且步骤为雷达，则自动弹出添加对话框
    try {
      if (localStorage.getItem('wizard') === 'radar') {
        this.handleAdd()
      }
    } catch (e) {}
  },
  methods: {
    handleQuery() {
      this.getList()
    },
    getList() {
      this.loading = true
      listRadar(this.queryParams).then((res) => {
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
        bindingAreaId: -1,
        ip: null,
        port: '50000',
        status: '1',
        latitude: '0.000000',
        longitude: '0.000000',
        northDeviationAngle: '0.000000',
        radarType: '',
        defenceAngle: '90',
        defenceRadius: '500',
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
      // 如果处于向导模式，标记当前步骤并在添加成功后跳转到相机管理
      this._isWizard = localStorage.getItem('wizard') === 'radar'
      if (this._isWizard) {
        // 向导模式：表单内嵌显示，不弹 dialog
        this.title = this.$t('common.add') + ' - ' + this.$t('nav.radar')
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
      getRadar(Ids).then((response) => {
        this.form = response.data.data
        this.form.latitude = this.formatDecimal(this.form.latitude)
        this.form.longitude = this.formatDecimal(this.form.longitude)
        this.form.northDeviationAngle = this.formatDecimal(this.form.northDeviationAngle)
        this.open = true
        this.title = this.$t('common.edit')
      })
    },
    submitForm: function () {
      const formRefName = this._isWizard ? 'wizardFormRef' : 'formRef'
      this.$refs[formRefName].validate((valid) => {
        if (valid) {
          if (this.form.id != undefined) {
            updateRadar(this.form).then((response) => {
              this.$modal.msgSuccess(this.$t('message.editSuccess'))
              this.open = false
              this.getList()
            })
          } else {
            addRadar(this.form).then((res) => {
              if (res.data.code == 200) {
                this.$modal.msgSuccess(this.$t('message.addSuccess'))
                this.open = false
                this.getList()
                if (this._isWizard) {
                  try {
                    localStorage.setItem('wizard', 'camera')
                  } catch (e) {}
                  // 触发应用切换到相机页面（全局事件）
                  try {
                    window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'camera' }))
                  } catch (e) {}
                }
              } else if (res.data.code == 102) {
                this.$modal.msgError(res.data.msg)
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
          return delRadar(Ids)
        })
        .then(() => {
          this.getList()
          this.$modal.msgSuccess(this.$t('message.deleteSuccess'))
        })
    },
    // ══════════ 向导方法 ══════════
    wizardSubmit() {
      this.wizardSubmitting = true
      this.submitForm()
      // submitForm 异步，成功后会在回调里关 loading
      setTimeout(() => { this.wizardSubmitting = false }, 3000)
    },
    wizardSkip() {
      try { localStorage.setItem('wizard', 'camera') } catch (e) {}
      try { window.dispatchEvent(new CustomEvent('wizard-next', { detail: 'camera' })) } catch (e) {}
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

/* ══════════ 向导布局 ══════════ */
.wizard-layout {
  min-height: 100vh;
  background: #f5f7fa;
  padding: 24px 32px;
}

.wizard-steps {
  max-width: 800px;
  margin: 0 auto 32px;
  padding: 24px;
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.04);
}

.wizard-body {
  max-width: 800px;
  margin: 0 auto;
  background: #fff;
  border-radius: 12px;
  padding: 32px;
  box-shadow: 0 2px 12px rgba(0,0,0,0.04);
}

.wizard-header {
  margin-bottom: 24px;
  padding-bottom: 16px;
  border-bottom: 1px solid #ebeef5;
}

.wizard-header h3 {
  margin: 0 0 8px;
  font-size: 18px;
  color: #303133;
}

.wizard-desc {
  margin: 0;
  font-size: 14px;
  color: #909399;
}

.wizard-form {
  margin-bottom: 24px;
}

.wizard-actions {
  display: flex;
  justify-content: flex-end;
  gap: 12px;
  padding-top: 20px;
  border-top: 1px solid #ebeef5;
}
</style>

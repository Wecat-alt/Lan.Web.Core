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
        prop="latitude"
        :label="$t('zone.latitude')"
        align="center"
        :show-overflow-tooltip="true"
      />
      <el-table-column
        prop="longitude"
        :label="$t('zone.longitude')"
        align="center"
        :show-overflow-tooltip="true"
      />
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
              <el-input v-model="form.latitude" />
            </el-form-item>
          </el-col>
          <el-col :lg="12">
            <el-form-item :label="$t('zone.longitude')" prop="longitude">
              <el-input v-model="form.longitude" />
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
  </div>
</template>

<script>
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
        latitude: '0',
        longitude: '0',

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
    handleAdd() {
      this.reset()
      this.open = true
      this.title = this.$t('common.add')
      this._isWizard = localStorage.getItem('wizard') === 'defencearea'
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

        this.open = true
        this.title = this.$t('common.edit')
      })
    },
    submitForm: function () {
      this.$refs['formRef'].validate((valid) => {
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
  },
}
</script>

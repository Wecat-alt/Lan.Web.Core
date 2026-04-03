<template>
  <div class="ptz-con">
    <!-- <el-button @mousedown="handleMouseDown('topleft')" @mouseup="handleMouseUp('topleft')" v-hasPermi="['camera:add']" plain icon="TopLeft">
		</el-button>
		<el-button @mousedown="handleMouseDown('top')" @mouseup="handleMouseUp('top')" v-hasPermi="['camera:add']" plain icon="Top">
		</el-button>
		<el-button @mousedown="handleMouseDown('topright')" @mouseup="handleMouseUp('topright')" v-hasPermi="['camera:add']" plain icon="TopRight">
		</el-button>
		<br>
		<el-button @mousedown="handleMouseDown('back')" @mouseup="handleMouseUp('back')" v-hasPermi="['camera:add']" plain icon="Back">
		</el-button>
		<el-button @mousedown="handleMouseDown('index')" @mouseup="handleMouseUp('index')" v-hasPermi="['camera:add']" plain icon="Index">
		</el-button>
		<el-button @mousedown="handleMouseDown('right')" @mouseup="handleMouseUp('right')" v-hasPermi="['camera:add']" plain icon="Right">
		</el-button>
		<br>
		<el-button @mousedown="handleMouseDown('bottomleft')" @mouseup="handleMouseUp('bottomleft')" v-hasPermi="['camera:add']" plain icon="BottomLeft">
		</el-button>
		<el-button @mousedown="handleMouseDown('bottom')" @mouseup="handleMouseUp('bottom')" v-hasPermi="['camera:add']" plain icon="Bottom">
		</el-button>
		<el-button @mousedown="handleMouseDown('bottomright')" @mouseup="handleMouseUp('bottomright')" v-hasPermi="['camera:add']" plain icon="BottomRight">
		</el-button>
		<br>
		<br>
		<el-button @mousedown="handleMouseDown('in')" @mouseup="handleMouseUp('in')" v-hasPermi="['camera:add']" plain icon="ZoomIn">
		</el-button>
		<el-button @mousedown="handleMouseDown('outin')" @mouseup="handleMouseUp('outin')" v-hasPermi="['camera:add']" plain icon="ZoomOut">
		</el-button> -->
    <div class="ptz-layout-compact">
      <div class="ptz-direction-group">
        <div class="ptz-row">
          <el-button
            @mousedown="handleMouseDown('topleft')"
            @mouseup="handleMouseUp('topleft')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="TopLeft"
          ></el-button>
          <el-button
            @mousedown="handleMouseDown('top')"
            @mouseup="handleMouseUp('top')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="Top"
          ></el-button>
          <el-button
            @mousedown="handleMouseDown('topright')"
            @mouseup="handleMouseUp('topright')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="TopRight"
          ></el-button>
        </div>
        <div class="ptz-row">
          <el-button
            @mousedown="handleMouseDown('back')"
            @mouseup="handleMouseUp('back')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="Back"
          ></el-button>
          <el-button
            @mousedown="handleMouseDown('index')"
            @mouseup="handleMouseUp('index')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="Refresh"
          ></el-button>
          <el-button
            @mousedown="handleMouseDown('right')"
            @mouseup="handleMouseUp('right')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="Right"
          ></el-button>
        </div>
        <div class="ptz-row">
          <el-button
            @mousedown="handleMouseDown('bottomleft')"
            @mouseup="handleMouseUp('bottomleft')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="BottomLeft"
          ></el-button>
          <el-button
            @mousedown="handleMouseDown('bottom')"
            @mouseup="handleMouseUp('bottom')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="Bottom"
          ></el-button>
          <el-button
            @mousedown="handleMouseDown('bottomright')"
            @mouseup="handleMouseUp('bottomright')"
            class="ptz-btn"
            v-hasPermi="['camera:add']"
            plain
            icon="BottomRight"
          ></el-button>
        </div>
      </div>
      <div class="ptz-zoom-group">
        <button
          @mousedown="handleMouseDown('in')"
          @mouseup="handleMouseUp('in')"
          class="ptz-btn-Zoom"
          v-hasPermi="['camera:add']"
        >
          <el-icon><ZoomIn /></el-icon>
        </button>
        <button
          @mousedown="handleMouseDown('outin')"
          @mouseup="handleMouseUp('outin')"
          class="ptz-btn-Zoom"
          v-hasPermi="['camera:add']"
        >
          <el-icon><ZoomOut /></el-icon>
        </button>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  name: 'PtzControl',
  methods: {
    handleMouseDown(type) {
      this.$emit('childEvent1', type)
    },
    handleMouseUp(type) {
      this.$emit('childEvent2', type)
    },
  },
}
</script>

<style scoped>
.ptz-layout-compact {
  display: flex;
  align-items: stretch;
  gap: 10px;
}

.ptz-direction-group {
  display: flex;
  flex-direction: column;
}

.ptz-zoom-group {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  align-items: center;
}

.ptz-zoom-group .ptz-btn-Zoom {
  margin: 0;
}
</style>

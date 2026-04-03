<template>
  <div class="video-container">
    <video ref="videoPlayer" controls autoplay muted></video>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'

const videoPlayer = ref(null)
let mediaSource = null
let sourceBuffer = null
let ws = null

const initWebSocket = () => {
  ws = new WebSocket('ws://localhost:5197/ws')
  
  ws.onopen = () => {
    console.log('WebSocket connected')
  }
  
  ws.onmessage = (event) => {
    if (sourceBuffer && !sourceBuffer.updating) {
      const arrayBuffer = event.data
      sourceBuffer.appendBuffer(arrayBuffer)
    }
  }
  
  ws.onclose = () => {
    console.log('WebSocket disconnected')
  }
}

const initMediaSource = () => {
  mediaSource = new MediaSource()
  videoPlayer.value.src = URL.createObjectURL(mediaSource)
  
  mediaSource.addEventListener('sourceopen', () => {
    sourceBuffer = mediaSource.addSourceBuffer('video/mp2t')
    sourceBuffer.mode = 'sequence'
  })
}

onMounted(() => {
  initMediaSource()
  initWebSocket()
})

onUnmounted(() => {
  if (ws) {
    ws.close()
  }
})
</script>

<style scoped>
.video-container {
  width: 100%;
  max-width: 800px;
  margin: 0 auto;
}
video {
  width: 100%;
  height: auto;
}
</style>
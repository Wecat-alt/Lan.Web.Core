<template>
  <div>
    <video ref="videoPlayer" controls :src="videoSrc" type="video/mp4" autoplay muted loop>

    </video>
    <!-- <div>
      <button @click="play">播放</button>
      <button @click="pause">暂停</button>
    </div> -->
  </div>
</template>

<script setup>
  const videoSrc = ref('/videos/2025.09.25_15.29.36.8670_192.168.88.204video.mp4'); // 假设视频放在public/videos目录下
  const videoPlayer = ref(null); // 用于获取video元素的引用

  const play = () => {
    if (videoPlayer.value) {
      videoPlayer.value.play();
    }
  };

  const pause = () => {
    if (videoPlayer.value) {
      videoPlayer.value.pause();
    }
  };

</script>

<style scoped>
video {
  width: 100%;
  max-width: 600px;
}
</style>
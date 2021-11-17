<template>
  <scrollView height="100%" :baseWrap="true" class="vtm-scroll-common-wrap">
    <div class="common-wrap">
      <slot></slot>
    </div>
  </scrollView>
</template>
<script>
export default {
  name: 'commonWrap'
}
</script>
<style lang="less">
.vtm-scroll-common-wrap{
  padding: 15px;
}
.common-wrap{
  height: 100%;
  .common-box{
    min-height: 100%;
  }
}
@media screen and (max-width: 1441px) {
  .vtm-scroll-common-wrap{
    padding: 10px;
  }
}
</style>

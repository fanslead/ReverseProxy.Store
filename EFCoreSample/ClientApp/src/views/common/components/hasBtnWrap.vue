<template>
  <scrollView height="100%" :baseWrap="true" class="vtm-scroll-view-wrap">
    <template v-slot:button>
      <button-wrap :className="className">
        <slot name="btn"></slot>
      </button-wrap>
    </template>
    <div class="has-btn-wrap">
      <div class="has-btn-box"><slot></slot></div>
    </div>
  </scrollView>
</template>
<script>
import buttonWrap from './buttonWrap'
export default {
  name: 'hasBtnWrap',
  components: {
    buttonWrap
  },
  props: {
    className: {
      type: String,
      default: ""
    }
  }
}
</script>
<style lang="less">
#main.vtm-scroll-view-wrap{
  display: flex;
  flex-direction: column;
}
.has-btn-wrap{
  height: 100%;
  .has-btn-box{
    height: 100%;
    padding: 15px;
  }
  .common-box{
    min-height: 100%;
  }
}
@media screen and (max-width: 1441px) {
  .has-btn-wrap{
    .has-btn-box{
      padding: 10px;
    }
  }
}
</style>

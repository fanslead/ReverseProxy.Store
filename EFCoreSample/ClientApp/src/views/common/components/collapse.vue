<template>
  <Row type="flex" class="flex-column collapse-wrap">
    <slot name="search" v-if="expand"></slot>
    <div class="content-wrap">
      <div class="collapse-item" @click="expand = !expand" v-if="!hideExpend">{{expand ? '收起' : '展开'}}</div>
      <slot name="content"></slot>
    </div>
  </Row>  
</template>
<script>
export default {
  name: 'collapse',
  data() {
    return {
      expand: true
    }
  },
  props: {
    hideExpend: { // 是否隐藏展开/收起
      type: Boolean,
      default: false
    }
  }
}
</script>
<style lang="less">
.collapse-wrap{
  .search-wrap{
    border-bottom: 1px solid #D9D9D9;
  }
  .collapse-item{
    width: 160px;
    height: 24px;
    line-height: 24px;
    background: #fff;
    box-shadow: 0px 4px 10px 0px rgba(0,0,0,0.1),0px -1px 0px 0px rgba(217,217,217,1);
    border-radius: 0px 0px 24px 24px;
    margin: 0 auto 16px;
    text-align: center;
    color: #333;
    font-size: 12px;
    cursor: pointer;
  }
}
</style>

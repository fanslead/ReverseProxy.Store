<template>
  <Row type="flex" align="middle" id="button-wrap" :class="className">
    <slot></slot>
  </Row>
</template>
<script>
export default {
  name: 'buttonWrap',
  props: {
    className: {
      type: String,
      default: ""
    }
  }
}
</script>
<style lang="less">
#button-wrap{
  height: 40px;
  box-shadow:0px 4px 10px 0px rgba(0,0,0,0.1);
  background: #fff;
  padding: 0 15px;
  overflow: hidden;
  z-index: 100;
  button{
    &:not(:last-child) {
      margin-right: 16px;
    }
  }
}
@media screen and (max-width: 1441px) {
  #button-wrap{
    padding: 0 10px;
    button{
      &:not(:last-child) {
        margin-right: 10px;
      }
    }
  }
}
</style>
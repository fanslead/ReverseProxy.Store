<template>
  <div>
    <Modal v-model="statusConfirm"
      title="更改状态"
      width="520"
      class-name="status-confirm"
      closable
      mask-closable
      @on-visible-change="visibleStatusChange"
    >
      <slot name="opt"></slot>
      <p><span>是否</span><span v-html="statusContent"></span></p>
      <slot name="tips"></slot>
      <div slot="footer">
        <Button type="text" @click="statusConfirm = false">{{cancelText}}</Button>
        <Button type="primary" @click="clickOk">{{okText}}</Button>
      </div>
    </Modal>
  </div>
</template>

<script>
export default {
  name: 'statusModal',
  props: {
    okText: {
      type: String,
      default: '提交'
    },
    cancelText: {
      type: String,
      default: '取消'
    },
    value: {
      type: Boolean,
      default: false
    },
    content: {
      type: String,
      default: ''
    }
  },
  watch: {
    value(newVal) {
      this.statusConfirm = newVal;
    },
    statusConfirm(newVal) {
      this.$emit('input', newVal);
    },
    content(newVal) {
      this.statusContent = newVal;
    }
  },
  data () {
    return {
      statusConfirm: false,
      statusContent: ''
    };
  },
  methods: {
    clickOk() {
      this.$emit("handleChangeStatus");
    },
    visibleStatusChange(visible) {
      this.$emit('isStatusVisible', visible);
    }
  }
};
</script>
<style lang="less">
.status-confirm{
  .ivu-modal{
    font-size: 16px;
    .ivu-modal-close{
      top: 8px;
    }
    .ivu-modal-header{
      height: 50px;
      padding: 0 16px;
      background: #FFFFFF;
      border-bottom: 1px solid #D8D8D8;
      .ivu-modal-header-inner{
        height: 50px;
        line-height: 50px;
      }
    }
    .ivu-modal-body{
      font-size: 16px;
      .img-wrap{
        text-align: center;
      }
      p{
        padding: 48px 0;
        margin: -16px 0;
        text-align: center;
        .name{
          color: #E96337;
          word-break: break-all;
          & > span{
            color: #515a6e;
          }
        }
      }
    }
    .ivu-modal-footer{
      text-align: center;
      .ivu-bt{
        width: 68px;
        height: 32px;
      }
      .ivu-btn-text{
        color: #666666;
        border: 1px solid #DADADA;
        margin-right: 12px;
        &:hover{
          color: #666666;
        }
      }
      .ivu-btn-primary{
        color: #FFFFFF;
        background: #202cbc;
        &:hover{
          color: #FFFFFF;
        }
      }
    }
  }
  .opt-row{
    margin-top: -36px;
  }
}
@media screen and (max-width: 1368px) {
  .status-confirm{
    .ivu-modal{
      .ivu-modal-body{
        font-size: 14px;
        p{
          padding: 30px 0;
        }
      }
    }
    .opt-row{
      margin-top: 0;
    }
  }
}
</style>

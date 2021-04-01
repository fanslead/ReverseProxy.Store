<template>
  <div>
    <Modal v-model="deleteConfirm"
      title="删除"
      width="520"
      class-name="del-confirm"
      closable
      mask-closable
      @on-visible-change="visibleDelChange"
    >
        <div>
          <div class="img-wrap"><img src="@/assets/images/common/delConfirm.png" alt="删除"></div>
          <p><span>是否</span><span v-html="delContent"></span></p>
          <div class="tip" v-if="delTip">{{delTip}}</div>
        </div>
        <div slot="footer">
          <Button type="text" @click="deleteConfirm = false">{{cancelText}}</Button>
          <Button type="primary" @click="clickOk">{{okText}}</Button>
        </div>
    </Modal>
  </div>
</template>

<script>
export default {
  name: 'deleteConfirm',
  props: {
    okText: {
      type: String,
      default: '确认'
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
      this.deleteConfirm = newVal;
    },
    deleteConfirm(newVal) {
      this.$emit('input', newVal);
    },
    content(newVal) {
      this.delContent = newVal;
    }
  },
  data () {
    return {
      deleteConfirm: false,
      delContent: '',
      delTip: ""
    };
  },
  methods: {
    clickOk() {
      this.$emit('handleChangeDelete');
    },
    visibleDelChange(visible) {
      this.$emit('isDelVisible', visible);
    }
  }
};
</script>
<style lang="less">
  .del-confirm{
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
        display: flex;
        justify-content: center;
        align-items: center;
        padding: 30px 0;
        font-size: 16px;
        .img-wrap{
          display: flex;
          justify-content: center;
          align-items: center;
          margin-bottom: 26px;
          img{
            max-width: 60px;
            max-height: 54px;
          }
        }
        p{
          padding: 0 20px;
          .name{
            color: #E96337;
            word-break: break-all;
          }
        }
        .tip {
          color: #999999;
          font-size: 12px;
          margin-top: 15px;
        }
      }
      .ivu-modal-footer{
        padding: 20px 18px;
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
  }
  @media screen and (max-width: 1368px){
    .del-confirm{
      .ivu-modal{
        .ivu-modal-body{
          padding: 16px 0;
        }
      }
    }
  }
</style>

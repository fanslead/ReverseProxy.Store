<template>
  <has-btn-wrap class="tow-col-layout">
    <template v-slot:btn>
      <slot name="btn"></slot>
    </template>
    <div class="common-box">
      <div class="left-col" :style="{width: this.leftWidth + 'px'}">
        <scrollView height="100%">
          <slot name="left"></slot>
        </scrollView>
      </div>
      <div class="right-col" :style="{paddingLeft: paddingLeft}">
        <div class="right-col-wrap" :class="{noPaddingLeft: !left}">
          <scrollView height="100%">
            <template v-slot:default>
              <slot name="right"></slot>
            </template>
            <template v-slot:page>
              <slot name="page"></slot>
            </template>
          </scrollView>
        </div>
      </div>
    </div>
    <slot></slot>
  </has-btn-wrap>
</template>
<script>
const defaultWidth = 200
const smallWidth = 150
export default {
  name: 'twoColLayout',
  props: {
    left: {
      type: Number,
      default: 200
    }
  },
  watch: {
    leftWidth(val) {
      if (typeof val !== 'number') {
        this.leftWidth = defaultWidth
      }
      if (val <= 0) {
        this.leftWidth = 0
      }
      this.getRightWidth()
    },
    isSmallScreen(val) {
      this.getRightWidth()
    }
  },
  methods: {
    getRightWidth() {
      if (this.leftWidth) {
        if (this.isSmallScreen) {
          this.leftWidth = smallWidth
        } else {
          this.leftWidth = defaultWidth
        }
      }
      this.paddingLeft = this.leftWidth + 'px'
    }
  },
  data() {
    return {
      leftWidth: this.left,
      paddingLeft: this.left + 'px'
    }
  },
  mounted() {
    this.$mutations.setOffsetWidth(document.documentElement.offsetWidth)
    this.getRightWidth()
  }
}
</script>
<style lang="less">
.tow-col-layout{
  .has-btn-box{
    padding-bottom: 0;
    .common-box{
      min-height: unset;
      height: 100%;
      & > div{
        height: 100%;
      }
    }
  }
  .left-col{
    width: 200px;
    background: #fff;
    box-shadow:0px 0px 10px 0px rgba(0, 0, 0, 0.1);
    float: left;
  }
  .left-col, .right-col{
    .vtm-scroll-view-content{
      .padding{
        padding: 10px;
      }
    }
  }
  .right-col{
    width: 100%;
    .vtm-scroll-view{
      box-shadow:0px 0px 10px 0px rgba(0, 0, 0, 0.1);
    }
    .vtm-scroll-view-content{
      & > div{
        height: 100%;
      }
    }
    .i-pages{
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      background: #fff;
      padding: 10px 0;
      border-top: 1px solid #D9D9D9;
      margin: 0;
      z-index: 100;
    }
  }
  .right-col-wrap{
    height: 100%;
    padding-left: 16px;
    &.noPaddingLeft{
      padding-left: 0;
    }
  }
  .padding-for-page{
    .content-wrap{
      padding: 0 0 64px;
    }
  }
}
@media screen and (max-width: 1441px) {
  .tow-col-layout{
    .right-col-wrap{
      padding-left: 10px;
    }
    .right-col{
      .i-pages{
        padding: 5px 0;
      }
    }
    .left-col, .right-col{
      .vtm-scroll-view-content{
        .padding{
          padding: 5px;
        }
      }
    }
    .ivu-page-total, .ivu-page-options-elevator{
      height: 24px;
      line-height: 24px;
    }
    .ivu-page-item-jump-next, .ivu-page-item-jump-prev, .ivu-page-next, .ivu-page-prev, .ivu-page-item{
      min-width: 24px;
      height: 24px;
      line-height: 22px;
    }
    .i-pages{
      .ivu-select-single{
        .ivu-select-selection{
          height: 24px;
          border-radius: 3px;
          .ivu-select-selected-value{
            height: 22px;
            line-height: 22px;
            font-size: 12px;
          }
        }
      }
    }
    .ivu-page-options-elevator{
      input{
        width: 44px;
        height: 24px;
        border-radius: 3px;
        padding: 1px 7px;
      }
    }
    .padding-for-page{
      .content-wrap{
        padding: 0 0 40px;
      }
    }
  }
}
</style>

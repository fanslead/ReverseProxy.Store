<template>
  <div class="vtm-scroll-view" :class="{sureHeight: isUseClassName}" :style="styles">
    <slot name="page"></slot>
    <slot name="button"></slot>
    <div
      ref="scroll"
      class="vtm-scroll-view-content"
      data-infinite-wrapper="true">
      <slot></slot>
    </div>
  </div>
</template>
<script>
import 'perfect-scrollbar/css/perfect-scrollbar.css';
import PerfectScrollbar from 'perfect-scrollbar';

const baseHeight = document.documentElement.clientHeight;

// let barConstructor = null;
export default {
  name: 'scrollView',
  props: {
    // view 距离顶部的高度
    offsetHeight: {
      type: [String, Number],
      default: 0
    },
    height: [Number, String],
    heightout: String,
    isUseClassName: { // 是否使用 calc 计算高度
      type: Boolean,
      default: false
    },
    baseWrap: {
      type: Boolean,
      default: false
    }
  },
  data() {
    return {
      barConstructor: null
    };
  },
  computed: {
    styles() {
      let height = null;
      if (this.height) {
        if (typeof this.height == 'number') {
          height = `${this.height}px`;
        } else {
          height = this.height;
        }
      } else {
        height = `${baseHeight - this.offsetHeight}px`;
      }
      if (this.heightout) {
        height = this.heightout;
      }

      return {
        height: height
      };
    }
  },
  methods: {
    resetScroll() {
      this.$refs.scroll && (this.$refs.scroll.scrollTop = 0);
    }
  },
  mounted() {
    this.$nextTick().then(() => {
      this.barConstructor = new PerfectScrollbar(this.$refs.scroll, {
        // suppressScrollX: true
      });
    });
  },
  updated() {
    this.barConstructor && this.barConstructor.update();
  },
  destroyed() {
    this.barConstructor && this.barConstructor.destroy();
    this.barConstructor = null;
  }
};
</script>

<style lang="less">
.vtm-scroll-view {
  position: relative;
  overflow: hidden;
  &.sureHeight {
    height: calc(100% - 100px);
  }
  &-content {
    position: relative;
    height: inherit;
    overflow: hidden;
  }
}
@media screen and (max-width: 1368px) {
  .vtm-scroll-view {
    &.sureHeight {
      height: calc(100% - 70px) !important;
    }
  }
}
</style>

export default {
  computed: {
    offsetWidth() {
      return this.$stores.offsetWidth
    },
    isSmallScreen() {
      return this.$stores.offsetWidth < 1441
    }
  },
  methods: {
    getBaseWrap() {
      let view = this.$parent
      while(view) {
        if (view.$options.name != 'scrollView') {
          view = view.$parent
          continue
        }
        if (view.baseWrap) {
          break
        }
      }
      return view
    },
    resetBaseScrollView() {
      let view = this.getBaseWrap()
      view && view.resetScroll && view.resetScroll()
    },
    updateBaseScrollView() {
      let view = this.getBaseWrap()
      view && view.barConstructor && view.barConstructor.update()
    }
  }
}
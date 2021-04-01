import '@/assets/css'
import 'lodash'
// import 'view-design/dist/styles/iview.css'

import Vue from 'vue'
import App from './App.vue'
import router from './router'

import { store, mutations } from '@/store'

Vue.config.productionTip = false

import ViewUI from 'view-design'
Vue.use(ViewUI, {
  size: 'small'
})

import VDistpicker from 'v-distpicker'
Vue.component('v-distpicker', VDistpicker)

import scrollView from '@/components/common/scrollView'
Vue.component('scrollView', scrollView)

import hasBtnWrap from '@/views/common/components/hasBtnWrap'
Vue.component('hasBtnWrap', hasBtnWrap)

import commonWrap from '@/views/common/components/commonWrap'
Vue.component('commonWrap', commonWrap)

import twoColLayout from '@/views/common/components/twoColLayout'
Vue.component('twoColLayout', twoColLayout)

import deleteModal from '@/components/common/deleteModal'
Vue.component('deleteModal', deleteModal)

import statusModal from '@/components/common/statusModal'
Vue.component('statusModal', statusModal)

import collapse from '@/views/common/components/collapse'
Vue.component('collapse', collapse)

Vue.prototype.$stores = store
Vue.prototype.$mutations = mutations

const moment = require('moment')
Vue.prototype.$moment = moment

import base from '@/mixins/base'
Vue.mixin(base)


new Vue({
  router,
  render: h => h(App)
}).$mount('#app')

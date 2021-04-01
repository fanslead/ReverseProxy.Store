import Vue from 'vue'
import VueRouter from 'vue-router'
import merchant from './modules/merchant'

const originalPush = VueRouter.prototype.push
VueRouter.prototype.push = function push(location) {
  return originalPush.call(this, location).catch(err => err)
}

Vue.use(VueRouter)

const routes = [
  {
    path: '/',
    name: 'layout',
    component: () => import(/* webpackChunkName: "layout" */ '@/views/common/layout'),
    redirect: {
      name: 'upstreamList'
    },
    children: [
      ...merchant
    ]
  },
  {
		path: '*',
		redirect: '/'
	},
  {
    path: '/login',
    name: 'login',
    component: () => import(/* webpackChunkName: "login" */ '@/views/user/login'),
    meta: {
      title: '登录'
    }
  }
]

const router = new VueRouter({
  mode: 'hash',
  base: process.env.BASE_URL,
  routes
})

const noTokenList = ['upstreamList', "routeList", "login"]

router.beforeEach((to, from, next) => {
  // let title = (to.meta && to.meta.title) || ''
  // if (title) {
  //   document.title = title
  // }
  let isExt = noTokenList.some(item => {
    return to.name == item
  })
  if (to.name == 'home' && location.href.indexOf('TokenStr') > -1) {
    isExt = true
  }
  let token = localStorage.getItem('token')
  if (isExt || token) {
    next()
  } else {
    next()
    // next({
    //   path: '/login',
    //   query: {
    //     redirect: to.fullPath
    //   }
    // })
  }
})

export default router

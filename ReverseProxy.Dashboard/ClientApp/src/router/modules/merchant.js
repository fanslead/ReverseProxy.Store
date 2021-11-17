export default [
  {
    path: '/upstreamList',
    name: 'upstreamList',
    component: () => import(/* webpackChunkName: "brandManager" */ '@/views/upstream/list')
  },
  {
    path: '/routeList',
    name: 'routeList',
    component: () => import(/* webpackChunkName: "brandManager" */ '@/views/upstream/routeList')
  }
]

import { createRouter, createWebHistory } from 'vue-router'
import { useUserStore } from '@/stores/user'
import HomeView from '../views/HomeView.vue'
import LoginView from '../views/LoginView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
      meta: { requiresAuth: true },
    },
    {
      path: '/login',
      name: 'login',
      component: LoginView,
      meta: { guest: true },
    },
    {
      path: '/about',
      name: 'about',
      component: () => import('../views/AboutView.vue'),
      meta: { requiresAuth: true },
    },
  ],
})

// 路由守卫
router.beforeEach((to, from, next) => {
  const userStore = useUserStore()

  // 初始化认证状态
  userStore.initializeAuth()

  const isLoggedIn = userStore.isLoggedIn

  // 如果路由需要认证
  if (to.meta.requiresAuth && !isLoggedIn) {
    next('/login')
    return
  }

  // 如果已登录用户访问登录页面，重定向到首页
  if (to.meta.guest && isLoggedIn) {
    next('/')
    return
  }

  next()
})

export default router

/**
 * 全局配置信息， 包含网站标题，  动态组件定义
 *
 * @author terrfly
 * @site https://www.agpay.vip
 * @date 2021/5/8 07:18
 */

/** 应用配置项 **/
export default {
  APP_TITLE: 'AgPay代理商系统', // 设置浏览器title
  ACCESS_TOKEN_NAME: 'authorization' // 设置请求token的名字， 用于请求header 和 localstorage中存在名称
}

/**
 * 与后端开发人员的路由名称及配置项
 * 组件名称 ：{ 默认跳转路径（如果后端配置则已动态配置为准）， 组件渲染 }
 * */
export const asyncRouteDefine = {

  'CurrentUserInfo': { defaultPath: '/current/userinfo', component: () => import('@/views/current/UserinfoPage') }, // 用户设置

  'MainPage': { defaultPath: '/main', component: () => import('@/views/dashboard/Analysis') },
  'PayConfigPage': { defaultPath: '/passageConfig', component: () => import('@/views/account/PayConfigPage') },
  'StatisticsPage': { defaultPath: '/statistic', component: () => import('@/views/account/StatisticsPage') },
  'SysUserPage': { defaultPath: '/users', component: () => import('@/views/sysuser/SysUserPage') },
  'SysUserTeamPage': { defaultPath: '/teams', component: () => import('@/views/sysUserTeam/TeamList') },
  'RolePage': { defaultPath: '/roles', component: () => import('@/views/role/RolePage') },
  'AgentListPage': { defaultPath: '/agent', component: () => import('@/views/agent/AgentList') }, // 代理商列表
  'MchListPage': { defaultPath: '/mch', component: () => import('@/views/mch/MchList') }, // 商户列表
  'MchAppPage': { defaultPath: '/apps', component: () => import ('@/views/mchApp/List') }, // 商户应用列表
  'MchStorePage': { defaultPath: '/store', component: () => import ('@/views/mchStore/MchStoreList') }, // 商户门店列表
  'PayOrderListPage': { defaultPath: '/payOrder', component: () => import('@/views/order/pay/PayOrderList') }, // 支付订单列表
  'RefundOrderListPage': { defaultPath: '/refundOrder', component: () => import('@/views/order/refund/RefundOrderList') }, // 退款订单列表
  'AgentConfigPage': { defaultPath: '/config', component: () => import('@/views/config/AgentConfigPage') }, // 系统配置
  'NoticeInfoPage': { defaultPath: '/notices', component: () => import('@/views/notice/NoticeList') } // 公告管理
}

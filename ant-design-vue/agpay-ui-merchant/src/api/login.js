import request from '@/http/request'
import { Base64 } from 'js-base64'

// 登录认证接口
export function login ({ username, password, vercode, vercodeToken }) {
  const data = {
    ia: Base64.encode(username), // 账号
    ip: Base64.encode(password), // 密码
    vc: Base64.encode(vercode), // 验证码值
    vt: Base64.encode(vercodeToken) // 验证码token
  }
  return request.request({
    url: '/api/anon/auth/validate',
    method: 'post',
    data: data
  }, true, true, false)
}

// 获取图形验证码信息接口
export function vercode () {
  return request.request({ url: '/api/anon/auth/vercode', method: 'get' }, true, true, true)
}

// 注册接口
export function register ({ agentName, code, confirmPwd, phone }) {
  const data = {
    agentName: agentName, // 账号
    code: Base64.encode(code), // 验证码
    confirmPwd: Base64.encode(confirmPwd), // 验证码值
    phone: Base64.encode(phone) // 验证码token
  }
  return request.request({
    url: '/api/anon/register',
    method: 'post',
    data: data
  }, true, true, true)
}

// 获取条约接口
export function treaty () {
  return request.request({ url: '/api/anon/treaty', method: 'get' }, true, true, true)
}

// 找回密码接口
export function forget (data) {
  return request.request({
    url: '/api/anon/cipher/retrieve',
    method: 'post',
    data: data
  }, true, true, true)
}

// 发送短信验证码信息接口
export function sendcode (data) {
  return request.request({
    url: '/api/anon/sms/code',
    method: 'post',
    data: data
  }, true, true, true)
}

// 获取当前用户信息
export function getInfo () {
  return request.request({
    url: '/api/current/user',
    method: 'get'
  })
}

// 退出接口
export function logout () {
  return new Promise(resolve => { resolve() })
}

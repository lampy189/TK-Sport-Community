import { initializeApp, getApps } from 'firebase/app';
import { getMessaging, getToken, deleteToken, isSupported } from 'firebase/messaging';
import { request } from './api';

const config = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID,
};
const vapidKey = import.meta.env.VITE_FIREBASE_VAPID_KEY;
export const pushConfigured = Object.values(config).every(Boolean) && Boolean(vapidKey);
const messaging = () => getMessaging(getApps().find(a => a.name === 'tksc-push') || initializeApp(config, 'tksc-push'));

export async function enablePush(askPermission = true) {
  if (!pushConfigured) throw new Error('Chưa cấu hình Firebase cho môi trường này.');
  if (!await isSupported()) throw new Error('Trình duyệt này chưa hỗ trợ thông báo đẩy.');
  const permission = askPermission ? await Notification.requestPermission() : Notification.permission;
  if (permission !== 'granted') throw new Error('Hãy cho phép thông báo trong cài đặt trình duyệt.');
  const registration = await navigator.serviceWorker.register('/firebase-messaging-sw.js');
  await navigator.serviceWorker.ready;
  const token = await getToken(messaging(), { vapidKey, serviceWorkerRegistration: registration });
  const response = await request('/api/notifications/device', { method: 'PUT', body: JSON.stringify({ token }) });
  if (!response.ok) throw new Error('Chưa đăng ký được thiết bị. Hãy thử lại.');
  localStorage.setItem('tksc-push-token', token);
  localStorage.setItem('tksc-push-enabled', 'true');
}

export async function disablePush() {
  localStorage.removeItem('tksc-push-enabled');
  const token = localStorage.getItem('tksc-push-token');
  if (token) {
    const response = await request('/api/notifications/device', { method: 'DELETE', body: JSON.stringify({ token }) });
    if (!response.ok) throw new Error('Chưa hủy được đăng ký thông báo.');
    localStorage.removeItem('tksc-push-token');
  }
  if (pushConfigured && await isSupported()) await deleteToken(messaging());
}

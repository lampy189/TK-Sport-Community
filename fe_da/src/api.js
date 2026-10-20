// API Client for TKSC Backend
let cachedCsrfToken = null;

export async function getCsrfToken(forceFresh = false) {
  if (!forceFresh && cachedCsrfToken) return cachedCsrfToken;
  try {
    const res = await fetch('/api/auth/csrf', {
      credentials: 'same-origin',
      cache: 'no-store'
    });
    if (res.ok) {
      const text = await res.text();
      const data = text ? JSON.parse(text) : {};
      cachedCsrfToken = data.token || null;
      return cachedCsrfToken;
    }
  } catch (err) {
    console.error('Failed to get CSRF token', err);
  }
  return null;
}

export function clearCsrfToken() {
  cachedCsrfToken = null;
}

export async function request(path, options = {}) {
  const method = (options.method || 'GET').toUpperCase();
  const headers = { ...options.headers };

  if (['POST', 'PUT', 'DELETE', 'PATCH'].includes(method)) {
    const token = await getCsrfToken();
    if (token) {
      headers['X-CSRF-TOKEN'] = token;
    }
    if (!headers['Content-Type'] && options.body && typeof options.body === 'string') {
      headers['Content-Type'] = 'application/json';
    }
  }

  let res = await fetch(path, {
    ...options,
    headers,
    credentials: 'same-origin',
  });

  // Handle 400 with invalid_csrf_token by retrying once
  if (res.status === 400) {
    const clone = res.clone();
    try {
      const text = await clone.text();
      const errorJson = text ? JSON.parse(text) : {};
      if (errorJson.code === 'invalid_csrf_token') {
        clearCsrfToken();
        const freshToken = await getCsrfToken(true);
        if (freshToken) {
          headers['X-CSRF-TOKEN'] = freshToken;
        }
        res = await fetch(path, {
          ...options,
          headers,
          credentials: 'same-origin',
        });
      }
    } catch {
      // not JSON, return original
    }
  }

  return res;
}

// Auth API
export const api = {
  async getMe() {
    const res = await request('/api/auth/me');
    if (!res.ok) return null;
    try {
      const text = await res.text();
      return text ? JSON.parse(text) : null;
    } catch {
      return null;
    }
  },

  async devLogin(email) {
    const res = await request('/api/auth/dev-login', {
      method: 'POST',
      body: JSON.stringify({ email }),
    });
    const text = await res.text();
    let data;
    try {
      data = text ? JSON.parse(text) : {};
    } catch {
      data = { message: text };
    }
    if (!res.ok) {
      if (res.status === 429) {
        throw new Error('Thao tác quá nhanh. Hệ thống giới hạn 30 request/phút. Vui lòng chờ 5-10 giây rồi thử lại.');
      }
      if (res.status === 403) {
        throw new Error('Tài khoản này không có quyền đăng nhập hoặc đã bị vô hiệu hóa.');
      }
      if (data.code === 'user_not_found') {
        throw new Error(data.message || `Không tìm thấy tài khoản với email: ${email}`);
      }
      if (data.code === 'invalid_csrf_token') {
        throw new Error('Phiên bảo mật CSRF chưa đồng bộ. Vui lòng bấm thử lại một lần nữa.');
      }
      throw new Error(data.message || data.code || `Lỗi đăng nhập (HTTP ${res.status})`);
    }
    clearCsrfToken();
    return data;
  },

  async logout() {
    const res = await request('/api/auth/logout', { method: 'POST' });
    clearCsrfToken();
    try {
      await getCsrfToken(true);
    } catch {}
    return res.ok;
  },

  async googleLogin() {
    try {
      const csrf = await getCsrfToken();
      if (!csrf) {
        throw new Error('Không thể lấy mã CSRF token. Hãy chắc chắn Backend đang chạy.');
      }
      const form = document.createElement('form');
      form.method = 'POST';
      form.action = '/api/auth/google';
      
      const inputCsrf = document.createElement('input');
      inputCsrf.type = 'hidden';
      inputCsrf.name = '__RequestVerificationToken';
      inputCsrf.value = csrf;
      form.appendChild(inputCsrf);

      const inputSource = document.createElement('input');
      inputSource.type = 'hidden';
      inputSource.name = 'source';
      inputSource.value = 'fe';
      form.appendChild(inputSource);

      document.body.appendChild(form);
      form.submit();
    } catch (err) {
      console.error('Google login error:', err);
      alert(err.message || 'Lỗi khi khởi động luồng đăng nhập Google.');
    }
  },

  // Sessions API
  async getSessions(params = {}) {
    const query = new URLSearchParams();
    if (params.sportId) query.set('sportId', params.sportId);
    if (params.sport) query.set('sport', params.sport);
    if (params.date) query.set('date', params.date);
    if (params.timeSlotId) query.set('timeSlotId', params.timeSlotId);
    if (params.status) query.set('status', params.status);

    const queryString = query.toString();
    const url = '/api/sessions' + (queryString ? `?${queryString}` : '');
    const res = await request(url);
    if (!res.ok) {
      throw new Error(`Failed to load sessions: HTTP ${res.status}`);
    }
    return res.json();
  },

  async getSessionById(id) {
    const res = await request(`/api/sessions/${id}`);
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw err;
    }
    return res.json();
  },

  async registerSession(sessionId) {
    const res = await request(`/api/sessions/${sessionId}/register`, {
      method: 'POST'
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      // Throw backend structured error { code, message, reason }
      throw data;
    }
    return data;
  }
};

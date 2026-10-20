// API Client for TKSC Backend
let cachedCsrfToken = null;

export async function getCsrfToken() {
  if (cachedCsrfToken) return cachedCsrfToken;
  try {
    const res = await fetch('/api/auth/csrf', {
      credentials: 'same-origin',
      cache: 'no-store'
    });
    if (res.ok) {
      const data = await res.json();
      cachedCsrfToken = data.token;
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

  const res = await fetch(path, {
    ...options,
    headers,
    credentials: 'same-origin',
  });

  // Handle 400 with invalid_csrf_token by retrying once
  if (res.status === 400) {
    const clone = res.clone();
    try {
      const errorJson = await clone.json();
      if (errorJson.code === 'invalid_csrf_token') {
        clearCsrfToken();
        const freshToken = await getCsrfToken();
        headers['X-CSRF-TOKEN'] = freshToken;
        return fetch(path, {
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
    return res.json();
  },

  async devLogin(email) {
    const res = await request('/api/auth/dev-login', {
      method: 'POST',
      body: JSON.stringify({ email }),
    });
    const data = await res.json();
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  async logout() {
    const res = await request('/api/auth/logout', { method: 'POST' });
    clearCsrfToken();
    return res.ok;
  },

  googleLogin() {
    window.location.href = '/api/auth/google';
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

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
<<<<<<< HEAD
=======
  async getCenterOptions() {
    const res = await request('/api/auth/centers');
    if (!res.ok) throw new Error('Không tải được danh sách cơ sở.');
    return res.json();
  },
  async selectCenter(venueId) {
    const res = await request('/api/auth/center', { method: 'POST', body: JSON.stringify({ venueId }) });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) throw new Error(data.message || 'Không thể chuyển cơ sở.');
    clearCsrfToken();
    return data;
  },
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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

<<<<<<< HEAD
  async devLogin(email, portal) {
    const res = await request('/api/auth/dev-login', {
      method: 'POST',
      body: JSON.stringify({ email, portal }),
=======
  async devLogin(email, portal, venueId) {
    const res = await request('/api/auth/dev-login', {
      method: 'POST',
      body: JSON.stringify({ email, portal, venueId: venueId ? Number(venueId) : null }),
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
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
        throw new Error(data.message || 'Tài khoản này không có quyền đăng nhập hoặc đã bị vô hiệu hóa.');
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
    } catch {
      /* Ignore CSRF refresh failure on logout */
    }
    return res.ok;
  },

<<<<<<< HEAD
  async googleLogin(portal) {
=======
  async googleLogin(portal, venueId) {
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
    try {
      const csrf = await getCsrfToken();
      if (!csrf) {
        throw new Error('Không thể lấy mã CSRF token. Hãy chắc chắn Backend đang chạy.');
      }
      const form = document.createElement('form');
      form.method = 'POST';
      form.action = '/api/auth/google' + (portal ? '?portal=' + encodeURIComponent(portal) : '');
<<<<<<< HEAD
=======
      if (venueId) form.action += (portal ? '&' : '?') + 'venueId=' + encodeURIComponent(venueId);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
      
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
  async getTimeSlots() {
    const res = await request('/api/sessions/time-slots');
    if (!res.ok) {
      throw new Error(`Failed to load time slots: HTTP ${res.status}`);
    }
    return res.json();
  },

<<<<<<< HEAD
=======
  async getVenues() {
    const res = await request('/api/sessions/venues');
    if (!res.ok) {
      throw new Error(`Failed to load venues: HTTP ${res.status}`);
    }
    return res.json();
  },

>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
  async getSessions(params = {}) {
    const query = new URLSearchParams();
    if (params.sportId) query.set('sportId', params.sportId);
    if (params.sport) query.set('sport', params.sport);
    if (params.date) query.set('date', params.date);
    if (params.fromDate) query.set('fromDate', params.fromDate);
    if (params.toDate) query.set('toDate', params.toDate);
    if (params.timeSlotId) query.set('timeSlotId', params.timeSlotId);
    if (params.status) query.set('status', params.status);
<<<<<<< HEAD
=======
    if (params.venueId) query.set('venueId', params.venueId);
    if (params.venue) query.set('venue', params.venue);
    if (params.sessionType) query.set('sessionType', params.sessionType);
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c

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
      throw data;
    }
    return data;
  },

  // Skill Profile & Self-Assessment API (UC-04, UC-05, UC-19, SWL-2)
  async getSkillRubrics(sportId) {
    const query = sportId ? `?sportId=${encodeURIComponent(sportId)}` : '';
    const res = await request(`/api/skills/rubrics${query}`);
    if (!res.ok) {
      throw new Error(`Failed to load skill rubrics: HTTP ${res.status}`);
    }
    return res.json();
  },

  async getMySkillProfiles() {
    const res = await request('/api/skills/me');
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw err;
    }
    return res.json();
  },

  // SWL 2: LamNV's Official Referee Published Skill Results API (/api/v1/skill/my-results)
  async getMyOfficialSkillResults(sportId = 1) {
    const res = await request(`/api/v1/skill/my-results?sportId=${encodeURIComponent(sportId)}`);
    if (!res.ok) {
      return [];
    }
    return res.json().catch(() => []);
  },

  async submitSkillSurvey(payload) {
    const res = await request('/api/skills/me/survey', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  // Personal & Public Player Profile API (UC-03 & UC-05)
  async getMyProfile() {
    const res = await request('/api/profile/me');
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw err;
    }
    return res.json();
  },

  async updateMyProfile(payload) {
    const res = await request('/api/profile/me', {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  async getPublicPlayerProfile(playerId) {
    const res = await request(`/api/profile/players/${encodeURIComponent(playerId)}`);
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  // Community, Friendships & Direct Chat Messaging API (UC-25c / UC-23 / UC-24)
  async searchCommunityMembers(query = '', limit = 40) {
    const params = new URLSearchParams();
    if (query) params.set('query', query);
    if (limit) params.set('limit', String(limit));
    const qs = params.toString();
    const res = await request(`/api/community/members${qs ? `?${qs}` : ''}`);
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw err;
    }
    return res.json();
  },

  async getFriends() {
    const res = await request('/api/community/friends');
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw err;
    }
    return res.json();
  },

  async getFriendSuggestions() {
    const res = await request('/api/community/friends/suggestions');
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw err;
    }
    return res.json();
  },

  async getFriendRequests() {
    const res = await request('/api/community/friends/requests');
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw err;
    }
    return res.json();
  },

  async sendFriendRequest(targetPlayerId) {
    const res = await request('/api/community/friends/request', {
      method: 'POST',
      body: JSON.stringify({ targetPlayerId })
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  async respondFriendRequest(friendshipId, accept) {
    const res = await request(`/api/community/friends/${encodeURIComponent(friendshipId)}/respond`, {
      method: 'POST',
      body: JSON.stringify({ accept })
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  async removeFriend(friendPlayerId) {
    const res = await request(`/api/community/friends/${encodeURIComponent(friendPlayerId)}`, {
      method: 'DELETE'
    });
    if (!res.ok && res.status !== 204) {
      const data = await res.json().catch(() => ({}));
      throw data;
    }
    return true;
  },

  async getConversation(friendPlayerId, limit = 60) {
    const res = await request(`/api/community/messages/${encodeURIComponent(friendPlayerId)}?limit=${encodeURIComponent(limit)}`);
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  async sendMessage(recipientPlayerId, content) {
    const res = await request('/api/community/messages', {
      method: 'POST',
      body: JSON.stringify({ recipientPlayerId, content })
    });
    const data = await res.json().catch(() => ({}));
    if (!res.ok) {
      throw data;
    }
    return data;
  },

  async markMessagesAsRead(friendPlayerId) {
    const res = await request(`/api/community/messages/${encodeURIComponent(friendPlayerId)}/read`, {
      method: 'POST'
    });
    return res.ok;
  }
};

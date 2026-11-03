import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  Pencil,
  Award,
  Calendar,
  User as UserIcon,
  Mail,
  FileText,
  Check,
  X,
  ArrowUpRight
} from 'lucide-react';
import { api } from '../api';
import '../styles/community.css';

function getSportItem(sportProfiles, code) {
  const found = (sportProfiles || []).find((s) => s.sportCode?.toLowerCase() === code.toLowerCase());
  return (
    found || {
      sportCode: code,
      sportName: code === 'badminton' ? 'Cầu lông' : 'Pickleball',
      currentBand: 'Unrated',
      currentScore: null,
      ratingStatus: 'Unrated'
    }
  );
}

function formatGenderLabel(g) {
  if (!g) return 'Chưa cập nhật';
  const low = g.toLowerCase();
  if (low === 'male' || low === 'nam') return 'Nam';
  if (low === 'female' || low === 'nữ' || low === 'nu') return 'Nữ';
  return 'Khác';
}

function formatStatusVi(status) {
  if (!status) return 'Chưa đánh giá';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'Chính thức · Trọng tài chấm';
  if (s === 'provisional') return 'Tạm tính · Khảo sát ban đầu';
  return 'Chưa đánh giá';
}

function getStatusPillClass(status) {
  if (!status) return 'unrated';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'confirmed';
  if (s === 'provisional') return 'provisional';
  return 'unrated';
}

function getStatusShortLabel(status) {
  if (!status) return 'Chưa chấm';
  const s = status.toLowerCase();
  if (s === 'confirmed' || s === 'rated') return 'Chính thức';
  if (s === 'provisional') return 'Tạm tính';
  return 'Chưa chấm';
}

function resizeImageFileToDataUrl(file, maxSize = 320) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const img = new Image();
      img.onload = () => {
        let width = img.width;
        let height = img.height;
        if (width > height && width > maxSize) {
          height = Math.round((height * maxSize) / width);
          width = maxSize;
        } else if (height >= width && height > maxSize) {
          width = Math.round((width * maxSize) / height);
          height = maxSize;
        }
        const canvas = document.createElement('canvas');
        canvas.width = width;
        canvas.height = height;
        const ctx = canvas.getContext('2d');
        ctx.drawImage(img, 0, 0, width, height);
        resolve(canvas.toDataURL('image/jpeg', 0.88));
      };
      img.onerror = () => reject(new Error('Không thể đọc file ảnh.'));
      img.src = reader.result;
    };
    reader.onerror = () => reject(new Error('Không thể đọc file ảnh.'));
    reader.readAsDataURL(file);
  });
}

export default function PersonalProfileView({ user, onNavigateSkill }) {
  const [myProfile, setMyProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [isEditing, setIsEditing] = useState(false);
  const [editForm, setEditForm] = useState({
    fullName: '',
    dateOfBirth: '',
    gender: '',
    avatarUrl: '',
    bio: ''
  });
  const [saving, setSaving] = useState(false);
  const [uploadingAvatar, setUploadingAvatar] = useState(false);
  const [feedback, setFeedback] = useState(null);
  const fileInputRef = useRef(null);

  const loadMyProfile = useCallback(async () => {
    setLoading(true);
    try {
      const data = await api.getMyProfile();
      setMyProfile(data);
      setEditForm({
        fullName: data.fullName || '',
        dateOfBirth: data.dateOfBirth || '',
        gender: data.gender || '',
        avatarUrl: data.avatarUrl || '',
        bio: data.bio || ''
      });
    } catch (err) {
      setFeedback({ type: 'error', text: err.message || 'Không thể tải hồ sơ cá nhân.' });
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadMyProfile();
  }, [loadMyProfile]);

  const handleAvatarFileSelected = async (e) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      setFeedback({ type: 'error', text: 'Vui lòng chọn một file hình ảnh hợp lệ (JPG, PNG, WEBP).' });
      return;
    }

    setUploadingAvatar(true);
    setFeedback(null);
    try {
      const dataUrl = await resizeImageFileToDataUrl(file, 320);
      setEditForm((prev) => ({ ...prev, avatarUrl: dataUrl }));

      const currentName = (isEditing ? editForm.fullName : myProfile?.fullName || user?.fullName || '').trim();
      if (currentName) {
        const updated = await api.updateMyProfile({
          fullName: currentName,
          dateOfBirth: (isEditing ? editForm.dateOfBirth : myProfile?.dateOfBirth) || null,
          gender: (isEditing ? editForm.gender : myProfile?.gender) || null,
          avatarUrl: dataUrl,
          bio: isEditing ? editForm.bio : myProfile?.bio || ''
        });
        setMyProfile(updated);
        setEditForm((prev) => ({
          ...prev,
          avatarUrl: updated.avatarUrl || dataUrl
        }));
        window.dispatchEvent(new CustomEvent('tksc:profile-updated', { detail: updated }));
        setFeedback({ type: 'success', text: 'Đã cập nhật ảnh đại diện mới.' });
      }
    } catch (err) {
      setFeedback({ type: 'error', text: err.message || 'Không thể tải ảnh đại diện lên.' });
    } finally {
      setUploadingAvatar(false);
    }
  };

  const handleOpenEdit = () => {
    if (myProfile) {
      setEditForm({
        fullName: myProfile.fullName || '',
        dateOfBirth: myProfile.dateOfBirth || '',
        gender: myProfile.gender || '',
        avatarUrl: myProfile.avatarUrl || '',
        bio: myProfile.bio || ''
      });
    }
    setFeedback(null);
    setIsEditing(true);
  };

  const handleSave = async (e) => {
    e.preventDefault();
    setSaving(true);
    setFeedback(null);
    try {
      const updated = await api.updateMyProfile({
        fullName: editForm.fullName.trim(),
        dateOfBirth: editForm.dateOfBirth || null,
        gender: editForm.gender || null,
        avatarUrl: editForm.avatarUrl || null,
        bio: editForm.bio
      });
      setMyProfile(updated);
      setIsEditing(false);
      window.dispatchEvent(new CustomEvent('tksc:profile-updated', { detail: updated }));
      setFeedback({ type: 'success', text: 'Đã lưu thông tin Hồ sơ cá nhân.' });
    } catch (err) {
      setFeedback({
        type: 'error',
        text: err.message || 'Không thể cập nhật hồ sơ cá nhân. Vui lòng kiểm tra lại thông tin.'
      });
    } finally {
      setSaving(false);
    }
  };

  const myBadminton = getSportItem(myProfile?.sportProfiles, 'badminton');
  const myPickleball = getSportItem(myProfile?.sportProfiles, 'pickleball');
  const currentAvatar = (isEditing ? editForm.avatarUrl : myProfile?.avatarUrl) || myProfile?.avatarUrl;

  return (
    <div className="comm-container">
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        style={{ display: 'none' }}
        onChange={handleAvatarFileSelected}
      />

      {/* Editorial Personal Profile Header */}
      <div className="comm-light-hero">
        <div className="comm-my-profile-left">
          <div
            className="comm-avatar-lg comm-avatar-editable"
            title="Bấm để chọn ảnh đại diện từ máy tính"
            onClick={() => fileInputRef.current?.click()}
          >
            {currentAvatar ? (
              <img src={currentAvatar} alt={myProfile?.fullName || 'Avatar'} />
            ) : (
              (myProfile?.fullName || user?.fullName || 'P').charAt(0).toUpperCase()
            )}
            <div className="comm-avatar-edit-overlay">
              <Pencil size={16} />
              <span>{uploadingAvatar ? 'Đang tải...' : 'Đổi ảnh'}</span>
            </div>
          </div>

          <div>
            <span className="comm-eyebrow">HỒ SƠ THÀNH VIÊN TKSC</span>
            <h2>
              <span>{myProfile?.fullName || user?.fullName || 'Người chơi TKSC'}</span>
              <span
                style={{
                  fontSize: '11px',
                  fontWeight: 700,
                  background: '#f0f9f4',
                  color: '#1b6f50',
                  border: '1px solid #cce8d8',
                  padding: '3px 9px',
                  borderRadius: '999px'
                }}
              >
                {myProfile?.role || user?.role || 'Player'}
              </span>
            </h2>
            <div className="comm-my-meta">
              <span>
                <Mail size={13} /> {myProfile?.email || user?.email}
              </span>
              <span>
                <UserIcon size={13} /> Giới tính: <strong>{formatGenderLabel(myProfile?.gender)}</strong>
              </span>
              <span>
                <Calendar size={13} /> Ngày sinh: <strong>{myProfile?.dateOfBirth || 'Chưa cập nhật'}</strong>
              </span>
            </div>
            <p className="comm-my-bio">
              {myProfile?.bio ||
                'Chưa có lời giới thiệu cá nhân. Bấm "Sửa hồ sơ" để thêm lời giới thiệu về sở trường Cầu lông / Pickleball của bạn.'}
            </p>
          </div>
        </div>

        <div style={{ display: 'flex', gap: '10px', flexWrap: 'wrap', alignItems: 'center' }}>
          {!isEditing && (
            <button
              type="button"
              className="comm-btn comm-btn-primary"
              onClick={handleOpenEdit}
            >
              <Pencil size={14} />
              <span>Sửa hồ sơ</span>
            </button>
          )}
          {onNavigateSkill && (
            <button
              type="button"
              className="comm-btn comm-btn-outline"
              onClick={onNavigateSkill}
            >
              <Award size={14} />
              <span>Hồ sơ kỹ năng</span>
            </button>
          )}
        </div>
      </div>

      {feedback && (
        <div
          style={{
            padding: '12px 16px',
            borderRadius: '12px',
            fontSize: '13.5px',
            fontWeight: 600,
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            background: feedback.type === 'error' ? '#fef2f2' : '#f0f9f4',
            color: feedback.type === 'error' ? '#b91c1c' : '#14553d',
            border: `1px solid ${feedback.type === 'error' ? '#fecaca' : '#cce8d8'}`
          }}
        >
          <span>{feedback.text}</span>
          <button
            type="button"
            onClick={() => setFeedback(null)}
            style={{ background: 'transparent', border: 'none', cursor: 'pointer', color: 'inherit' }}
          >
            <X size={15} />
          </button>
        </div>
      )}

      {/* VIEW MODE */}
      {!isEditing ? (
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fit, minmax(340px, 1fr))',
            gap: '20px'
          }}
        >
          {/* Card 1: Thông tin cá nhân */}
          <div
            style={{
              background: '#ffffff',
              borderRadius: '18px',
              border: '1px solid #e1e8e3',
              padding: '24px',
              display: 'flex',
              flexDirection: 'column',
              gap: '16px'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h3 style={{ margin: 0, fontSize: '16px', fontWeight: 700, color: '#14261c' }}>
                Thông tin cá nhân
              </h3>
              <button
                type="button"
                className="comm-btn comm-btn-ghost"
                onClick={handleOpenEdit}
              >
                <Pencil size={13} />
                <span>Chỉnh sửa</span>
              </button>
            </div>

            {loading ? (
              <div style={{ color: '#55685d', fontSize: '13.5px' }}>Đang tải thông tin...</div>
            ) : (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
                {[
                  { icon: UserIcon, label: 'Họ và tên', value: myProfile?.fullName || user?.fullName },
                  { icon: Mail, label: 'Email đăng nhập', value: myProfile?.email || user?.email },
                  { icon: UserIcon, label: 'Giới tính', value: formatGenderLabel(myProfile?.gender) },
                  { icon: Calendar, label: 'Ngày sinh', value: myProfile?.dateOfBirth || 'Chưa cập nhật' }
                ].map(({ icon: Icon, label, value }) => (
                  <div
                    key={label}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      padding: '11px 14px',
                      borderRadius: '11px',
                      background: '#f7faf8',
                      border: '1px solid #e8efe9'
                    }}
                  >
                    <span style={{ fontSize: '13px', color: '#55685d', display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <Icon size={14} /> {label}
                    </span>
                    <strong style={{ fontSize: '13.5px', color: '#14261c' }}>{value}</strong>
                  </div>
                ))}

                <div
                  style={{
                    padding: '12px 14px',
                    borderRadius: '11px',
                    background: '#f7faf8',
                    border: '1px solid #e8efe9'
                  }}
                >
                  <div style={{ fontSize: '13px', color: '#55685d', display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '6px' }}>
                    <FileText size={14} /> Giới thiệu bản thân
                  </div>
                  <div style={{ fontSize: '13.5px', color: '#14261c', lineHeight: 1.5 }}>
                    {myProfile?.bio || 'Chưa cập nhật lời giới thiệu.'}
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Card 2: Trình độ Thể thao hiện tại */}
          <div
            style={{
              background: '#ffffff',
              borderRadius: '18px',
              border: '1px solid #e1e8e3',
              padding: '24px',
              display: 'flex',
              flexDirection: 'column',
              gap: '16px'
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h3 style={{ margin: 0, fontSize: '16px', fontWeight: 700, color: '#14261c' }}>
                Mức Band &amp; Điểm Kỹ năng
              </h3>
              {onNavigateSkill && (
                <button
                  type="button"
                  className="comm-btn comm-btn-ghost"
                  onClick={onNavigateSkill}
                >
                  <span>Chi tiết 5 tiêu chí</span>
                  <ArrowUpRight size={14} />
                </button>
              )}
            </div>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
              {[
                { isPickleball: false, title: 'Cầu lông (Badminton)', data: myBadminton },
                { isPickleball: true, title: 'Pickleball', data: myPickleball }
              ].map(({ isPickleball, title, data }) => (
                <div
                  key={data.sportCode}
                  style={{
                    padding: '16px 18px',
                    borderRadius: '14px',
                    background: isPickleball ? '#f8faff' : '#f7faf8',
                    border: `1px solid ${isPickleball ? '#e6eefc' : '#e8efe9'}`,
                    display: 'flex',
                    flexDirection: 'column',
                    gap: '8px'
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <strong
                      style={{
                        fontSize: '14px',
                        color: '#14261c',
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: '8px'
                      }}
                    >
                      <span className={`comm-dot ${isPickleball ? 'is-pickleball' : ''}`} />
                      {title}
                    </strong>
                    <span className={`comm-status-pill ${getStatusPillClass(data.ratingStatus)}`}>
                      {getStatusShortLabel(data.ratingStatus)}
                    </span>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', marginTop: '2px' }}>
                    <div>
                      <div style={{ fontSize: '11px', color: '#55685d', fontWeight: 600 }}>
                        Band hiện tại
                      </div>
                      <div
                        style={{
                          fontSize: '23px',
                          fontWeight: 800,
                          color: isPickleball ? '#1d4ed8' : '#1b6f50'
                        }}
                      >
                        {data.currentBand && data.currentBand !== 'Unrated' ? `Band ${data.currentBand}` : '—'}
                      </div>
                    </div>
                    <div style={{ textAlign: 'right' }}>
                      <div style={{ fontSize: '11px', color: '#55685d', fontWeight: 600 }}>
                        Điểm kỹ năng
                      </div>
                      <div style={{ fontSize: '19px', fontWeight: 800, color: '#14261c' }}>
                        {data.currentScore !== null && data.currentScore !== undefined ? `${data.currentScore} / 100` : '— / 100'}
                      </div>
                    </div>
                  </div>

                  <div style={{ fontSize: '12px', color: '#55685d' }}>
                    Nguồn: <strong>{formatStatusVi(data.ratingStatus)}</strong>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : (
        /* EDIT MODE */
        <div
          style={{
            background: '#ffffff',
            borderRadius: '18px',
            border: '1px solid #e1e8e3',
            padding: '24px 28px'
          }}
        >
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
            <div>
              <h3 style={{ margin: '0 0 4px', fontSize: '17px', fontWeight: 700, color: '#14261c' }}>
                Chỉnh sửa Hồ sơ cá nhân
              </h3>
              <p style={{ margin: 0, fontSize: '13px', color: '#55685d' }}>
                Để đổi ảnh đại diện, đưa chuột vào vòng tròn ảnh phía trên và bấm chọn file ảnh từ máy tính.
              </p>
            </div>
            <button
              type="button"
              className="comm-btn comm-btn-outline"
              onClick={() => setIsEditing(false)}
            >
              <X size={14} /> Đóng
            </button>
          </div>

          <form onSubmit={handleSave}>
            <div className="comm-form-grid">
              <div className="comm-form-field full">
                <label>Họ và tên hiển thị *</label>
                <input
                  type="text"
                  required
                  value={editForm.fullName}
                  onChange={(e) => setEditForm((f) => ({ ...f, fullName: e.target.value }))}
                  placeholder="Nhập họ và tên của bạn"
                />
              </div>

              <div className="comm-form-field">
                <label>Giới tính</label>
                <select
                  value={editForm.gender}
                  onChange={(e) => setEditForm((f) => ({ ...f, gender: e.target.value }))}
                >
                  <option value="">-- Chọn giới tính --</option>
                  <option value="Male">Nam</option>
                  <option value="Female">Nữ</option>
                  <option value="Other">Khác</option>
                </select>
              </div>

              <div className="comm-form-field">
                <label>Ngày sinh</label>
                <input
                  type="date"
                  value={editForm.dateOfBirth}
                  onChange={(e) => setEditForm((f) => ({ ...f, dateOfBirth: e.target.value }))}
                />
              </div>

              <div className="comm-form-field full">
                <label>Giới thiệu bản thân</label>
                <textarea
                  rows={3}
                  maxLength={500}
                  value={editForm.bio}
                  onChange={(e) => setEditForm((f) => ({ ...f, bio: e.target.value }))}
                  placeholder="Ví dụ: Thuận tay phải, thích đánh đôi cầu lông Band B3 và giao lưu Pickleball cuối tuần..."
                />
              </div>
            </div>

            <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px', marginTop: '20px' }}>
              <button
                type="button"
                className="comm-btn comm-btn-outline"
                onClick={() => setIsEditing(false)}
              >
                Hủy
              </button>
              <button type="submit" className="comm-btn comm-btn-primary" disabled={saving}>
                <Check size={14} />
                <span>{saving ? 'Đang lưu...' : 'Lưu thay đổi'}</span>
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

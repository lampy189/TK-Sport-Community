import { useEffect, useState } from 'react';
import { api } from '../api';

export function useCenterOptions() {
  const [state, setState] = useState({ loading: true, enabled: false, centers: [], error: '' });
  useEffect(() => {
    let active = true;
    api.getCenterOptions().then(data => {
      if (active) setState({ ...data, loading: false, error: '' });
    }).catch(() => {
      if (active) setState({ loading: false, enabled: false, centers: [], error: 'Chưa tải được danh sách cơ sở. Vui lòng tải lại trang.' });
    });
    return () => { active = false; };
  }, []);
  return state;
}

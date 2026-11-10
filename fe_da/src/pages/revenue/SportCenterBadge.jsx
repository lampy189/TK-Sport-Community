import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';

export default function SportCenterBadge() {
  const [text, setText] = useState('');
  useEffect(() => {
    let active = true;
    workflowRequest('/api/v1/sport-centers').then(scope => {
      if (active) setText(scope.canViewAll ? 'Tổng bộ' : scope.centers.find(c => c.id === scope.assignedVenueId)?.name || 'Chưa được phân công Sport Center');
    }).catch(() => { if (active) setText('Chưa tải được Sport Center'); });
    return () => { active = false; };
  }, []);
  if (!text) return null;
  return <p style={{ margin: '16px 32px 0', fontSize: 13, color: '#53645b' }}><strong>Sport Center: </strong>{text}</p>;
}

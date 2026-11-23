import { useEffect, useState } from 'react';
import { workflowRequest } from '../workflowClient';

export default function SportCenterBadge({ compact = false }) {
  const [text, setText] = useState('');
  useEffect(() => {
    let active = true;
    workflowRequest('/api/v1/sport-centers').then(scope => {
      if (active) setText(scope.canViewAll && !scope.selectedVenueId ? 'Tổng bộ' : scope.centers.find(c => c.id === (scope.selectedVenueId || scope.assignedVenueId))?.name || 'Chưa được phân công Sport Center');
    }).catch(() => { if (active) setText('Chưa tải được Sport Center'); });
    return () => { active = false; };
  }, []);
  if (!text) return null;
  return <p className={compact ? 'manager-center-label' : undefined} style={compact ? undefined : { margin: '16px 32px 0', fontSize: 13, color: '#53645b' }}><strong>{compact ? 'Cơ sở: ' : 'Sport Center: '}</strong>{text}</p>;
}

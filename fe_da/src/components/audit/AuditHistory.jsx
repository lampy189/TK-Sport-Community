import PaginatedList from '../ui/PaginatedList';
import { auditFields, auditText, describeAudit } from './auditPresentation';
import './audit-history.css';
function Fields({ fields }) {
  return <dl className="audit-fields">{fields.map((field, i) => <div key={i}><dt>{field.label}</dt><dd>{field.value}</dd></div>)}</dl>;
}
export function AuditDetails({ value }) {
  const fields = auditFields(value);
  return fields.length ? <Fields fields={fields}/> : <p className="audit-text">{auditText(value)}</p>;
}
export default function AuditHistory({ items = [], people = [] }) {
  if (!items.length) return <p>Chưa có lịch sử xử lý.</p>;
  return <div className="audit-history"><PaginatedList items={items} label="Phân trang lịch sử xử lý">{(entry, index) => {
    const detail = describeAudit(entry);
    const person = people.find(p => String(p.id) === String(entry.actorId));
    const actor = person?.name || (entry.actorId == null ? 'Hệ thống' : `Người dùng #${entry.actorId}`);
    return <article className="audit-entry" key={entry.id ?? index}><h4>{detail.title}</h4><p className="audit-meta">{actor}{detail.at && <> · <time>{detail.at}</time></>}</p><Fields fields={detail.fields}/></article>;
  }}</PaginatedList></div>;
}

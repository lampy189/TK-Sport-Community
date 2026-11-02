import { useEffect, useRef, useState } from 'react';
import { paymentClient } from './paymentClient';
import { Camera, Upload, ScanLine, Ticket, UserRound, MapPin, CalendarDays, CircleCheck, ArrowRight, LoaderCircle, AlertCircle } from 'lucide-react';
import SectionLayout from '../../components/layout/SectionLayout';
import './checkin.css';

export default function CheckInPage() {
  const [code,setCode]=useState('');
  const [ticket,setTicket]=useState(null);
  const [error,setError]=useState('');
  const [busy,setBusy]=useState(false);
  const [scanning,setScanning]=useState(false);
  const [verified,setVerified]=useState(false);
  const video=useRef(null);
  const controls=useRef(null);
  const generation=useRef(0);
  const busyRef=useRef(false);
  useEffect(()=>()=>{generation.current++; controls.current?.stop();},[]);
  function stop() { generation.current++; controls.current?.stop(); controls.current=null; setScanning(false); }
  async function inspect(value) {
    if(busyRef.current) return;
    busyRef.current=true; setBusy(true); setError(''); setTicket(null); setVerified(false);
    try { setTicket(await paymentClient.inspectTicket(value)); }
    catch(e) { setError(e.message); }
    finally { busyRef.current=false; setBusy(false); }
  }
  async function scan() {
    stop(); setError(''); setTicket(null); setVerified(false); setScanning(true);
    const current=generation.current;
    try {
      if(!navigator.mediaDevices?.getUserMedia) throw new Error('Camera cần HTTPS hoặc localhost. Bạn có thể tải ảnh QR hoặc nhập mã vé.');
      const {BrowserQRCodeReader}=await import('@zxing/browser');
      if(current!==generation.current) return;
      const reader=new BrowserQRCodeReader();
      let found=false;
      const scanner=await reader.decodeFromConstraints({video:{facingMode:{ideal:'environment'}},audio:false},video.current,(result,_error,scannerControls)=>{
        if(!result || found || current!==generation.current) return;
        found=true; scannerControls.stop(); stop(); setCode(result.getText()); void inspect(result.getText());
      });
      if(current!==generation.current) scanner.stop(); else controls.current=scanner;
    } catch(e) { if(current===generation.current) {stop();setError(e.name==='NotAllowedError'?'Chưa được cấp quyền camera. Hãy cho phép camera, tải ảnh QR hoặc nhập mã vé.':e.message);} }
  }
  async function upload(file) {
    if(!file || busyRef.current) return;
    stop(); setError(''); setTicket(null); setVerified(false);
    busyRef.current=true; setBusy(true);
    const current=generation.current;
    const url=URL.createObjectURL(file);
    let decoded;
    try {
      const {BrowserQRCodeReader}=await import('@zxing/browser');
      const result=await new BrowserQRCodeReader().decodeFromImageUrl(url);
      decoded=result.getText();
    } catch { if(current===generation.current) setError('Không đọc được QR trong ảnh. Hãy chọn ảnh rõ nét hoặc nhập mã vé.'); }
    finally { URL.revokeObjectURL(url); busyRef.current=false; setBusy(false); }
    if(decoded && current===generation.current) { setCode(decoded); await inspect(decoded); }
  }
  async function confirm() {
    if(busyRef.current || !ticket || !verified) return;
    busyRef.current=true;setBusy(true);setError('');
    try {setTicket(await paymentClient.checkIn(ticket.ticketCode));}
    catch(e){setTicket(null);setVerified(false);setError(e.message);}
    finally{busyRef.current=false;setBusy(false);}
  }
  const step = ticket?.checkedIn ? 3 : ticket ? 2 : 1;
  const date = ticket?.sessionDate?.split('-').reverse().join('/');
  return (
    <SectionLayout className="checkin-workspace" title="Điểm danh vào sân"
      description="Kiểm tra vé và đối chiếu người chơi trước khi xác nhận vào sân.">
      <ol className="checkin-steps" aria-label="Quy trình điểm danh">
        {['Kiểm tra vé', 'Đối chiếu người chơi', 'Xác nhận vào sân'].map((label, index) => (
          <li key={label} className={step >= index + 1 ? 'is-active' : ''} aria-current={step === index + 1 ? 'step' : undefined}>
            <span>{step > index + 1 ? <CircleCheck size={16} aria-hidden="true" /> : index + 1}</span>{label}
          </li>
        ))}
      </ol>
      {error && <div role="alert" className="checkin-alert"><AlertCircle size={20} aria-hidden="true" /><p>{error}</p></div>}
      <div className="checkin-grid">
        <section className="checkin-panel" aria-labelledby="checkin-scan-title">
          <div className="checkin-panel-heading"><span className="checkin-icon"><ScanLine size={22} aria-hidden="true" /></span>
            <div><h3 id="checkin-scan-title">Quét vé người chơi</h3><p>Sử dụng mã QR hoặc mã vé đã được cấp.</p></div>
          </div>
          <div className={`checkin-viewfinder ${scanning ? 'is-scanning' : ''}`}>
            <video ref={video} hidden={!scanning} muted playsInline aria-label="Camera quét QR" />
            {!scanning && <div className="checkin-camera-placeholder"><ScanLine size={48} strokeWidth={1.3} aria-hidden="true" />
              <strong>Sẵn sàng quét mã QR</strong><p>Đưa mã trên vé vào giữa khung hình.</p></div>}
          </div>
          <div className="checkin-scan-actions">
            <button type="button" className="button button-primary" disabled={busy} onClick={scanning ? stop : scan}>
              <Camera size={18} aria-hidden="true" />{scanning ? 'Dừng camera' : 'Mở camera'}</button>
            <label className={`button button-secondary checkin-upload ${busy || scanning ? 'is-disabled' : ''}`}>
              <Upload size={18} aria-hidden="true" />Tải ảnh QR
              <input type="file" aria-label="Tải ảnh mã QR của vé" accept="image/*" disabled={busy || scanning}
                onChange={e => { void upload(e.target.files?.[0]); e.target.value = ''; }} />
            </label>
          </div>
          <div className="checkin-divider"><span>hoặc nhập mã vé</span></div>
          <form className="checkin-code-form" onSubmit={e => { e.preventDefault(); stop(); void inspect(code.trim()); }}>
            <label htmlFor="checkin-code">Mã vé</label>
            <input id="checkin-code" autoComplete="off" spellCheck={false} required maxLength={100} value={code}
              placeholder="Nhập mã trên vé người chơi" disabled={busy || scanning}
              onChange={e => { setCode(e.target.value); setTicket(null); setVerified(false); }} />
            <button className="button button-secondary" disabled={busy || scanning || !code.trim()}>
              {busy ? <LoaderCircle className="checkin-spinner" size={18} aria-hidden="true" /> : <Ticket size={18} aria-hidden="true" />}
              {busy ? 'Đang xử lý…' : 'Kiểm tra vé'}</button>
          </form>
        </section>
        <section className="checkin-panel checkin-result" aria-labelledby="checkin-result-title" aria-busy={busy}>
          <div className="checkin-panel-heading"><span className="checkin-icon"><UserRound size={22} aria-hidden="true" /></span>
            <div><h3 id="checkin-result-title">Thông tin vào sân</h3><p>Đối chiếu thông tin với người đang có mặt.</p></div>
          </div>
          <div aria-live="polite" className="checkin-result-body">
            {busy ? <div className="checkin-empty" role="status"><LoaderCircle size={32} className="checkin-spinner" aria-hidden="true" />
              <h4>Đang xử lý vé</h4><p>Vui lòng đợi trong giây lát.</p></div> : ticket ? <>
              <span className={`checkin-status ${ticket.checkedIn ? 'is-complete' : ''}`}><CircleCheck size={15} aria-hidden="true" />
                {ticket.checkedIn ? 'Đã điểm danh' : 'Vé hợp lệ · Chưa điểm danh'}</span>
              <h4 className="checkin-player">{ticket.holderName}</h4>
              <dl className="checkin-details">
                <div><dt><MapPin size={16} aria-hidden="true" />Sân chơi</dt><dd>{ticket.courtName}<span>{ticket.sportName}</span></dd></div>
                <div><dt><CalendarDays size={16} aria-hidden="true" />Thời gian</dt><dd>{date}<span>{ticket.startTime?.slice(0, 5)} – {ticket.endTime?.slice(0, 5)}</span></dd></div>
                <div><dt><Ticket size={16} aria-hidden="true" />Mã vé</dt><dd><code>{ticket.ticketCode}</code></dd></div>
              </dl>
              {ticket.checkedIn ? <div className="checkin-success"><CircleCheck size={20} aria-hidden="true" /><div><strong>Đã ghi nhận vào sân</strong>
                <p>Vé này không cần điểm danh lại. Bạn có thể tiếp tục kiểm tra vé tiếp theo.</p></div></div> : <>
                <label className="checkin-confirm-label"><input type="checkbox" checked={verified} onChange={e => setVerified(e.target.checked)} />
                  <span>Tôi đã đối chiếu <strong>đúng người chơi và đúng sân</strong>.</span></label>
                <button type="button" className="button button-primary checkin-confirm-button" disabled={!verified || busy} onClick={confirm}>
                  Xác nhận vào sân<ArrowRight size={18} aria-hidden="true" /></button>
              </>}
            </> : <div className="checkin-empty"><span className="checkin-empty-icon"><Ticket size={32} strokeWidth={1.4} aria-hidden="true" /></span>
              <h4>Chưa có vé được chọn</h4><p>Quét QR, tải ảnh hoặc nhập mã vé.<br />Thông tin người chơi sẽ hiển thị tại đây.</p></div>}
          </div>
        </section>
      </div>
    </SectionLayout>
  );
}
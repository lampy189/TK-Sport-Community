import { useEffect, useRef, useState } from 'react';
import { paymentClient } from './paymentClient';
import './payment.css';

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
  return <section className="payment-workspace"><div className="payment-heading"><h1>Check-in vào sân</h1></div><p>Quét QR trên vé, đối chiếu người chơi và sân trước khi xác nhận.</p>
    {error&&<p role="alert" className="payment-error">{error}</p>}
    <div className="checkout"><section className="payment-card"><h4>Quét vé người chơi</h4><div className="payment-actions"><button className="button button-primary" disabled={busy} onClick={scanning?stop:scan}>{scanning?'Dừng camera':'Mở camera quét QR'}</button><label className="button button-secondary">Chọn ảnh QR<input type="file" accept="image/*" disabled={busy||scanning} onChange={e=>{void upload(e.target.files?.[0]);e.target.value='';}}/></label></div>
    <video ref={video} className="checkin-camera" hidden={!scanning} muted playsInline aria-label="Camera quét QR"/>
    <form className="checkin-form" onSubmit={e=>{e.preventDefault();stop();void inspect(code.trim());}}><label htmlFor="checkin-code">Hoặc nhập mã vé</label><input id="checkin-code" autoComplete="off" required maxLength={100} value={code} disabled={busy||scanning} onChange={e=>{setCode(e.target.value);setTicket(null);setVerified(false);}}/><button className="button button-secondary" disabled={busy||scanning||!code.trim()}>Kiểm tra vé</button></form></section>
    <section className="payment-card" aria-live="polite">{busy?<p>Đang kiểm tra…</p>:ticket?<><span className={`payment-badge ${ticket.checkedIn?'confirmed':'pending'}`}>{ticket.checkedIn?'Đã check-in':'Vé hợp lệ — chưa check-in'}</span><h4>{ticket.holderName}</h4><p>{ticket.sportName} · {ticket.courtName}</p><p>{ticket.sessionDate} · {ticket.startTime?.slice(0,5)} – {ticket.endTime?.slice(0,5)}</p><code className="ticket-code">{ticket.ticketCode}</code>{ticket.checkedIn?<p className="payment-notice">Vé đã được ghi nhận vào sân. Không ghi nhận thêm lần check-in.</p>:<><label className="checkin-verify"><input type="checkbox" checked={verified} onChange={e=>setVerified(e.target.checked)}/> Tôi đã đối chiếu đúng người chơi và đúng sân.</label><button className="button button-primary checkout-pay" disabled={!verified||busy} onClick={confirm}>Xác nhận vào sân</button></>}</>:<p>Thông tin người chơi, sân và thời gian sẽ xuất hiện sau khi quét vé.</p>}</section></div>
  </section>;
}

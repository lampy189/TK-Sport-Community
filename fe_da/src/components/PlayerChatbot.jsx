import { useEffect, useRef, useState } from 'react';
import { BookOpen, MessageCircle, Send, X } from 'lucide-react';
import { request } from '../api';
import './player-chatbot.css';


export default function PlayerChatbot({ preview = false }) {
  const [open, setOpen] = useState(false);
  const [question, setQuestion] = useState('');
  const [messages, setMessages] = useState([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const controller = useRef(null);
  const input = useRef(null);
  const launcher = useRef(null);
  const log = useRef(null);
  useEffect(() => () => controller.current?.abort(), []);
  useEffect(() => {
    const closeAssistant = () => setOpen(false);
    window.addEventListener('tksc:close-assistant', closeAssistant);
    return () => window.removeEventListener('tksc:close-assistant', closeAssistant);
  }, []);
  useEffect(() => { if (open) input.current?.focus(); }, [open]);
  useEffect(() => { if (log.current) log.current.scrollTop = log.current.scrollHeight; }, [messages, busy, open]);

  function close() { setOpen(false); launcher.current?.focus(); }
  function toggle() {
    if (open) { close(); return; }
    window.dispatchEvent(new Event('tksc:close-chat'));
    setOpen(true);
  }
  async function ask(text) {
    const value = text.trim();
    if (controller.current || value.length < 2 || value.length > 1200) return;
    if (preview) { setError('Đăng nhập bằng tài khoản Player để hỏi trợ lý.'); return; }
    const abort = new AbortController();
    controller.current = abort;
    setBusy(true); setError(''); setQuestion('');
    setMessages(previous => [...previous.slice(-18), { role: 'user', text: value }]);
    const timeout = setTimeout(() => abort.abort(), 50000);
    try {
      const response = await request('/api/player/chatbot/ask', {
        method: 'POST', signal: abort.signal, body: JSON.stringify({ question: value }),
      });
      if (!response.ok) throw new Error(response.status === 429 ? 'Bạn hỏi hơi nhanh. Vui lòng chờ một phút rồi thử lại.'
        : response.status === 401 || response.status === 403 ? 'Vui lòng đăng nhập lại bằng tài khoản Player.'
          : 'Chưa gửi được câu hỏi. Vui lòng thử lại.');
      const answer = await response.json();
      setMessages(previous => [...previous, { role: 'assistant', text: answer.answer, sources: answer.sources, mode: answer.mode, retrievedAt: answer.retrievedAt }]);
    } catch (err) {
      setError(err.name === 'AbortError' ? 'Yêu cầu đã hết thời gian chờ. Bạn có thể gửi lại câu hỏi.' : err.message);
      setQuestion(value);
    } finally {
      clearTimeout(timeout); controller.current = null; setBusy(false);
    }
  }

  return <aside className="player-chatbot" aria-label="Trợ lý TKSC">
    {open && <section className="player-chatbot-panel" aria-label="Hỏi trợ lý TKSC" onKeyDown={event => { if (event.key === 'Escape') close(); }}>
      <header className="player-chatbot-header"><div><strong>Trợ lý TKSC</strong><span>Hướng dẫn dành cho người chơi</span></div>
        <button type="button" onClick={close} aria-label="Đóng trợ lý"><X size={20} /></button></header>
      <div className="player-chatbot-log" ref={log} role="log" aria-live="polite" aria-relevant="additions">
        {messages.length === 0 && <div className="player-chatbot-welcome"><BookOpen size={24} /><h3>Bạn cần tìm hiểu điều gì?</h3><p>Tra cứu lịch, giá, chỗ còn lại và dữ liệu của bạn từ hệ thống. Bạn cứ nhập câu hỏi, tôi sẽ tìm thông tin phù hợp.</p></div>}
        {messages.map((message, index) => <article key={index} className={`player-chatbot-message ${message.role}`}>
          <strong>{message.role === 'user' ? 'Bạn' : 'Trợ lý TKSC'}</strong>
          {message.mode === 'reference' && <small>Trích dẫn tài liệu · AI tạm chưa sẵn sàng</small>}
          {message.mode === 'live' && <small>Dữ liệu hệ thống · đọc trực tiếp</small>}
          {message.mode === 'unavailable' && <small>Chưa kết nối được nguồn dữ liệu</small>}
          {message.retrievedAt && <small>Tra cứu lúc {new Date(message.retrievedAt).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' })} (giờ Việt Nam)</small>}
          <p>{message.text}</p>
          {message.sources?.length > 0 && <details><summary>Nguồn tham khảo ({message.sources.length})</summary>{message.sources.map(source => <div key={source.id} className="player-chatbot-source"><strong>{source.title}</strong><small>{source.reference}</small><p>{source.excerpt}</p></div>)}</details>}
        </article>)}
        {busy && <p role="status">Đang tìm tài liệu và soạn câu trả lời…</p>}
      </div>
      <form className="player-chatbot-form" onSubmit={event => { event.preventDefault(); ask(question); }}>
        {error && <p role="alert" className="player-chatbot-error">{error}</p>}
        <label htmlFor="player-chatbot-question">Câu hỏi của bạn</label>
        <div><input id="player-chatbot-question" ref={input} value={question} onChange={event => setQuestion(event.target.value)} maxLength={1200} placeholder="Nhập câu hỏi của bạn…" autoComplete="off" /><button type="submit" disabled={busy || question.trim().length < 2} aria-label="Gửi câu hỏi"><Send size={18} /></button></div>
        <small>Vé, đơn và hoàn tiền chỉ tra cứu theo tài khoản của bạn, không gửi sang OpenAI. Câu hỏi chính sách và nội dung công khai có thể gửi tới OpenAI. Không nhập dữ liệu nhạy cảm.</small>
      </form>
    </section>}
    <button ref={launcher} type="button" className="player-chatbot-launcher" aria-expanded={open} onClick={toggle}><MessageCircle size={21} /><span>Hỏi trợ lý</span></button>
  </aside>;
}

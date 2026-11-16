import { createRoot } from 'react-dom/client';
import SkillReviews from '../../src/pages/skill/SkillReviews';
import TicketOperations from '../../src/pages/payment/TicketOperations';
import OperationalExceptions from '../../src/pages/payment/OperationalExceptions';
import PaymentRecoveryPage from '../../src/pages/payment/PaymentRecoveryPage';
import TicketSupport from '../../src/pages/payment/TicketSupport';
import '../../src/index.css';
const screen = new URLSearchParams(location.search).get('screen');
createRoot(document.getElementById('root')).render(<main style={{maxWidth:1100,margin:'20px auto',padding:16}}>
  <p>KIỂM THỬ — dữ liệu giả lập API, không phải giao dịch thật</p>
  {screen === 'operations' ? <OperationalExceptions role="manager"/> : screen === 'recovery' ? <PaymentRecoveryPage role="manager"/> : screen === 'exception' ? <SkillReviews role="manager" kind="exceptions"/> :
    screen === 'appeal' ? <SkillReviews role="manager" kind="appeals"/> :
    screen === 'player' ? <TicketSupport ticketId={41} initialOpen/> :
    <TicketOperations role="manager" initialTab={screen === 'venue' ? 'sessions' : 'issues'}/>}
</main>);

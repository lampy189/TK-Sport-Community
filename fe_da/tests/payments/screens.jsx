import { createRoot } from 'react-dom/client';
import PaymentRecoveryPage from '../../src/pages/payment/PaymentRecoveryPage';
import GoodsPaymentPage from '../../src/pages/payment/GoodsPaymentPage';
import '../../src/index.css';
const screen=new URLSearchParams(location.search).get('screen');
createRoot(document.getElementById('root')).render(<main style={{maxWidth:1100,margin:'20px auto',padding:16}}>
 <p>KIỂM THỬ — dữ liệu giả lập, không phải giao dịch thật</p>
 {screen==='goods'?<GoodsPaymentPage initialOrderId={3}/>:<PaymentRecoveryPage/>}
</main>);

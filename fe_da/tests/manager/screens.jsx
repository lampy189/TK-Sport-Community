import { createRoot } from 'react-dom/client';
import HomePage from '../../src/pages/home/HomePage';
import '../../src/index.css';
createRoot(document.getElementById('root')).render(<HomePage role="manager" user={{fullName:'Manager test'}}/>);

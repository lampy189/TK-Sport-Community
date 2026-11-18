import { useState } from 'react';
import { createRoot } from 'react-dom/client';
import PaginatedList from '../../src/components/ui/PaginatedList';
export default function Fixture() {
 const [count,setCount]=useState(53);
 const [filter,setFilter]=useState(false);
 const items=Array.from({length:count},(_,id)=>({id:id+1})).filter(item=>!filter||item.id>50);
 return <><button onClick={()=>setFilter(!filter)}>Lọc</button><button onClick={()=>setCount(0)}>Xóa hết</button><button onClick={()=>setCount(52)}>Xóa cuối</button><section aria-label="Thẻ"><PaginatedList items={items}>{(item,index)=><article key={item.id}>Mục {item.id} · vị trí {index}</article>}</PaginatedList></section><table><tbody><PaginatedList items={items} columns={2}>{item=><tr key={item.id}><td>{item.id}</td><td>Nội dung</td></tr>}</PaginatedList></tbody></table></>;
}
createRoot(document.getElementById('root')).render(<Fixture/>);

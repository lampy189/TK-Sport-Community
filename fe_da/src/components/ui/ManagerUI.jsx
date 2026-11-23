import { useManagerUI } from './managerUiState';
import PaginatedList from './PaginatedList';
export function ManagerList({items = [], children, ...props}) {
 const manager = useManagerUI();
 return manager ? <PaginatedList items={items} {...props}>{children}</PaginatedList> : items.map(children);
}

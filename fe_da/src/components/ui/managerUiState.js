import { createContext, useContext } from 'react';
export const ManagerUIContext = createContext(false);
export const useManagerUI = () => useContext(ManagerUIContext);
export const displayDate = value => /^\d{4}-\d{2}-\d{2}$/.test(value || '') ? value.split('-').reverse().join('/') : value;
export const sportLabel = value => ({Badminton:'Cầu lông',Pickleball:'Pickleball'})[value] || value;
export const matchesText = (value, query) => String(value ?? '').normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/đ/g,'d').toLowerCase().includes(query.trim().normalize('NFD').replace(/[\u0300-\u036f]/g,'').replace(/đ/g,'d').toLowerCase());

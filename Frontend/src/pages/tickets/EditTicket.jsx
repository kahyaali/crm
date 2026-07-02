// src/pages/tickets/EditTicket.jsx
// Aslında TicketDetail içinde edit mode olduğu için 
// ayrı bir sayfaya gerek yok. Ama route olarak ekleyelim:

import { useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

export default function EditTicket() {
  const { id } = useParams();
  const navigate = useNavigate();
  
  useEffect(() => {
    // Detail sayfasına yönlendir (edit mode açık olarak)
    navigate(`/tickets/${id}`, { state: { editMode: true } });
  }, [id, navigate]);
  
  return null;
}
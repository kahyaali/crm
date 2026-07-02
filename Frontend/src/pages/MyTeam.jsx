import { useState, useEffect } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { Link } from 'react-router-dom';
import toast from 'react-hot-toast';
import api from '../services/api';

export default function MyTeam() {
  const { user } = useAuth();
  const [teamMembers, setTeamMembers] = useState([]);
  const [myInfo, setMyInfo] = useState(null);
  const [loading, setLoading] = useState(true);
  
  // Pagination
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const pageSize = 10;

  const isManager = user?.role === 'SystemAdmin' || user?.role === 'Admin' || user?.role === 'SatisMuduru';

  useEffect(() => {
    fetchMyTeam();
  }, [page, search]);

  const fetchMyTeam = async () => {
    try {
      setLoading(true);
      const response = await api.get('/Personels/my-team', {
        params: { page, pageSize, search }
      });
      setMyInfo(response.data.currentPersonel);
      setTeamMembers(response.data.teamMembers || []);
      setTotalCount(response.data.totalCount || 0);
      setTotalPages(response.data.totalPages || 1);
    } catch (error) {
      console.error('Takım yüklenemedi:', error);
      toast.error('Takım bilgileri yüklenemedi');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = () => {
    setSearch(searchInput);
    setPage(1);
  };

  const formatSalary = (salary, currency) => {
    if (!salary) return '-';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(salary);
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 dark:border-blue-400"></div>
      </div>
    );
  }

  return (
    <div className="p-6">
      {/* Başlık */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-800 dark:text-white">Takımım</h1>
        <p className="text-gray-500 dark:text-gray-400">Bağlı personellerim</p>
      </div>

      {/* Arama */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow p-4 mb-6">
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="Personel ara (isim, soyisim, email)..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
            className="flex-1 p-2 border rounded-lg dark:bg-gray-700 dark:border-gray-600 dark:text-white dark:placeholder-gray-400"
          />
          <button onClick={handleSearch} className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors">
            Ara
          </button>
        </div>
      </div>

      {/* Tablo */}
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead className="bg-gray-50 dark:bg-gray-700">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Personel</th>
                 <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Personel No</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Sicil No</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Email</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Pozisyon</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Departman</th>
                {isManager && <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">Maaş</th>}
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">İşe Başlama</th>
                <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-300 uppercase">İşlem</th>
              </tr>
            </thead>
            <tbody>
              {teamMembers.length === 0 ? (
                <tr>
                  <td colSpan={isManager ? 7 : 6} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                    Henüz bağlı personeliniz bulunmuyor
                  </td>
                </tr>
              ) : (
                teamMembers.map((member) => (
                  <tr key={member.id} className="border-t border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        {member.avatarUrl ? (
                          <img src={member.avatarUrl} className="w-8 h-8 rounded-full object-cover" />
                        ) : (
                          <div className="w-8 h-8 bg-blue-500 rounded-full flex items-center justify-center text-white text-sm">
                            {member.firstName?.charAt(0)}{member.lastName?.charAt(0)}
                          </div>
                        )}
                        <span className="text-gray-800 dark:text-white">{member.firstName} {member.lastName}</span>
                      </div>
                     </td>
                      <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{member.personnelNumber || '-'}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{member.registrationNumber || '-'}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{member.email}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{member.positionName || '-'}</td>
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{member.departmentName || '-'}</td>
                    {isManager && (
                      <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{formatSalary(member.salary, member.currency)}</td>
                    )}
                    <td className="px-6 py-4 text-gray-700 dark:text-gray-300">{member.hireDate?.split('T')[0] || '-'}</td>
                    <td className="px-6 py-4 text-center">
                      <Link
                        to={`/personel-detail/${member.id}`}
                        className="inline-flex items-center gap-1 px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-xs font-medium rounded-lg transition-colors"
                      >
                        👁️ Detay
                      </Link>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex justify-between items-center mt-4">
          <div className="text-sm text-gray-500 dark:text-gray-400">Toplam {totalCount} personel</div>
          <div className="flex gap-2">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors"
            >
              ◀
            </button>
            <span className="px-3 py-1 text-sm text-gray-700 dark:text-gray-300">Sayfa {page} / {totalPages}</span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="px-3 py-1 border rounded disabled:opacity-50 hover:bg-gray-100 dark:border-gray-600 dark:text-white dark:hover:bg-gray-700 transition-colors"
            >
              ▶
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
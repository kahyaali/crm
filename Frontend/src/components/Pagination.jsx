export default function Pagination({ currentPage, totalPages, onPageChange }) {
  const getPageNumbers = () => {
    const pages = [];
    const maxVisible = 5;
    let start = Math.max(1, currentPage - Math.floor(maxVisible / 2));
    let end = Math.min(totalPages, start + maxVisible - 1);
    
    if (end - start + 1 < maxVisible) {
      start = Math.max(1, end - maxVisible + 1);
    }
    
    for (let i = start; i <= end; i++) {
      pages.push(i);
    }
    return pages;
  };

  if (totalPages <= 1) return null;

  return (
    <div className="flex justify-center items-center gap-2 mt-6">
      <button
        onClick={() => onPageChange(currentPage - 1)}
        disabled={currentPage === 1}
        className="px-3 py-1 rounded border disabled:opacity-50 hover:bg-gray-100 dark:hover:bg-gray-700"
      >
        ◀ Önceki
      </button>
      
      {currentPage > 3 && (
        <>
          <button onClick={() => onPageChange(1)} className="px-3 py-1 rounded hover:bg-gray-100">1</button>
          <span className="px-2">...</span>
        </>
      )}
      
      {getPageNumbers().map(page => (
        <button
          key={page}
          onClick={() => onPageChange(page)}
          className={`px-3 py-1 rounded ${
            page === currentPage 
              ? 'bg-blue-600 text-white' 
              : 'hover:bg-gray-100 dark:hover:bg-gray-700'
          }`}
        >
          {page}
        </button>
      ))}
      
      {currentPage < totalPages - 2 && (
        <>
          <span className="px-2">...</span>
          <button onClick={() => onPageChange(totalPages)} className="px-3 py-1 rounded hover:bg-gray-100">{totalPages}</button>
        </>
      )}
      
      <button
        onClick={() => onPageChange(currentPage + 1)}
        disabled={currentPage === totalPages}
        className="px-3 py-1 rounded border disabled:opacity-50 hover:bg-gray-100"
      >
        Sonraki ▶
      </button>
    </div>
  );
}
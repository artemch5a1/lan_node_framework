import type { Book } from "../api/types";

export type BooksSectionProps = {
  baseUrl: string | null;
  books: Book[] | null;
  booksLoading: boolean;
  onFetchBooks: () => void;
};

export function BooksSection({ baseUrl, books, booksLoading, onFetchBooks }: BooksSectionProps) {
  return (
    <section className="card">
      <div className="row card-header-row">
        <h2>Книги</h2>
        <button
          type="button"
          className="btn-refresh"
          disabled={!baseUrl || booksLoading}
          onClick={() => void onFetchBooks()}
        >
          {booksLoading ? "Загрузка…" : "Получить книги"}
        </button>
      </div>
      {books === null && (
        <p className="muted">
          Нажмите кнопку, чтобы запросить <code>GET /api/Books</code>.
        </p>
      )}
      {books && books.length > 0 && (
        <table className="books-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>Название</th>
              <th>Автор</th>
              <th>Год</th>
            </tr>
          </thead>
          <tbody>
            {books.map((b) => (
              <tr key={b.id}>
                <td>{b.id}</td>
                <td>{b.title}</td>
                <td>{b.author}</td>
                <td>{b.yearPublished}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {books && books.length === 0 && <p className="muted">Список пуст.</p>}
    </section>
  );
}

import { AdminPanelContainer } from "../components/admin/AdminPanelContainer";
import { BooksSection } from "../components/BooksSection";
import { useBackendSession } from "../stores/BackendSessionStore";
import { useBooksStore } from "../stores/BooksStore";
import "../App.css";

export default function App() {
  const { baseUrl, backendLoadError } = useBackendSession();
  const { books, booksLoading, fetchBooks } = useBooksStore();

  return (
    <>
      <AdminPanelContainer />
      <main className="container">
        <h1>Тест API</h1>

        {!baseUrl && !backendLoadError && <p className="muted">Загрузка backend…</p>}

        <BooksSection
          baseUrl={baseUrl}
          books={books}
          booksLoading={booksLoading}
          onFetchBooks={fetchBooks}
        />
      </main>
    </>
  );
}

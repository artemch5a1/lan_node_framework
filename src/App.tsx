import { useEffect, useState } from "react";
import { invoke } from "@tauri-apps/api/core";
import { AdminPanel } from "./AdminPanel";
import "./App.css";

type Book = {
  id: number;
  title: string;
  author: string;
  yearPublished: number;
};

export default function App() {
  const [baseUrl, setBaseUrl] = useState<string | null>(null);
  const [backendLoadError, setBackendLoadError] = useState<string | null>(null);
  const [books, setBooks] = useState<Book[] | null>(null);
  const [booksLoading, setBooksLoading] = useState(false);
  const [booksError, setBooksError] = useState<string | null>(null);

  useEffect(() => {
    void invoke<string>("get_backend_base_url").then(setBaseUrl).catch((e) => {
      setBackendLoadError(String(e));
    });
  }, []);

  async function fetchBooks() {
    if (!baseUrl) return;
    setBooksLoading(true);
    setBooksError(null);
    try {
      const r = await fetch(`${baseUrl}/api/Books`);
      if (!r.ok) throw new Error(`книги: HTTP ${r.status}`);
      const data = (await r.json()) as Book[];
      setBooks(data);
    } catch (e) {
      setBooksError(String(e));
      setBooks(null);
    } finally {
      setBooksLoading(false);
    }
  }

  return (
    <>
      <AdminPanel baseUrl={baseUrl} backendLoadError={backendLoadError} />
      <main className="container">
        <h1>Тест API</h1>

        {!baseUrl && !backendLoadError && <p className="muted">Загрузка backend…</p>}
        {backendLoadError && <p className="error">{backendLoadError}</p>}

        <section className="card">
          <div className="row card-header-row">
            <h2>Книги</h2>
            <button
              type="button"
              className="btn-refresh"
              disabled={!baseUrl || booksLoading}
              onClick={() => void fetchBooks()}
            >
              {booksLoading ? "Загрузка…" : "Получить книги"}
            </button>
          </div>
          {booksError && <p className="error">{booksError}</p>}
          {books === null && !booksError && (
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
      </main>
    </>
  );
}

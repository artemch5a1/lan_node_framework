import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import { fetchBooksList } from "../api/booksApi";
import type { Book } from "../api/types";
import { useNotificationService } from "../services/NotificationServiceProvider";
import { useBackendSession } from "./BackendSessionStore";

type BooksStoreValue = {
  books: Book[] | null;
  booksLoading: boolean;
  fetchBooks: () => Promise<void>;
};

const BooksStoreContext = createContext<BooksStoreValue | null>(null);

export function BooksStoreProvider({ children }: { children: ReactNode }) {
  const { baseUrl } = useBackendSession();
  const notifications = useNotificationService();
  const [books, setBooks] = useState<Book[] | null>(null);
  const [booksLoading, setBooksLoading] = useState(false);

  const fetchBooks = useCallback(async () => {
    if (!baseUrl) return;
    setBooksLoading(true);
    try {
      const data = await fetchBooksList(baseUrl);
      setBooks(data);
    } catch (e) {
      notifications.showErrorFromUnknown(e);
      setBooks(null);
    } finally {
      setBooksLoading(false);
    }
  }, [baseUrl, notifications]);

  const value: BooksStoreValue = { books, booksLoading, fetchBooks };

  return <BooksStoreContext.Provider value={value}>{children}</BooksStoreContext.Provider>;
}

export function useBooksStore(): BooksStoreValue {
  const v = useContext(BooksStoreContext);
  if (!v) throw new Error("useBooksStore must be used within BooksStoreProvider");
  return v;
}

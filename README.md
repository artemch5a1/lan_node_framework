# OP-1-26 Non-standard Situations

Десктопное клиент-серверное приложение: **Tauri 2** (оболочка + запуск backend), **React 18** + **TypeScript** + **Vite** (интерфейс), **ASP.NET Core** (HTTP API и логика узла в LAN).

Backend публикуется как **self-contained single-file** и укладывается в ресурсы приложения, чтобы на целевой машине не требовался установленный .NET Runtime.

---

## Возможности интерфейса

- Запрос списка книг с локального API (`GET /api/Books`).
- **Админ-панель** (Ctrl+Shift и клавиша **Backquote** — на US-клавиатуре обычно над Tab, на русской часто совпадает с **Ё**): режим узла, конфигурация discovery, сканирование LAN и подключение к пиру; ошибки показываются **тостами**; при открытии панели основной контент затемняется полупрозрачной подложкой.

---

## Структура репозитория

| Путь | Назначение |
|------|------------|
| `src/` | Фронтенд: React-приложение |
| `src/app/` | Корень UI: провайдеры, страница `App` |
| `src/api/` | Вызовы HTTP и Tauri (`booksApi`, `netApi`, `tauriBackendApi`, типы DTO) |
| `src/stores/` | Состояние через React Context: сессия backend, книги, админка / сеть |
| `src/services/` | Сервис уведомлений и форматирование текстов ошибок |
| `src/components/` | Компоненты отрисовки (в т.ч. админ-панель: контейнер + «чистый» view) |
| `src/ui/` | Общие UI-обёртки (например, тосты) |
| `src/domain/` | Чистая доменная логика без React |
| `src-dotnet/` | Решение .NET: `Backend.API`, слои Application / Infrastructure |
| `src-tauri/` | Rust-оболочка Tauri, sidecar backend, конфиги bundle |

---

## Требования к окружению

- **Node.js** (LTS) и npm  
- **Rust** и зависимости [Tauri v2](https://v2.tauri.app/start/prerequisites/) для вашей ОС  
- **.NET SDK** (совместимый с `Backend.API.csproj`) — для разработки и публикации API

---

## Локальная разработка

### Вариант A: полное приложение (`tauri dev`)

1. Соберите или опубликуйте исполняемый файл API (пример под Windows):

   ```bash
   dotnet publish ./src-dotnet/Backend.API/Backend.API.csproj -c Debug -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./artifacts/backend-win
   ```

2. Укажите Tauri путь к этому файлу (перед `npm run tauri dev` в той же оболочке):

   ```bash
   set BACKEND_EXECUTABLE=C:\path\to\artifacts\backend-win\Backend.API.exe
   npm install
   npm run tauri dev
   ```

   На Linux/macOS используйте `export BACKEND_EXECUTABLE=/absolute/path/to/Backend.API`.

При старте оболочка поднимает свободный порт на `127.0.0.1`, передаёт его backend через переменную **`BACKEND_HTTP_BASE_URL`**, ждёт ответа **`GET /health`**, затем отдаёт базовый URL фронту через команду `get_backend_base_url`.

### Вариант B: только веб-UI (без Tauri)

```bash
npm install
npm run dev
```

Страница откроется на порту Vite (см. консоль). Запросы к API без отдельно поднятого backend и без `invoke` работать не будут — удобно для вёрстки и быстрых правок UI.

### Backend отдельно (отладка API)

```bash
dotnet run --project ./src-dotnet/Backend.API/Backend.API.csproj
```

По умолчанию URL можно задать через `BACKEND_HTTP_BASE_URL` или аргументы `--urls` (см. `Program.cs`).

---

## Сборка релиза (self-contained backend + Tauri)

Используйте скрипт под целевую ОС хоста (или CI с соответствующим toolchain).

### Windows (`win-x64`)

Удобно выполнять **на Windows** (или в CI с Windows + MSVC).

```bash
npm install
npm run build:win-x64
```

Выполняется: `vite build` → `dotnet publish` в `src-tauri/binaries/backend/win-x64/Backend.API.exe` → `tauri build` с конфигом `src-tauri/tauri.bundle.windows.json`.

**Артефакты:** `src-tauri/target/x86_64-pc-windows-msvc/release/bundle/`.

### Linux (`linux-x64`)

Удобно выполнять **на Linux** с зависимостями Tauri/WebView для вашего дистрибутива.

```bash
npm install
npm run build:linux-x64
```

**Артефакты:** `src-tauri/target/release/bundle/` (при сборке на хосте с дефолтным target).

### Полезные npm-скрипты

| Скрипт | Описание |
|--------|----------|
| `npm run dev` | Только Vite (фронт) |
| `npm run build:web` | `tsc` + production-сборка фронта в `dist/` |
| `npm run build:backend:win-x64` / `build:backend:linux-x64` | Публикация только backend в `src-tauri/binaries/...` |
| `npm run build:bundle:win-x64` / `build:bundle:linux-x64` | Фронт + backend под платформу |
| `npm run tauri` | CLI Tauri (аргументы передаются после `--`) |

### Примечания по сборке

- В `tauri.conf.json` перед сборкой Tauri вызывается **`npm run build:web`**; публикация .NET в bundle делается скриптами `build:bundle:*` / `build:win-x64` / `build:linux-x64`.
- Кросс-компиляция Rust (например Windows `.exe` с Linux-хоста) требует отдельной настройки toolchain; для Windows-инсталлятора проще хост или CI под Windows.

---

## Переменные окружения (оболочка / backend)

| Переменная | Назначение |
|------------|------------|
| `BACKEND_EXECUTABLE` | Абсолютный путь к `Backend.API` при **разработке** (если нет встроенного bundle) |
| `BACKEND_HTTP_BASE_URL` | Базовый HTTP URL, на котором должен слушать дочерний процесс (задаёт Tauri при spawn) |
| `LOCAL_HTTP_BASE` | Альтернатива для разрешения URL в `Backend.API` (`Program.cs`) |

---

## API (кратко)

Помимо минимальных маршрутов `GET /health` и `GET /greet`, приложение использует контроллеры ASP.NET Core, в том числе:

- `GET/PUT /api/net/...` — роль, статус, конфигурация discovery, LAN-пиры, отключение;
- `GET /api/Books` — тестовый список книг.

В режиме Development доступен **Swagger**: `/swagger`.

---

## Лицензия и продукт

Проект помечен как `private` в `package.json`. При необходимости добавьте файл лицензии и обновите этот раздел.

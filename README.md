# LAN Node Framework (`lan_node_framework`)

Шаблон **локального клиент-серверного** десктоп-приложения: оболочка **Tauri 2** поднимает рядом **ASP.NET Core** API на `127.0.0.1`, фронтенд **React 18 + TypeScript + Vite** ходит к нему по HTTP и забирает базовый URL через `invoke`. Репозиторий и файл решения Visual Studio: **`lan_node_framework.sln`**.

Этот репозиторий не «готовый продукт», а **стартовая точка**: слои уже разделены, есть пример доменного API и UI для сетевого узла в LAN — дальше вы подставляете свою бизнес-логику и брендинг.

---

## Для чего шаблон

- Один установочный пакет: **веб-UI + встроенный backend** без отдельной установки .NET на машине пользователя (публикация **self-contained single-file**).
- Работа **только в локальной сети / на localhost**: типичный сценарий — киоск, цех, лаборатория, офлайн-контур, где сервер крутится на той же машине или рядом в LAN.
- Готовая связка **оболочка ↔ дочерний процесс**: свободный порт, переменная `BACKEND_HTTP_BASE_URL`, ожидание `GET /health`, корректное завершение процесса при выходе.

---

## Специфика шаблона (что уже заложено)

| Область | Что сделано |
|--------|-------------|
| **Связка Tauri ↔ .NET** | Поиск `Backend.API` через `BACKEND_EXECUTABLE`, bundle в `src-tauri/binaries/backend/...`, резервный путь рядом с `.exe`; передача базового URL в процесс и опрос здоровья перед стартом UI. |
| **Фронтенд** | Слои `api` / `stores` (Context) / `services` (уведомления) / `components` + `ui` (тосты); пример запроса книг и админ-панели. |
| **Backend** | Слои `Backend.*` + вынесенный пакет **DistributedLocalSystem** (discovery, beacon, LAN, прокси клиент↔хост); контроллеры `Books`, `Net`; Swagger в Development. |
| **Сеть** | API вида `/api/net/...` (роль, статус, конфигурация, пиры, disconnect); в UI — вкладки и тосты об ошибках, подложка при открытой админ-панели. |
| **Сборка** | Скрипты `build:win-x64` / `build:linux-x64` и merge-конфиги Tauri только с нужным бинарником платформы. |

Что **намеренно остаётся за вами** при форке: `identifier` и `productName` в `src-tauri/tauri.conf.json`, имя npm-пакета в `package.json`, идентификаторы в сторах/логах, политика безопасности (CSP, capabilities Tauri), реальная авторизация и контракты API под ваш продукт.

---

## Структура репозитория

| Путь | Назначение |
|------|------------|
| `lan_node_framework.sln` | Решение Visual Studio (.NET + тесты) |
| `src/` | React: `app/`, `api/`, `stores/`, `services/`, `components/`, `ui/`, `domain/` |
| `src-dotnet/` | `Backend.API` и слои приложения |
| `DistributedLocalSystem/` | Общая подсистема discovery / LAN |
| `src-tauri/` | Rust, `tauri.conf.json`, bundle backend |

---

## Возможности демо-UI

- Запрос списка книг: `GET /api/Books`.
- **Админ-панель** (Ctrl+Shift и клавиша **Backquote**): режим узла, конфигурация discovery, LAN, подключение к пиру; ошибки — **тосты**; подложка на основной контент при открытой панели.

---

## Требования к окружению

- **Node.js** (LTS), npm  
- **Rust** и зависимости [Tauri v2](https://v2.tauri.app/start/prerequisites/)  
- **.NET SDK** (совместимый с `Backend.API.csproj`)

---

## Локальная разработка

### Полное приложение (`tauri dev`)

1. Соберите backend (пример, Windows):

   ```bash
   dotnet publish ./src-dotnet/Backend.API/Backend.API.csproj -c Debug -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o ./artifacts/backend-win
   ```

2. Укажите путь к исполняемому файлу и запустите Tauri:

   ```bash
   set BACKEND_EXECUTABLE=C:\path\to\artifacts\backend-win\Backend.API.exe
   npm install
   npm run tauri dev
   ```

   Linux/macOS: `export BACKEND_EXECUTABLE=/absolute/path/to/Backend.API`.

### Только фронт (Vite)

```bash
npm install
npm run dev
```

Без поднятого API и без `invoke` часть функций недоступна.

### Только API

```bash
dotnet run --project ./src-dotnet/Backend.API/Backend.API.csproj
```

---

## Сборка релиза

### Windows (`win-x64`)

```bash
npm install
npm run build:win-x64
```

Артефакты: `src-tauri/target/x86_64-pc-windows-msvc/release/bundle/`.

### Linux (`linux-x64`)

```bash
npm install
npm run build:linux-x64
```

Артефакты: `src-tauri/target/release/bundle/` (на хосте с дефолтным target).

### Скрипты npm

| Скрипт | Описание |
|--------|----------|
| `npm run dev` | Vite |
| `npm run build:web` | `tsc` + сборка в `dist/` |
| `npm run build:backend:win-x64` / `build:backend:linux-x64` | Публикация backend в `src-tauri/binaries/...` |
| `npm run build:bundle:*` | Фронт + backend под платформу |
| `npm run tauri` | CLI Tauri |

Перед `tauri build` вызывается `npm run build:web`; публикация .NET в ресурсы — через `build:bundle:*` или готовые `build:win-x64` / `build:linux-x64`.

---

## Переменные окружения

| Переменная | Назначение |
|------------|------------|
| `BACKEND_EXECUTABLE` | Путь к `Backend.API` в разработке |
| `BACKEND_HTTP_BASE_URL` | Базовый URL, который оболочка передаёт дочернему процессу |
| `LOCAL_HTTP_BASE` | Альтернатива разрешения URL в `Backend.API` (`Program.cs`) |

---

## API (кратко)

- `GET /health`, `GET /greet`
- `GET/PUT /api/net/...` — сеть и discovery
- `GET /api/Books` — демо-список

В Development: **Swagger** — `/swagger`.

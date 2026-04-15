# OP-1-26_Non-standard_Situations

ОП 1 26. Нестандартные ситуации. Клиент-серверное приложение

## Сборка (self-contained .NET backend + Tauri)

Backend публикуется как self-contained single-file и кладётся в ресурсы приложения, чтобы на целевой машине не требовался установленный .NET Runtime.

Используйте отдельную команду под целевую ОС.

### Windows (`win-x64`)

Сборку удобно выполнять **на Windows** (или в CI с Windows + MSVC toolchain).

```bash
npm install
npm run build:win-x64
```

- фронтенд (`vite build`);
- `dotnet publish` → `src-tauri/binaries/backend/win-x64/Backend.API.exe`;
- `tauri build --target x86_64-pc-windows-msvc` + merge-конфиг `src-tauri/tauri.bundle.windows.json` (только win-бэкенд в bundle).

Артефакты: `src-tauri/target/x86_64-pc-windows-msvc/release/bundle/`.

### Linux (`linux-x64`)

Сборку удобно выполнять **на Linux** с зависимостями Tauri/WebView для вашего дистрибутива.

```bash
npm install
npm run build:linux-x64
```

- фронтенд;
- `dotnet publish` → `src-tauri/binaries/backend/linux-x64/Backend.API`;
- `tauri build` + merge-конфиг `src-tauri/tauri.bundle.linux.json`.

Артефакты: `src-tauri/target/release/bundle/` (при дефолтном таргете хоста).

### Примечания

- `tauri.conf.json` вызывает перед сборкой только `npm run build:web`; публикация backend выполняется скриптами `build:bundle:*` выше.
- Кросс-компиляция Rust (например Windows `.exe` с Linux-хоста) требует отдельной настройки MSVC/линкера; для Windows-билда проще хост или CI под Windows.
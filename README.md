# ZapretGUI

Windows GUI для [zapret-discord-youtube](https://github.com/flowseal/zapret-discord-youtube) — обёртка над `service.bat` с удобным интерфейсом.

## Функционал

- **Управление сервисом** — установка, удаление, проверка статуса
- **Выбор стратегии** — список всех `.bat` файлов из папки zapret, установка нужной в один клик
- **Game Filter** — переключение режимов TCP/UDP фильтрации для игр
- **IPSet** — переключение режимов (ANY / NONE / LOADED)
- **Авто-обновление** — включение/выключение планировщика Windows
- **Обновление IPSet и hosts** — загрузка актуальных списков из репозитория
- **Диагностика** — 13 проверок: BFE, прокси, TCP Timestamps, Adguard, Killer, Intel Connectivity, Check Point, SmartByte, VPN, DoH DNS, hosts файл, конфликты WinDivert и других bypass-утилит
- **Тестирование стратегий** — автоматический прогон всех конфигов с HTTP + Ping тестами, определение лучшей стратегии
- **DPI-тесты** — проверка freeze-паттерна через curl (требует curl.exe)
- **Двуязычный интерфейс** — RU / EN, переключается кнопкой в шапке

## Требования

- Windows 10 / 11
- Права администратора
- [zapret-discord-youtube](https://github.com/flowseal/zapret-discord-youtube) — скачать и распаковать отдельно

## Использование

1. Скачать `ZapretGUI.exe` из [Releases](../../releases)
2. Запустить от имени администратора
3. Нажать «Обзор» и выбрать папку с zapret
4. Выбрать стратегию → «Установить сервис»

## Сборка

```
dotnet publish ZapretGUI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

## Disclaimer

> Проект создан с помощью ИИ в экспериментальных целях.
> Официальная поддержка отсутствует — issues и PR не рассматриваются.
> Используйте на свой страх и риск.

## Лицензия

MIT — см. [LICENSE](LICENSE). Основан на [zapret-discord-youtube](https://github.com/flowseal/zapret-discord-youtube) (MIT).

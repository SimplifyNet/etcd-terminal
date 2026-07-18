s# План разработки etcd-terminal

## Этап 1: Базовая структура проекта
- [x] Создать .NET 10 solution с тремя проектами (Domain, Infrastructure, Presentation)
- [x] Настроить csproj с необходимыми NuGet-пакетами
- [x] Настроить связи между проектами

## Этап 2: Domain Layer
- [x] Модели: EtcdConnectionConfig, EtcdKeyValue, EtcdUser, EtcdRole, EtcdPermission, PermissionType
- [x] Интерфейс IEtcdClient (CRUD ключей + управление пользователями/ролями/пермишенами)
- [x] Интерфейс IConnectionConfigRepository (загрузка/сохранение конфигурации инстансов)

## Этап 3: Infrastructure Layer
- [x] Реализация EtcdClientAdapter (адаптер для dotnet-etcd)
- [x] Реализация JsonBasedConfigRepository (чтение/запись config.json)
- [ ] Логирование через Simplify.Log
- [x] Обработка ошибок подключения (try-catch в адаптере, RpcException)

## Этап 4: Presentation Layer
- [x] Настройка DI-контейнера (Simplify.DI) с регистрациями
- [x] Program.cs — точка входа с инициализацией
- [x] InstanceSelectionScreen — выбор etcd инстанса при запуске
- [x] MainScreen — главное меню с навигацией
- [x] KeyBrowserScreen — просмотр ключей (tree view по префиксам)
- [x] KeySearchScreen — поиск по ключам и значениям
- [x] KeyCreateScreen — создание нового ключа
- [x] KeyEditScreen — редактирование ключа
- [x] UserManagementScreen — управление пользователями
- [x] RoleManagementScreen — управление ролями
- [x] PermissionViewScreen — просмотр пермишенов пользователей

## Этап 5: Конфигурация
- [x] config.json с примерами инстансов
- [x] Создание директории ~/.config/etcd-terminal/ при первом запуске (реализовано в JsonBasedConfigRepository при сохранении)
- [x] Валидация конфигурации (проверка при загрузке из файла и при добавлении через UI)

## Этап 6: Дополнительно
- [x] SSL/TLS настройка для подключения (через UseSsl + configureChannelOptions)
- [x] Обработка ошибок и переподключение
- [ ] Логирование операций
- [x] Документация в README
- [ ] Тестирование

## Этап 7: Полировка
- [x] Улучшение UI/UX (цвета, иконки, спиннеры Spectre.Console)
- [x] Подтверждение опасных операций (удаление ключей, пользователей)
- [ ] Статус-бар с информацией о подключении
- [ ] Обработка Ctrl+C

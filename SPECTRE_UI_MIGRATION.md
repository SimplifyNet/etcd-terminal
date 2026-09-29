# Миграция UI на Spectre.Console

## Назначение

Техническое задание для следующего исполнителя. Постоянные архитектурные правила находятся в `AGENTS.md`; здесь зафиксированы проверенное состояние проекта, ограничения библиотеки, порядок разработки и приёмка.

Цель: меньше собственного кода отрисовки и обслуживающих его тестов за счёт готовых компонентов Spectre, без потери поведения и визуального оформления. Перенос ручной геометрии из компонентов в Infrastructure сам по себе целью не является.

Документ переписан 2026-09-27. Все утверждения о состоянии проекта получены запуском сборки и тестов, ограничения Spectre — чтением исходников закреплённого тега `0.57.2`. Отдельно перечислено то, что проверить не удалось.

Рабочее дерево возвращено к `ee4aee2`; незакоммиченная попытка миграции удалена полностью. Разбор того, почему она не сложилась, сохранён в разделе 3 — эти выводы определяют порядок работы и не должны повторяться. Перед началом повторно проверить `git status` и перечитать `AGENTS.md`.

## 1. Состояние Дерева

Проверено на `ee4aee2`, дерево чистое. Два коммита над `cf92b61` меняют только документацию (`AGENTS.md`, эта спецификация). В `src` незакоммиченных изменений нет.

| Область | Фактическое состояние |
| --- | --- |
| `src/EtcdTerminal` | Общий core-проект: бизнес-контракты смешаны с 8 файлами `Terminal`, 2 `Theming`, 1 `Localization`. UI-контрактов нет вообще. Отсутствие NuGet-зависимостей не делает UI доменом. |
| `src/EtcdTerminal.App` | `Components`, `Engine`, `Screens`, тема, локализация, composition root. Вся отрисовка ручная. |
| `src/EtcdTerminal.Infrastructure` | etcd и хранилища; `Terminal`: `ConsoleTerminal`, `EscapableConsole`, `SpectreTextInput`. Spectre.Console закреплён на `0.57.2`. |
| `src/EtcdTerminal.Tests` | 177 тестов, геометрические fake terminal (`FakeTerminal`, `RecordingTerminal`). `Spectre.Console.Testing` в проекте **отсутствует**. |
| `src/EtcdTerminal.IntegrationTests` | Интеграционные проверки etcd. UI-миграция не повод их удалять. |

UI-контрактов, которые предыдущая попытка создавала, больше нет: `src/EtcdTerminal/Presentation` не существует, `SpectrePanelRenderer` и `SpectreStatusBarRenderer` не существуют. Их нужно создать заново, не создавая вторых версий по ходу дела.

## 2. Baseline

```
dotnet build src/EtcdTerminal.slnx          →  Build succeeded, 0 Error(s), 0 Warning(s)
dotnet test src/EtcdTerminal.Tests          →  Passed! Failed: 0, Passed: 177, Total: 177
```

Это поведенческий эталон. Любая миграция принимается только относительно него: 177 тестов проходят, 0 предупреждений. Расхождение числа тестов в обе стороны — повод объяснить, куда делись проверки.

Единственные потребители Spectre сегодня — `SpectreTextInput` и `EscapableConsole` в Infrastructure, плюс `AnsiConsole`-вызовы в `ConsoleTerminal` (таблица, баннер, исключение).

## 3. Разбор Удалённой Попытки

Попытка добавила `Presentation`-контракты, два Spectre-renderer, перевёл на них пагинацию и панели ключей, удалил часть ручной геометрии. Итог: **9 ошибок компиляции, ни один тест не запускается, ни одно наблюдаемое поведение не изменено.** Срез был в трёх правках от зелёной сборки.

Диагностическая копия дерева с тремя правками мостов дала `Build succeeded`, `Passed: 177, Failed: 3` — падали только три новых теста, ни один существующий не был сломан. То есть работа не стоила ничего: ни прогресса, ни регрессий.

Четыре причины, каждую из которых нужно не повторить.

**3.1 Три несовместимые формы моделей одновременно.** Объявлено одно, использовано другое, проверено третье:

| Форма | Содержание | Где |
| --- | --- | --- |
| Объявлено | `PanelModel(IReadOnlyList<PanelLine>, PanelKind, Title)`, `PanelLine(IReadOnlyList<StyledText>)`, `StyledText(Text, TextRole)`, `StatusBarModel(Version, Left, Right)`, `TextRole` из 9 значений | `src/EtcdTerminal/Presentation/` |
| Использовано | `PanelModel([string])` — плоские строки; `StatusBarModel { LeftHints, Name, Connection, Username }` — nullable-поля | `KeyBrowseLayout`, `StatusBar`, оба renderer, `PanelMigrationTests`, `LayoutTests`, `SessionFlowTests` |
| Смесь | Строки в `PanelModel` и отсутствующие поля в `StatusBarModel` | Результат сборки |

Порядок «создать модель, потом найти ей потребителей» был нарушен: контракт изменили, а потребителей переписали под другую версию. Правило: **одна форма модели, и она компилируется целиком прежде, чем что-либо удаляется.** Нельзя менять контракт и одновременно мигрировать на него первых потребителей — это создаёт промежуточное состояние, которое не собирается и не покрыто тестами.

**3.2 `TextRole` не использовался вообще.** Поиск по репозиторию давал упоминания только в `StyledText.cs` и в самом перечислении. Ни один producer не выбирал роль, ни один mapper не преобразовывал роль в `Style`. Заявленная семантика цветов не была реализована в какой-либо степени: палитра терялась на этапе сборки модели. Создание перечисления ролей без mapper'а — это не миграция.

**3.3 У `PanelLine` нет доступа к буквальному тексту.** `record` с единственным полем `Spans` и без `ToString()`. Наивный рендер печатал `PanelLine { Spans = <>z__ReadOnlySingleElementList`1[...StyledText] }` — это был наблюдаемый вывод упавшего теста. Контракт модели должен предоставлять текст явно.

**3.4 Поля модели не использовались, а половина экрана осталась ручной.** `StatusBarModel.Left`/`Right` не читались ни одним renderer. Мигрированы 3 метода из 7 в `KeyBrowseLayout`; `RenderSearchBar` и `RenderKeyList` остались на `WriteFillRow`, `PadCurrentRow`, `WindowWidth`, `CursorLeft`/`CursorTop` и `DisplayCells.Width` — 36 мест прямой записи в файле. Следствие — заведомо неверная арифметика строк: `KeyBrowseControl.cs:27` сдвигает курсор на `+2`, что соответствовало ручной строке поиски в две строки, а Spectre-панель занимает три. Экран стал гибридом двух систем, хуже любой из них. Вертикальный срез переносится целиком либо не начинается.

Отдельно: удалённые `ITerminalOutput.WriteBorderedFillRow`/`WriteBorderedRow` и `StatusBarPersistenceTests` были направлены в правильную сторону, но выполнены в том же промежуточном состоянии, поэтому потерялись без пользы. `StatusBarPersistenceTests` содержит проверку, что имя активного подключения видно во время prompt и press-any-key — это требование поведения, его нужно перенести в model- и PTY-проверки, а не просто удалять. Геометрическое утверждение о прокрутке до строки `WindowHeight - ReservedRows - 2` восстанавливать не нужно.

## 4. Что Фактически Требуется Перенести

Это инвентаризация состояния `ee4aee2`, а не список потерь.

### 4.1 Ручная Геометрия

| Место | Что делает |
| --- | --- |
| `KeyBrowseLayout` | 36 мест прямой записи: `WindowWidth`, колонки ключ/значение, `WriteFillRow`, `PadCurrentRow`, `DisplayCells.Width`, строковые ANSI-цвета |
| `Menu.cs` | собственный цикл redraw: `SetCursorPosition` на каждую строку, `ClearToEndOfScreen`, ручной подсчёт ширины, ASCII-троеточие вместо `…` |
| `StatusBar.cs` | `WindowHeight`, `SetCursorPosition(0, WindowHeight - 3)`, `FillRow`, `GetVisibleLength`, шесть ступеней подгонки |
| `ScreenLayout.cs` | `Clear()` и ручной возврат курсора |
| `MultiLinePasteReader.cs` | `"\r" + new string(' ', WindowWidth - 1)` для очистки строки |
| `Spinner.cs` | `"\r"`-перерисовка и `ClearLine()` |
| `ConsoleTerminal` | `FillRow`, `PadCurrentRow`, `WriteRow`, `GetVisibleLength` (ANSI-aware обход), генерация `\x1b[38;2;…m` и `\x1b[48;2;…m` |
| `DisplayCells` | собственная таблица ширин, ANSI-aware truncation |

Политика подгонки футера (`StatusBar.cs:40-79`) — это работающая логика, а не мусор: шесть ступеней потери деталей при нехватке ширины, включая расчёт бюджета для имени. Её нужно сохранить как требование и реализовать в Infrastructure, а не выбросить и не заменить константами в Presentation.

### 4.2 Семь Владельцев Вывода Футера

| Файл | Строки |
| --- | --- |
| `App/Components/ScreenLayout.cs` | 14 |
| `App/Engine/Menu.cs` | 47 |
| `App/Screens/Keys/KeyBrowseControl.cs` | 37 |
| `App/Engine/Prompt.cs` | 86, 90 |
| `App/Components/PressAnyKeyPrompt.cs` | 12 |
| `App/Components/MultiLinePasteReader.cs` | 24, 73 |

`ScreenLayout.RenderHeader` выводит футер и возвращает курсор обратно; `KeyBrowseControl.Render` выводит его же ещё раз. `Menu` вызывает футер сразу после записи последнего пункта без завершающего перевода строки. `ReservedRows` и `EnsureCursorAboveBar` — геометрия, согласованная именно с таким потоковым выводом.

Требование: у экрана ровно один владелец композиции и вывода. Дети не дописывают и не перемещают общий футер.

### 4.3 Визуальный Язык, Который Нужно Сохранить

Эталон — `screenshots/`. Съёмка сделана на работающем коде `cf92b61`, пригодна для сверки.

- Figlet-баннер `etcd-terminal`, по центру, цвет `Banner`.
- Полосы во всю ширину **без рамки**, с тёмным фоном: строка поиска, список ключей, пагинация, футер. Рамок Spectre в текущем оформлении нет нигде.
- Панели выбранного ключа и подсказок действий — вертикальная черта `│` у левого края, не полная рамка.
- Список ключей: две колонки, ключ слева, значение примерно с середины; выбранная строка целиком в `Accent` с префиксом `❯`, остальные в `Primary`.
- Футер: подсказки слева в `Muted`, сведения о подключении **прижаты вправо** — `•` `Success`, имя `Secondary`, connection `Muted`, разделители `Subtle`, пользователь `Warning`, версия `Primary`.
- Отступ 4 пробела, указатель выбора `  ❯ `.

Замена полос на `RoundedBorder` панели меняет язык оформления. Так делала удалённая попытка, молча и без согласования. Если рамки нужны — это отдельное решение пользователя, а не деталь миграции.

### 4.4 Два Владельца Консоли

`SpectreTextInput` создаёт собственный `new EscapableConsole(AnsiConsole.Console)`; `ConsoleTerminal` использует `AnsiConsole` для таблиц, баннера и исключений. Ни то, ни другое не связано с зарегистрированным в DI `ITerminal`. Статический `AnsiConsole.Console` фактически играет роль сервис-локатора, что `AGENTS.md` запрещает, и обходит порядок инициализации, который задают `Program.cs` и `ConsoleTerminal.Initialize()`.

### 4.5 Архитектурные Проверки Слабее, чем Кажутся

- `ArchitectureTests._promptException` (`:9`) — пустой список исключений, существующий специально, чтобы быть заполненным.
- `ScreensContainNoEscapeLiterals` (`:116-126`) сканирует только каталог `Screens`, тогда как геометрия живёт в `Components`, `Engine` и в feature-local контролах под `Screens/Keys`.
- `AppTypesDoNotReferenceSpectre` работает по сигнатурам и не видит вызовы в телах методов.
- Ни одна проверка не утверждает, что в Presentation нет расчёта ширины, позиций курсора и строковых цветов.

## 5. Целевая Структура

```text
Domain / Application
  бизнес-данные, правила, use cases
              ^
              | использует
App: Screens / Components / Engine
  локализация, доступность действий, UI-состояние, навигация
              |
              v
Presentation contracts
  данные, семантические роли, интерфейсы операций
              ^
              | реализует
Infrastructure: Spectre rendering / interactive adapters
  маппинг, композиция renderables, один владелец консоли
```

Связывание реализаций выполняется только в `App/Setup/IocRegistrations.cs`.

**Presentation.** Компонент описывает форму: заголовок, список, действия, футер, выбранный элемент, семантические роли. Он не вычисляет ширину, не двигает курсор, не заливает фон, не подбирает бюджет под ширину окна. `StatusBar` собирает данные сессии и локализацию в модель. `KeyBrowseLayout` формирует модели поиска, списка, выбранного ключа, пагинации и действий. Фильтрация, права E/D, выбранный ключ и переходы остаются в `KeyBrowseScreen`/`KeyBrowseControl`. Presentation имеет право использовать бизнес-сервисы; это не делает его Domain.

**Infrastructure.** Один модуль маппинга в `Infrastructure/Terminal` либо в его тематическом подкаталоге: общий преобразователь `TextRole` + `ITheme` → `Style` (имя выбрать при реализации), `StyledText` → буквальный `Paragraph`/`Text`, колонки → `Table`/`Grid`. Построение дочернего renderable отделяется от физического вывода. Политика подгонки футера и политика overflow живут здесь. Один screen host владеет кадром и интерактивной сессией; это реализация Presentation-контракта, а не сервис, доступный только через Infrastructure-тип.

**Физическое разделение сборок.** Выделить dependency-free проект `src/EtcdTerminal.Presentation/EtcdTerminal.Presentation.csproj` и перенести в него UI-модели, контракты темы и локализации. Сейчас в core-проекте смешаны 8 файлов `Terminal`, 2 `Theming`, 1 `Localization` с бизнес-контрактами. `TableData`, `MenuItem`, `ITextInput` сначала проверить на пригодность, не переименовывать ради переименования; удаляемые legacy-интерфейсы не переносить автоматически. `App` и `Infrastructure` ссылаются на Presentation; Domain — нет. Добавить проект в `src/EtcdTerminal.slnx`. Отдельный Application-проект не создавать ради симметрии.

## 6. Проверенные Возможности Spectre

Документация получена через Context7: `/websites/spectreconsole_net`. Для версионных деталей прочитан исходник `/mnt/Data/Projects/Ext/spectre.console/`.

Локальный HEAD: `e2cf675`, `0.57.2-19-ge2cf675`. Тег `0.57.2`: `bbbb5729dde27b58deee44f447a788eea46ee451`. Проект закрепляет `0.57.2` (`EtcdTerminal.Infrastructure.csproj:13`). Расхождение тега и HEAD измерено: **4 файла, 72 строки.** `TableMeasurer.cs` — 4 строки, меняющие fallback второй максимальной колонки с `1` на `0` и условие выхода с `== 0` на `<= 0`; `ListPrompt.cs` и `ListPromptRenderHook.cs` — снятие generic с внутреннего класса, на поведение не влияет; `Calendar.cs` — новый виджет, **в закреплённой версии отсутствует**, не использовать. Поведение узких таблиц измерять на теге.

Все пути ниже относительно `src/Spectre.Console/` локального репозитория библиотеки, на теге.

### 6.1 Компоненты

| Компонент | Для чего | Ограничение |
| --- | --- | --- |
| `Widgets/Paragraph.cs`, `Text` | Буквальный текст и стилизованные spans | Не смешивать пользовательские данные с `Markup`; sanitization управляющих символов сохраняется отдельно. |
| `Widgets/Panel.cs` | Рамки, заголовки, отступы, expansion | **Фона нет.** Свойства `Border`, `BorderStyle`, `Padding`, `Expand`, `Width`, `Height`; стиля внутренней поверхности не существует. Недостающие до ширины панели места заполняются `Segment.Padding(diff)`. |
| `Widgets/Padder.cs` | Отступы вокруг содержимого | Верхний, нижний и боковой отступы — тот же безстильный `Segment.Padding`. |
| `Widgets/Table/`, `Grid` | Колонки ключ/значение, пользователи, роли, левая и правая часть футера | Не интерактивный список и не scrolling viewport. Футер таблицы — не футер экрана. |
| `Widgets/Layout/Layout.cs` | Логические регионы header/body/actions/footer | `Size` — фиксированный размер, `Ratio` — доля, `MinimumSize` (по умолчанию 1) — нижняя граница. Отсутствие `Size` означает пропорцию, а не auto-height. |
| `Live/LiveDisplay.cs` | Обновление общей композиции | Управляет своей областью, не даёт независимой bottom-anchor гарантии. Собственного таймера обновления нет. |
| `Prompts/SelectionPrompt.cs` | Выбор пунктов | Требует `Profile.Capabilities.Interactive` **и** `Ansi`, иначе `NotSupportedException` (`Prompts/List/ListPrompt.cs:27,34`). Не захватывает `RunExclusive`. `PageSize` по умолчанию 10. |
| `Prompts/TextPrompt.cs` | Ввод, secret, validation, editable default | Это `IPrompt<T>`, не `IRenderable`. Захватывает `RunExclusive`. Проверок capability не делает. |
| `Live/Status/Status.cs` | Индикатор длительной операции | Построен на Progress; это не нижний статус-бар приложения. |

### 6.2 Закреплённый Футер: Механизм Подтверждён

`Widgets/Layout/Layout.cs:212`:

```csharp
var height = options.Height ?? options.ConsoleSize.Height;
```

`Layout` заполняет `layoutLines` ровно `height` пустыми строками и накладывает регионы поверх. `LayoutSplitter.RowSplitter` (строки 34-46) назначает регионы сверху вниз: `region.Y + offset`, `offset += childHeight`. Размеры считает `Internal/Ratio.Resolve`, где `Size` фиксирован, а долю получают остальные.

Следствие: `SplitRows(header, body, footer.Size(1))` помещает футер в строку `ConsoleSize.Height - 1`, то есть в последнюю строку экрана. **Закреплённый футер получается из библиотечной композиции, а не из ручного позиционирования.** Это не гипотеза и не предмет прототипирования «сможет ли Spectre» — механизм прочитан в исходниках.

Осталось проверить в PTY только то, что касается не `Layout`, а `Live`:

- `LiveRenderable.PositionCursor` (`Live/LiveRenderable.cs:40`) поднимает курсор на `Height - 1` от текущей позиции и нигде не привязывает кадр к низу экрана. Кадр окажется внизу, только если его начало — строка 0. Значит, перед стартом кадра нужен `console.Clear(true)` либо cursor home, либо alternate screen. Это решение, а не деталь реализации.
- `LiveRenderable.Render` (`:98`, ветка переполнения `:112-154`) обрезает кадр, если он выше профиля консоли. По умолчанию `LiveDisplay.Overflow = VerticalOverflow.Ellipsis`, `Cropping = Top` (`Live/LiveDisplay.cs:21,26`): строки удаляются **сверху**, а в начало вставляется жёлтый `…`. На низком терминале футер будет срезан и заменён многоточием. Требуемая политика — `Overflow(VerticalOverflow.Crop).OverflowCropping(VerticalOverflowCropping.Bottom)`. Значение по умолчанию неприемлемо и должно быть задано явно.
- `SegmentShape.Inflate` (`Rendering/SegmentShape.cs`) только увеличивает размер кадра, никогда не уменьшает. Уменьшение списка ключей оставит строки-призраки от прошлого кадра.
- `Ratio.Resolve` при нехватке места возвращает для гибких регионов `MinimumSize` (по умолчанию 1), а `Layout` при этом всё равно рендерит больше строк, чем есть в терминале. Регион `Size(3)` в двухстрочном терминале даст обрезку по правилам выше.

### 6.3 Эксклюзивность Интерактивных Операций

`RunExclusive` встречается на теге ровно в трёх местах: `Live/LiveDisplay.cs:92`, `Live/Progress/Progress.cs:125`, `Prompts/TextPrompt.cs:129`. `Internal/DefaultExclusivityMode.cs` при неудачном `Wait(0)` бросает `InvalidOperationException`, а не ставит операцию в очередь.

`SelectionPrompt` в этой версии `RunExclusive` не вызывает: `ListPrompt` устанавливает `RenderHookScope` (строка 56) и сам управляет интерактивным выводом. Практические следствия:

- `TextPrompt` или `Status` внутри активного `Live` на той же консоли **бросят исключение**. Это не «может конфликтовать», это детерминированный отказ.
- `SelectionPrompt` внутри активного `Live` исключения не бросит, но установит второй render hook поверх `LiveRenderable`, который тоже управляет областью. Состав вывода в этом случае не определён ни одним контрактом библиотеки.
- Оборачивать существующий `Prompt` в `Live` нельзя. Остановка `Live` перед prompt тоже не сохраняет закреплённый футер сама по себе.

Отдельно про `SelectionPrompt`, если меню на него переводить:

- Указатель выбора — `ListPromptConstants.Arrow = ">"`, `const`, API для замены нет (`Prompts/List/ListPromptConstants.cs:5`). Текущий — `"  ❯ "`. Видимое изменение.
- `MoreChoicesMarkup` и `SearchPlaceholderMarkup` — английские строки библиотеки. Локализация проекта не совпадёт.
- Строка выбора строится как `new Markup(indent + prompt + " " + text, style)` (`Prompts/SelectionPrompt.cs:231`). Escape применяется **только к текущему пункту** (`:223`); остальные проходят через `Markup` как есть. Название инстанса или connection string с `[` будет разобрано как разметка. Режима буквального текста у виджета нет.

### 6.4 Фон Панели: Доказанный Пробел Библиотеки

`AGENTS.md` требует перед собственным адаптером проверить библиотеку и задокументировать пробел. Проверка выполнена, пробел подтверждён:

- `Widgets/Panel.cs` не имеет свойства фона. Единственный стиль — `BorderStyle`, применяемый к рамке.
- Недостающие до ширины панели места: `content.Add(Segment.Padding(diff))`.
- `Rendering/Segment.cs:58`: `public static Segment Padding(int size) => new Segment(new string(' ', size));` — конструктор без стиля, то есть `Style.Plain`.
- `Widgets/Padder.cs` заполняет верхний, нижний и боковой отступы тем же `Segment.Padding`.
- `Rendering/SegmentShape.cs`, метод `Apply`, дополняет кадр до размера тоже безстильным `Segment.Padding`.

Итог: заливка фона средствами `0.57.2` невозможна — ни `Panel`, ни `Padder`, ни `Table`, ни `Markup`. Собственная заливка пробелами и ANSI — это ровно тот код, который `AGENTS.md` запрещает. Решение требует выбора пользователя, см. раздел 7.

### 6.5 Тестовая Консоль Не Проверяет Ничего Из Перечисленного

`Spectre.Console.Testing` сейчас не подключён. Если будет добавлен, `src/Spectre.Console.Testing/TestConsole.cs` создаёт внутреннюю консоль с:

```csharp
Ansi = AnsiSupport.Yes, ColorSystem = ColorSystemSupport.TrueColor,
Interactive = InteractionSupport.No, ExclusivityMode = new NoopExclusivityMode()
```

и `NoopCursor`, а `Profile.Width = 80; Profile.Height = 24` заданы константами. `Write` разворачивает renderable в `StringWriter`, отбрасывая control codes.

- Тест, вложивший `TextPrompt` в `Live`, **пройдёт** на `TestConsole` и упадёт на настоящей консоли.
- Закреплённый футер, поведение при 40-строчном или 2-строчном терминале, resize через `TestConsole` не проверяются в принципе.
- Утверждения о совместимости вложенных виджетов допустимы только как «не бросает на `NoopExclusivityMode`», не как «работает у пользователя».

### 6.6 Alternate Screen

`Extensions/AnsiConsoleExtensions.Screen.cs` даёт `AlternateScreen(Action)`, бросает `NotSupportedException` без `Ansi` и `AlternateBuffer`, восстанавливает буфер в `finally`. На теге существует **только перегрузка `Action`** — асинхронный host ею напрямую не воспользуется. Кандидат для host, не готовое решение.

### 6.7 Строка Статуса Ввода: Доказанный Пробел Библиотеки

`MultiLinePasteReader.RenderPasteStatus` перерисовывает одну строку на месте: `\r`, дозаполнение пробелами по `WindowWidth`, второй `\r` и текст. Это ровно тот cursor/ANSI/width-код, который `AGENTS.md` запрещает в компонентах, поэтому перед решением библиотека проверена на теге `0.57.2`:

- `Live/Status/Status.cs`, метод `StartAsync`: `var progress = new Progress(_console) { FallbackRenderer = new FallbackStatusRenderer(), AutoClear = true, AutoRefresh = AutoRefresh };`. `AutoClear` зашит там жёстко, у `Status` нет свойства и нет способа его отключить. Строка статуса исчезает вместе с окончанием операции и не остаётся над превью, как сейчас.
- `Live/Status/StatusContext.cs`: менять можно только `Status`, `Spinner` и `SpinnerStyle`. Нет ни `AutoClear`, ни вызова `Completed`, то есть оставить дорисованное состояние средствами `Status` нельзя.
- `Status` — индикатор вокруг асинхронной задачи. Разбор вставки — блокирующий цикл чтения клавиш; обернуть его в `Status` технически можно, но это меняет судьбу строки (см. выше), а не её отрисовку.
- `AnsiConsole.Live` требует собственный целевой renderable и свой регион. Строка статуса печатается поверх уже потокового вывода — строки приглашения и двух пустых строк выше, — поэтому кадр `Live` здесь не подходит: он очистил бы экран и приглашение вместе со строкой.

Итог: штатного способа держать обновляемую строку статуса поверх потокового вывода в `0.57.2` нет. Переносить этот код в Infrastructure тоже нельзя — перенос ручного cursor/ANSI/width-кода в Infrastructure не является выполнением требования.

## 7. Решения, Требующие Вашего Согласования

Ни одно из этих решений нельзя принимать молча.

1. **Фон панели.** Пробел доказан (6.4). **Решено в этапе 0:** панели без рамок и без фона; потеря фона принята осознанно. Варианты: (а) отказаться от фона полос и явно согласовать изменение оформления; (б) согласовать узкий адаптер заливки, описав его как исключение из запрета; (в) оставить фон только там, где его даёт сама библиотека. Вариант «заполнить пробелами и ANSI» запрещён `AGENTS.md` и не предлагается.
2. **Футер во время ввода.** `TextPrompt` внутри активного `Live` детерминированно бросает исключение. Нужен выбор: штатное поведение, при котором футер на время ввода уходит с экрана; либо минимальный input-адаптер с библиотечной отрисовкой, отрисовывающий футер вручную, с явным согласованием; либо заранее согласованное исключение из требования о закреплённом футере в состояниях ввода. **Решено пользователем: вариант (б), одобрен как исключение.** Prompt, paste reader и меню работают вне кадра host; футер в это время дорисовывает Infrastructure (`SpectreStatusBarRenderer.Write` ставит его на последнюю строку), а App только сохраняет и возвращает позицию курсора. Ни один prompt не выполняется внутри активного `Live`, поэтому исключение не срабатывает; закрепление футера подтверждено PTY. Цель этапа 4 — убрать геометрию (`ReservedRows`, `EnsureCursorAboveBar`, сохранение позиции) из App, сохранив этот же результат.
3. **Внешний вывод.** `SelectionPrompt` требует `Interactive`, `TextPrompt` — нет. При перенаправлении вывода меню начнёт падать, а prompt продолжит работать. Поведение для non-interactive нужно определить отдельно и не выдавать потоковый вывод за интерактивный паритет. **Решено:** `SelectionPrompt` не используется (7.4), поэтому `ListPrompt.Show` со своим `NotSupportedException` для non-interactive и ANSI в наше дерево не попадает. Меню остаётся своим чтением клавиш. Паритет для перенаправленного вывода не заявляется: интерактивный паритет подтверждается только в терминале или PTY (раздел 9).
4. **Меню на `SelectionPrompt`.** Меняет указатель с `❯` на `>`, добавляет библиотечные английские подсказки, ограничивает `PageSize` значением 10 и не экранирует невыбранные пункты. Либо принять, либо согласовать иной путь. **Решено пользователем: сохранить согласованный адаптер, `SelectionPrompt` не принимается.** Проверка `0.57.2`, `Prompts/List/ListPrompt.cs` и `Prompts/SelectionPrompt.cs`:
   - `ListPrompt.Show` начинается с `if (!_console.Profile.Capabilities.Interactive) throw new NotSupportedException(...)` и тем же проверкой на `ANSI`; принятие превращает меню в исключение при перенаправлении (см. 3).
   - `hook.Clear()` в конце `Show` стирает список, а у нас меню — кадр host с баннером и закреплённым футером; `RenderHookScope` конкурирует с `Live` host, а не заменяет его.
   - `HandleInput` возвращает `Abort` только при `key.Key == Escape && CancelResult is not null`, то есть без сентинелы Escape перестаёт отменять, а это обязанность меню.
   - `ListPromptConstants.Arrow == ">"`, подсказки — англоязычный markup `(Move up and down to reveal more choices)` и `(Type to search)`; это меняет визуальный язык (4.3).
   - Невыбранные пункты попадают в `new Markup(indent + prompt + " " + text, style)` без экранирования, экранируется только текущий. IPv6 и brackets из соединений — требования на буквальность (раздел 9), их пришлось бы экранировать снаружи и ловить двойное экранирование строки с `RemoveMarkup()`.
   - Недоступных пунктов и разделителя в `SelectionPrompt` нет: отключаются только группы в `SelectionMode.Leaf`, а разделитель в `InstanceSelectionScreen` — лист без детей; `MenuTests` проверяет именно такой строке.

   Собственный построчный redraw в меню уже отсутствует: `Menu.ShowFramed` отдаёт одну модель кадру и обновляет её через `IScreenHost.Update`, строковой отрисовки в `Engine/Menu` нет.
5. **Рамки панелей.** Полосы без рамки плюс вертикальная черта слева (4.3) или явное согласование рамок. **Решено в этапе 0:** `NoBorder`, вертикальная черта утеряна вместе с фоном. Вернуть черту без фона нельзя: `Panel` резервирует 2 колонки под обе стороны рамки и рисует нижнюю линию.
6. **Строка статуса ввода.** Пробел доказан (6.7). **Решено пользователем:** оставить текущую ручную перерисовку в `MultiLinePasteReader.RenderPasteStatus` как одобренное исключение. Адаптер не строить и в Infrastructure не переносить; Spectre для этого места не используется.

## 8. Порядок Работ

### Этап 0. Создать Контракты И Mapper

Первый вертикальный срез, без обещания исправить закрепление футера.

1. Создать UI-контракты в `src/EtcdTerminal/Presentation`: `PanelModel`, `PanelLine`, `StyledText`, `TextRole`, `StatusBarModel`, `PanelKind`, `IPanelRenderer`, `IStatusBarRenderer`. Модели — данные; операции — в интерфейсах. Никаких Spectre-типов, markup, ANSI, координат, размеров и ширин.
2. Дать `PanelLine` явный доступ к буквальному тексту.
3. Определить роли. Не перечисление без потребителей: mapper и минимум один producer ролей появляются в том же этапе, иначе это заготовка.
4. Добавить общий преобразователь `TextRole` + `ITheme` → `Style` в Infrastructure. Исключить дублирование палитры по renderer.
5. Создать `SpectrePanelRenderer` и `SpectreStatusBarRenderer`, использующие общий mapper. Реализовать в них прижатие правого крыла футера вправо, а не конкатенацию строк.
6. Роли по `screenshots/key-browse.png`: выбранный ключ и его значение — `Accent`; остальные ключи и значения — `Primary`; клавиши E/D/Esc — `Primary`, подписи — `Muted`; нумерация страниц — `Primary`, подписи — `Muted`; индикатор подключения — `Success`, имя — `Secondary`, connection — `Muted`, разделители — `Subtle`, пользователь — `Warning`, версия — `Primary`.
7. Определить `Default` и единственного владельца форматирования версии. Не собирать версию одновременно из отдельного поля и из правого крыла.
8. Перевести на модели `KeyBrowseLayout.RenderPagination`, `RenderSelectedPanel`, `RenderButtonsPanel` и `StatusBar.BuildModel`.
9. Сохранить политику подгонки футера как требование, перенести её в Infrastructure вместе с рендерингом. Не заменять константами в Presentation.
10. Сохранить буквальность заголовков, ключей, IPv6 и brackets. Отделить нормализацию control characters в `ValuePreview` от обрезания по display width; sanitization не удалять.
11. Зарегистрировать в `IocRegistrations`, прогнать сборку и тесты.

**Дисциплина среза:** после шага 11 решение собирается, все 177 существующих тестов проходят, поведение не изменено. Только после этого переходить к следующему этапу. Не удалять ручной код, пока не заменён работающий потребитель.

Выход: согласованные контракты, работающий mapper, воспроизведённая семантика цветов, зелёная сборка. Промежуточный результат, не приёмка миграции.

#### Статус: этап 0 выполнен

| Проверка | Результат |
| --- | --- |
| `dotnet build src/EtcdTerminal.slnx` | 0 ошибок, 0 предупреждений |
| `dotnet test` | 197 из 197, из них 20 новых |
| Собственная отрисовка в App | не уменьшилась: `RenderSearchBar` и `RenderKeyList` ждут этапа 2 |
| Визуальная проверка | **не выполнена**, только `TestConsole` |

Создано: `PanelKind`, `TextRole`, `StyledText`, `PanelLine` (с `Text` вместо `ToString()`), `PanelModel` (без `Title` — в оригинале подпись и значение идут одной строкой), `StatusBarModel` (поля сохраняют идентичность, чтобы Infrastructure мог укорачивать футер по приоритету), `IPanelRenderer`, `IStatusBarRenderer`, `RoleStyleMapper`, `SpectrePanelRenderer`, `SpectreStatusBarRenderer`, два fake.

Решения, принятые в этом этапе:

- Панели рисуются как `Panel.NoBorder()`. Рамки не добавлены, тёмный фон полос утрачен: фон недостижим в `0.57.2` (6.4). Это осознанная потеря, а не дефект, который чинится mapper'ом.
- Политика подгонки футера перенесена в `SpectreStatusBarRenderer.Fit` в исходном порядке: сначала укорачивается endpoint, затем отбрасывается пользователь, затем endpoint, затем подсказки, и только в последнюю очередь имя. Закреплено тестами на ширинах 80, 45, 30, 15.
- `StatusBar` перестал зависеть от `ITerminalStyle` и больше не знает ширину окна.
- `ValuePreview.Sanitize` отделён от `Preview`: sanitization control-символов остаётся в приложении, усечение по ширине ушло в Infrastructure.
- Из 4 тестов, проверявших прежний ANSI-вывод футера, три переписаны на модель, один переименован в `LongConnectionDetails_FooterModelKeepsFullLiteralValues` и теперь проверяет, что модель не урезает значения сама.

Два отступления, которые нужно закрыть:

- `SpectreStatusBarRenderer` измеряет ширину через существующий `DisplayCells` из core-проекта. Новый движок измерения не вводился, но требование «ноль собственного расчёта display-width» этот класс пока нарушает. Замена на измерение Spectre либо перенос подгонки в host — задача этапа 1.
- `ReservedRows` и `EnsureCursorAboveBar` сохранены: на них ещё держится геометрия `Menu`, `Prompt`, `PressAnyKeyPrompt` и `MultiLinePasteReader`. Удаляются вместе с закреплением футера.

### Этап 1. Доказать Композицию И Lifecycle

1. Выполнить минимальный прототип в PTY и закрыть решения раздела 7 до изменения callers.
2. Ввести data-only контракт кадра и интерфейс host в объёме подтверждённой реализации. Возможные имена `ScreenModel`/`IScreenHost` — предложение; таких типов пока нет.
3. Модель содержит только логические регионы и состояния. Операции начала, обновления, ввода и завершения принадлежат интерфейсам, не полям модели.
4. `ScreenLayout` собирает логическую композицию; Infrastructure создаёт `Layout`, управляет выводом и **явно задаёт** `Overflow`/`OverflowCropping`, плюс старт кадра в строке 0. `StatusBar` предоставляет модель и не выбирает строку и не скроллит.
5. У host один экземпляр на консоль, явный begin/update/end lifecycle, в `finally` освобождение интерактивного контекста и восстановление состояния через библиотеку, включая cancellation и exception.
6. Проверить начало кадра, поведение при уменьшении содержимого и при недостатке высоты (6.2).
7. Зарегистрировать host и единый экземпляр консоли в `IocRegistrations`. `SpectreTextInput`, `EscapableConsole` и renderer должны использовать одну согласованную консоль вместо статического `AnsiConsole.Console`. Учесть инициализацию, recovery loop и cleanup в `Program.cs`.

Если обязательный паритет недостижим без исключений — остановить именно этот этап с описанием блокера, а не упрощать дизайн.

### Этап 2. Перевести Экран Ключей Целиком

- `KeyBrowseLayout`: модели поиска, списка, выбранного ключа, пагинации и действий. Ручные колонки заменить `Table`/`Grid` в mapper.
- `KeyBrowseControl`: сохранить команды и состояние; удалить `SearchEndCol`, `SearchBarRow`, перемещение курсора и самостоятельный вывод футера.
- `KeyBrowseScreen`: сохранить фильтр после edit/delete, выбранный элемент, права изменения, счётчики и отмену; прямые записи деталей заменить моделями.
- Обновлять один кадр вместо дописывания панелей. Прежний renderer не должен оставаться скрытым fallback.
- Удалить `width`/`padding`/`fill` алгоритмы вместе с их тестами после исчезновения потребителей.

### Этап 3. Перевести Остальной Интерактив

| Компонент | Изменение | Что сохранить |
| --- | --- | --- |
| `Engine/Menu`, `Components/MenuScreen` | Штатный `SelectionPrompt` при подтверждённом паритете либо согласованный адаптер; убрать собственный построчный redraw | ID, одинаковые labels, недоступные пункты и разделители, циклическая навигация, Enter/Escape, отмена |
| `Engine/Prompt`, `SpectreTextInput`, `EscapableConsole` | Согласовать с host; переиспользовать `TextPrompt` и существующую обработку Escape | trim, empty/null, editable default, secret, validation/cancellation |
| `Components/Spinner` | `Status` в допустимом lifecycle, не внутри `Live` | completion, cancel, распространение ошибок |
| `MultiLinePasteReader`, `PressAnyKeyPrompt` | Убрать собственные перерисовки строки; согласовать input state | многострочная вставка, queued input, Escape, отсутствие утечки секрета |
| `Header`, `Message` | Модели содержимого и семантики; Figlet и стили в Infrastructure | тексты, роли success/error/warning |
| `UserListRenderer`, `RoleListRenderer`, `PermissionViewRenderer` | Builders таблиц, переиспользовать пригодный `TableData` | локализация, содержимое, разрешения |
| `KeyImportJsonScreen`, `InstanceSelectionScreen`, остальные screens | Убрать оставшиеся прямые rendering writes | preview, empty-state, переходы |

Не удалять смысловую логику ввода только потому, что она не относится к бизнес-домену. Цель удаления — повторная реализация библиотечного рендера, а не вся Presentation-логика.

### Этап 4. Закрепить Границы И Удалить Legacy

- Выделить `src/EtcdTerminal.Presentation`, исправить references, DI и архитектурные проверки.
- Удалить неиспользуемое: строковые ANSI-цвета и методы заливки в `ITerminalStyle`/`ITerminalOutput`; cursor-зависимости Presentation, `ReservedRows`, `EnsureCursorAboveBar`, ручной redraw меню; алгоритмы width, padding, clear-line и escape generation из `ConsoleTerminal`; `DisplayCells` и ANSI-aware truncation после отделения нужной sanitization; геометрические fake terminal, когда исчезнет последний осмысленный потребитель.
- Расширить архитектурные проверки: запрет геометрии, ANSI и строковых цветов в Presentation; запрет `AnsiConsole` в App; data-only проверка UI-моделей. Не полагаться на пустой `_promptException`. Сканировать `Components` и `Engine`, а не только `Screens`.
- Прямой `System.Console` допустим в Infrastructure только как lifecycle/input, но не как восстановление ручной отрисовки. Результат должен уменьшать собственный rendering-код, а не перемещать его.

**Статус этапа 4 (выполнено, не закоммичено):**

- `src/EtcdTerminal.Presentation` выделен в отдельный проект, добавлен в `.slnx`; Infrastructure, App и Tests ссылаются на него явно. В него вошли **все** контракты UI-слоя, а не только папка `Presentation`: сами модели (`PanelModel`, `PanelLine`, `StyledText`, `TextRole`, `PanelKind`, `ScreenModel`, `StatusBarModel`), интерфейсы рендеров (`IPanelRenderer`, `IStatusBarRenderer`, `IScreenHost`, `IStatusIndicator`) и папки `Terminal` (`ITerminal*`, `ITextInput`, `MenuItem`, `TerminalColor`, `DisplayCells`), `Theming` (`ITheme`, `RgbColor`), `Localization` (`ILocalization`) — 27 файлов. Namespace'ы не менялись, поэтому перенос не потребовал ни одной правки в коде.
  - `ITextInput`/`ITerminal*` — не «домен» и не «инфраструктура», а входные порты, запрещённые бизнес-коду: AGENTS относит их к presentation-контрактам («renderer/input interfaces»), а размещать их в Infrastructure нельзя, потому что App потребляет их напрямую и не должен ссылаться на Infrastructure.
  - `EtcdTerminal` после переноса содержит только `Configuration`, `Environment`, `Keys`, `Permissions`, `Roles`, `Security`, `Session`, `Users` и etcd-операции — то есть только домен и application-порты. Ни одного `using` на `EtcdTerminal.Terminal|Theming|Localization` в домене не было и до переноса, поэтому вынос не изменил ни одной зависимости.
- Домен `EtcdTerminal` **не** ссылается на Presentation (`DomainDoesNotReferencePresentation`, теперь с проверкой имени сборки — маркерный тип `IEtcdClient` обязан жить в `EtcdTerminal`, иначе проверка молча стала бы проверкой не той сборки). Проверка `DomainSourcesContainNoUiContractNamespaces` не даёт вернуть контракты UI в доменную сборку.
- DI: единый экземпляр консоли через `Infrastructure/Terminal/SpectreConsoleSource` вместо статического `AnsiConsole.Console`. `IocRegistrations` не называет ни одного типа `Spectre.*`, а в исходниках App не осталось ни одного упоминания `Spectre.Console` — это закрывает слепую зону, в которой проверка по сигнатурам не видела тела лямбд.
- Удалено из `ITerminalOutput`: `ClearLine`, `ClearToEndOfScreen`, `FillRow`, `WriteFillRow`, `WriteRow`, `WriteBorderedFillRow`, `WriteBorderedRow`, `PadCurrentRow`, `GetVisibleLength`; вместе с ними `RecordingTerminalTests.Markers_CostNoCells`.
- Удалены строковые ANSI-цвета `ITerminalStyle`: `PanelBackground`, `PanelDarkerBackground`, `Primary`, `Secondary`, `Success`, `Danger`, `Warning`, `Muted`, `SetBackground`, `ResetColor`, `SetDarkBackground`. Остались только реально используемые `Subtle`, `Accent`, `Reset`, `SelectionPointer`, `Indent`, `ResetBackground`.
- Из `ConsoleTerminal` убраны алгоритмы заливки, padding, clear-line и ручной разбор escape-последовательностей (`GetVisibleLength`); `ResetColor` и `SetDarkBackground` стали приватными и используются только самим terminal lifecycle.
- `ReservedRows` и `EnsureCursorAboveBar` переехали в `SpectreStatusBarRenderer.EnsureRoomAbove`: Infrastructure сам решает, сколько строк резервирует футер и куда ставить курсор. В App не осталось рисования курсором — только сохранение и возврат позиции (`RenderPreservingCursor`). `Spectre.Console` при этом не умеет читать позицию курсора (`IAnsiConsoleCursor` — только `Show`/`SetPosition`/`Move`), поэтому строка берётся из `ITerminalCursor`; это задокументировано в `EnsureRoomAbove`.
- `LayoutTests.ShortTerminal_NoNegativePositions` удалён по §9 — заменён на три renderer-теста через `TestConsole` (короткий терминал, курсор вне резерва, курсор внутри резерва).
- `DisplayCells.TruncateStyled` (ANSI-aware truncation) удалён вместе с тестами. Сам `DisplayCells` **не** legacy: `Width` нужен футеру и `ValuePreview`, поэтому пункт «удалить `DisplayCells` целиком» закрывается как отменённый, а не невыполненный.
- Геометрический fake `RecordingTerminal` остаётся: он даёт строку курсора renderer-тестам, последний осмысленный потребитель не исчез.
- Ручной redraw меню отсутствует: `Menu.ShowFramed` работает только через `IScreenHost.Begin/Update/End`.
- Архитектурные проверки добавлены: `DomainDoesNotReferencePresentation`, `PresentationReferencesNoDomainInfrastructureOrSpectre`, `PresentationSourcesContainNoEscapeSequencesOrConsoleAccess`, `AppSourcesContainNoSpectreConsoleReferences`, `AppSourcesContainNoSystemConsoleAccess`, `AppSourcesContainNoEscapeLiterals` (теперь весь App, включая `Components`, `Engine` и `Setup`, а не только `Screens`). `_promptException` остаётся пустым и не является единственной защитой: проверки, которые им пользуются, продублированы сканированием исходников.
- PTY-проверка против baseline `8e83f30` (worktree): H=8 с prompt после подключения, H=24 List Users и H=24 Create Key дают **байт-в-байт** идентичный вывод, 0 скроллов, исключений нет.

**Открытые пункты этапа 4:**

- `DisplayCells` — не контракт, а геометрический хелпер, и теперь он лежит в Presentation только потому, что уехал вместе с папкой `Terminal`. Единственный app-потребитель — `ValuePreview.Preview(value, maxWidth)` (`KeyImportJsonScreen`), тогда как весь остальной App уже использует `ValuePreview.Sanitize`. Нужно перевести этот вызов на `Sanitize`, отдать обрезку рендереру, удалить `ValuePreview.Preview` и перенести `DisplayCells` в Infrastructure (его оставшиеся потребители — `SpectreStatusBarRenderer` и `RecordingTerminal` — уже там или в тестах). Отдельным решением: проверить в закреплённой версии Spectre, есть ли готовая ellipsis-обрезка, чтобы не возвращать ручной пересчёт ширины.
- `ITerminalInput` тянет `ConsoleKeyInfo` — тип `System.Console` в контракте. Работает, но это осознанный долг: собственный `Key`-контракт убрал бы из Presentation последнюю зависимость от `System.Console`.

## 9. Тесты И Приёмка

### Что Оставить

- Модели: текст и роли, выбранный элемент, E/D только при наличии прав, информация сессии и её отсутствие после выхода.
- Поведение: `MenuTests`, `PromptTests`, `KeyBrowseScreenTests`, `MultiLinePasteReaderTests`, `SpinnerTests`, `SessionFlowTests`; harness адаптировать к новым контрактам, не привязывать к курсору.
- Мапперы: несколько `TestConsole`-тестов буквального текста и соответствия roles/styles. Проверять собственное преобразование, не алгоритмы измерения и рамок Spectre. Потребуется подключить `Spectre.Console.Testing 0.57.2` в `EtcdTerminal.Tests.csproj`.
- Архитектура: см. этап 4.
- Существующие domain/application/security/integration тесты сохранить по назначению.

`LayoutTests` используют `RecordingTerminal`, который считает позиции и длины строк вручную. Такие assertions проверяют геометрию, которой после миграции не будет. `AssertBounds` и проверки `CursorSets` удалить вместе с последним потребителем. `LongKeysAndValues_StayWithinWidth`, `WideText_StayWithinWidth`, `ControlCharacters_RenderedSingleLineWithinWidth`, `LongConnectionDetails_FooterFitsNarrowWindow` переформулировать через модель и mapper, а не через позиции записи. `ShortTerminal_NoNegativePositions` теряет смысл, когда позиции курсора перестают задаваться приложением.

`StatusBarPersistenceTests` (3 теста) сейчас в дереве и подлежит переносу: имя активного подключения должно быть видно во время prompt и press-any-key. Это модельные и PTY-проверки, а не геометрические. Утверждение `PressAnyKey_ScrollsContentSoLabelStaysAboveStatusBar` восстанавливать не нужно.

Не добавлять новый набор exhaustive width/Unicode/cursor snapshots. Не расширять fake до собственного эмулятора терминала. В отчёте перечислить удалённые алгоритмы и обслуживавшие их тесты.

### Проверка В Терминале Или PTY

`TestConsole` для этих сценариев непригоден (6.5). Проверять в настоящем терминале или PTY.

| Сценарий | Критерий |
| --- | --- |
| Меню и экран ключей, несколько обновлений подряд | Один футер внизу viewport; панели не дублируются и не появляются на строке выбранного пункта |
| Выбор, поиск, pagination, readonly | Цвета и семантика сохранены; все доступные действия достижимы |
| Prompt, validation error, Escape, возвращение | Футер остаётся внизу; нет конфликтов интерактивных сессий, потерянного ввода, старых панелей |
| Secret, editable default, multiline paste | Поведение сохранено, секрет не попадает в видимый вывод и логи |
| Spinner, cancellation, exception | Операция завершается, консоль пригодна для следующего экрана |
| Подключение и выход из сессии | Актуальные сведения, username только по условиям приложения, версия один раз |
| Низкий терминал, когда сумма `Size` превышает высоту | Футер не срезан и не заменён многоточием; действует выбранная политика overflow |
| Уменьшение списка между кадрами | Нет строк-призраков от предыдущего кадра |
| Resize терминала | Кадр перерисовывается, футер остаётся внизу |
| Длинные значения, brackets, IPv6, CJK/emoji, control characters | Буквальный безопасный текст; нет markup-инъекций и возврата собственного display-width движка |
| Фон панелей и футера | Проверены текст, отступы и свободные области, а не только цвет рамки |
| Перенаправленный вывод | Поведение определено и не выдаёт потоковый вывод за интерактивный |

Записать терминал или PTY, размеры, capability profile, сценарии и фактический результат. Сверить с `screenshots/`. Если сценарий не проверен — так и написать. При несовместимости требований нужно явное решение пользователя, а не отметка «готово».

### Команды

```sh
dotnet build src/EtcdTerminal.slnx
dotnet test src/EtcdTerminal.Tests/EtcdTerminal.Tests.csproj
dotnet run --project src/EtcdTerminal.App/EtcdTerminal.App.csproj
```

Последнюю команду выполнять в интерактивном терминале или PTY. Интеграционные тесты запускать при наличии требуемого etcd-окружения; недоступность окружения указать в отчёте.

### Готовность

Миграция принята только при совместном выполнении условий: сборка и релевантные тесты проходят; закреплённый футер и остальной паритет проверены в PTY и сверены со `screenshots/`; футер имеет ровно одного владельца; Presentation и Domain не содержат технического rendering; собственные ANSI, width, cursor и fill алгоритмы удалены либо конкретное исключение явно согласовано; UI-тестовая нагрузка уменьшена без потери поведения. Одних моделей, наличия `Panel` или зелёных текстовых snapshots недостаточно.

## 10. Краткая Задача Для Следующего Исполнителя

Прочитай `AGENTS.md` и этот документ, перепроверь `git status` и версию Spectre. Начни с этапа 0: создай UI-контракты, mapper ролей в стили и переведи на них пагинацию, панели ключей и модель футера — так, чтобы решение собиралось и все 177 тестов проходили. Помни, что предыдущая попытка на этом месте сломалось: контракт и его потребители должны быть согласованы в одном срезе, перечисление ролей без mapper'а не считается миграцией, а половина экрана, переведённая на новую и старую систему одновременно, ломает арифметику строк.

Затем докажи на PTY-прототипе жизнеспособность одного владельца кадра с нижним футером, явно заданной политикой overflow и переходами ввода, и вынеси пользователю на согласование решения раздела 7 — особенно невозможность заливки фона панели и судьбу футера во время ввода. Не обещай, что `Layout`/`Live` совместимы с prompts: половина ограничений раздела 6.3 — это детерминированные отказы, а не предупреждения. После согласования мигрируй экраны целиком, отдели UI-контракты от Domain и удали ручные rendering-алгоритмы вместе с их геометрическими тестами. В конце приложи результаты сборки, тестов и PTY-проверок и список оставшихся ограничений.

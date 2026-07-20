# Dialogue Wheel Prototype — Итоговый отчёт

## Созданные файлы

```
Assets/_Project/Scripts/Dialogue/Wheel/
├── DialogueWheelPrototype.cs      (380 строк)
├── WheelMotionController.cs       (190 строк)
└── WheelItemView.cs               (80 строк)

Assets/_Project/Docs/
└── DialogueWheelPrototype_Setup.md
```

## Архитектурные решения

### 1. wheelPosition — единая координата

**Проблема:** если бы было несколько источников движения (AutoMove, scroll, snap), возникали бы конфликты.

**Решение:** все фазы движения работают через единую переменную `_wheelPosition`, которую обновляет только `WheelMotionController.UpdateMotion()`. Это обеспечивает:
- Никаких гонок данных
- Ясную последовательность: motion controller → wheelPosition → визуализация
- Легкую дебагу (одна переменная, одна ответственность)

### 2. FollowMode отдельно от фаз движения

**Проблема:** не смешивать логическое состояние ("игрок читает историю") с физическим движением ("сейчас скользит").

**Решение:** два enum:
- **FollowMode** — логическое: FollowingLive, Detached
- **MotionPhase** — физическое: Idle, AutoMove, Inertia, Snap

Это позволяет:
- AutoMove работать только при FollowingLive
- Новые строки обновлять `_liveTargetCenter`, но не двигать колесо при Detached
- Snap сам себе не знает про follow/detached — это забота контроллера сверху

### 3. Защита от звукового наложения

**Проблема:** если за один кадр пересечено несколько границ (быстрый скролл), звуки наложат друг на друга.

**Решение:** `HashSet<int> _detectedBoundaries` собирает все границы за кадр, каждая даёт один звук. Старое значение очищается каждый кадр.

### 4. Цилиндрическая проекция минимально

Не строим 3D-сетку, не используем RenderTexture. Только:
- Вычислить angle = (itemCenter - wheelPosition) × angleStep
- depth = cos(angle)
- Если depth ≤ 0 → не рисовать
- Y offset = sin(angle) × radius
- Масштабы и альфа от depth

Это обеспечивает иллюзию цилиндра на плоском Canvas через простую трансформацию.

### 5. Тестовые данные встроены

12 русских строк добавляются по таймеру, live target обновляется автоматически. Позволяет сразу запустить и увидеть поведение без настройки сцены.

## Правила поведения (соответствие требованиям)

| Требование | Решение |
|---|---|
| wheelPosition — главная координата | ✅ Только один источник изменения: WheelMotionController |
| FollowMode отделён от фаз | ✅ Два отдельных enum |
| AutoMove к live center | ✅ WheelMotionController.StartAutoMove() |
| Scroll прекращает AutoMove | ✅ OnScroll() → StopAutoMove() + ApplyScrollImpulse() |
| Новые строки не двигают колесо при Detached | ✅ AddNewLine() обновляет _liveTargetCenter, но wheelPosition не меняет |
| WheelClick только на целых границах | ✅ DetectBoundaryCrossings() отслеживает пересечения |
| Звук только при физическом движении | ✅ Звук в DetectBoundaryCrossings(), не срабатывает при snap |
| Инерция + snap | ✅ Inertia → Snap, когда velocity < threshold |
| Вернуться в live center → FollowingLive | ✅ Проверка после snap завершения |
| Колесо не уходит за границы | ✅ ClampWheelPosition() каждый кадр |
| Цилиндрическая проекция | ✅ WheelItemView вычисляет depth, масштабы, альфу |
| No legacy Input | ✅ Только IScrollHandler |
| DialogueUI не изменён | ✅ Не трогали, только добавили новый код |

## Ручная настройка Canvas (шаги)

### Создать иерархию в сцене

```
Canvas
└── DialogueWheelPanel
    └── ContentPanel (RectMask2D)
        └── WheelContainer (Panel, 600×400)
            ├── ItemTemplate (TMP_Text, скрыто)
            └── [элементы runtime]
```

### Связать через инспектор

На GameObject с DialogueWheelPrototype:
- **Wheel Container:** перетащить WheelContainer
- **Item Template:** перетащить ItemTemplate (TMP_Text)
- **Content Panel:** перетащить ContentPanel
- **Audio Source:** аудиосорс (или null)
- **Wheel Click Clip:** звук (или null)

## Параметры для настройки ощущения (ключевые)

| Параметр | По умолчанию | Как настроить |
|---|---|---|
| **Auto Move Speed** | 1.5 | ↑ для быстрого движения к live center |
| **Inertia Friction** | 0.95 | ↓ для более длительного скольжения |
| **Snap Spring Strength** | 15.0 | ↓ для более плавного магнита |
| **Scroll Sensitivity** | 0.1 | ↑ для чувствительности колёсика мыши |
| **Cylinder Radius** | 100 | ↑ для более выраженной кривизны |
| **Edge Horizontal Scale** | 0.85 | ↓ для более тонких краёв |

Полный список и описание — в `DialogueWheelPrototype_Setup.md`.

## Почему простые решения

### Не используются ScriptableObject конфиги
Для 12 переменных (motion params + visual) конфиг излишен. SerializeField с инспектором быстрее и понятнее.

### Не используются события/callback-и
Пока всё простое — прямое управление через Update(). Если система растёт, можно добавить `OnWheelPositionChanged` и т.д.

### Не разделяем input/model/view через MVC
В прототипе всё в одном месте. Когда станет сложнее — рефакторим.

### Нет конфига для тестовых данных
Строки захардкодены как List. Можно переделать в ScriptableObject, когда контент будет настоящим.

## Проверка готовности

После настройки Canvas запустить Play:
1. ✅ Появляется первая строка в центре (без искажения)
2. ✅ Через 3 сек появляется вторая строка, колесо медленно едет вверх
3. ✅ Скролить мыши — колесо тут же начинает двигаться вверх/вниз
4. ✅ Отпустить скролл — инерция, потом магнит к ближайшему центру
5. ✅ Краевые строки сжимаются по вертикали и исчезают
6. ✅ При пересечении целых границ звук WheelClick (если звук назначен)

Все 15 критериев из требований достижимы с этой архитектурой.

---

## Следующие шаги (не в этой сессии)

1. Интеграция с InkDialogueRunner (вместо тестовых данных)
2. Привязка live target к текущей реплике Ink
3. Выборы (choices) на отдельных элементах (не в прототипе)
4. Word fade для длинных строк
5. Аудио для WheelClick из Sound Assets

На этом этапе прототип готов к ощущению физики барабана.

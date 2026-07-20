# Dialogue Wheel Prototype — Инструкция настройки

## Файлы

Созданы три скрипта в `Assets/_Project/Scripts/Dialogue/Wheel/`:
1. **DialogueWheelPrototype.cs** — главный контроллер, управляет wheelPosition и логикой
2. **WheelMotionController.cs** — физические фазы движения (Idle, AutoMove, Inertia, Snap)
3. **WheelItemView.cs** — визуальный элемент с цилиндрической проекцией

## Решения архитектуры

### wheelPosition
- **Единая координата** отслеживает положение барабана
- Целые числа (0, 1, 2...) — границы элементов
- Половинки (0.5, 1.5, 2.5...) — центры элементов, где стоит колесо

### FollowMode
- **FollowingLive:** новые строки автоматически двигают барабан, если колесо не может свободно двигаться
- **Detached:** новые строки добавляются, но колесо остаётся на месте (игрок читает историю)

### Физические фазы
1. **Idle** — колесо стоит в центре элемента, никаких сил
2. **AutoMove** — медленное движение к live center (только при FollowingLive)
3. **Inertia** — движение с затуханием после скролла пользователя
4. **Snap** — пружина к ближайшему N+0.5

### Правила
- Пользовательский scroll немедленно: переводит в Detached, прекращает AutoMove, добавляет velocity
- Когда Snap завершится на live center → FollowMode становится FollowingLive
- WheelClick только при пересечении целых границ (если колесо физически двигалось)

## Ручная настройка Canvas

### Шаг 1. Создать иерархию

```
Canvas (ScreenSpace-Overlay)
├── DialogueWheelPanel (Panel, full-screen размер)
│   ├── ContentPanel (Panel, с RectMask2D)
│   │   └── WheelContainer (Panel, центр экрана, размер ~600x400)
│   │       ├── ItemTemplate (TMP_Text, скрыт)
│   │       └── [элементы добавляются сюда при runtime]
```

### Шаг 2. Настроить ContentPanel

- **RectTransform:**
  - Anchors: stretch, stretch (заполняет родителя)
  - Offset: 0, 0, 0, 0

- **Image:** отключить (фон прозрачный)

- **Add Component → RectMask2D**
  - Использовать для обрезки элементов за краями

### Шаг 3. Настроить WheelContainer

- **RectTransform:**
  - Anchor: middle, middle
  - Pos: (0, 0)
  - Size: (600, 400) — настроить под размер диалога
  - Scale: (1, 1, 1)

- **Layout Element:** отключить

### Шаг 4. Настроить ItemTemplate

- **Префаб TMP_Text:**
  - Anchor: middle, middle
  - Pos: (0, 0)
  - Size: (500, 50) — по ширине и высоте элемента

- **TextMeshProUGUI:**
  - Font Size: 36
  - Alignment: Center, Middle
  - Wrapping: Enabled

- **Add Component → CanvasGroup** (для Alpha контроля)

- **Add Component → WheelItemView** (вручную в инспекторе)

- **Отключить GameObject** (видимый, но не активный)

### Шаг 5. Связать через инспектор DialogueWheelPrototype

Создать пустой GameObject на сцене, вешаем DialogueWheelPrototype:

- **Wheel Container:** (drag WheelContainer)
- **Item Template:** (drag ItemTemplate)
- **Content Panel:** (drag ContentPanel)
- **Audio Source:** (создать или перетащить существующий)
- **Wheel Click Clip:** (перетащить звуковой файл или оставить пусто)

## Параметры для настройки ощущения

### WheelMotionController

| Параметр | По умолчанию | Описание |
|---|---|---|
| **Auto Move Speed** | 1.5 | Единиц в секунду при движении к live center. ↑ быстрее, ↓ медленнее |
| **Inertia Friction** | 0.95 | Множитель затухания (0.95 = 5% потери скорости за кадр). Ближе к 1.0 = дольше скользит |
| **Snap Spring Strength** | 15.0 | Жёсткость пружины. ↑ быстрее магнитится, ↓ медленнее и плавнее |
| **Velocity Threshold** | 0.01 | Минимальная скорость перед автоматическим snap. ↓ дольше ждёт полной остановки |
| **Center Tolerance** | 0.02 | Допуск расстояния до центра считается "достигнут" |

### DialogueWheelPrototype

| Параметр | По умолчанию | Описание |
|---|---|---|
| **Scroll Sensitivity** | 0.1 | Множитель скролла. ↑ более чувствительно к движению колеса мыши |
| **Release Detection Time** | 0.5 | Секунды без скролла для определения "отпускания". ↓ быстрее начинает snap |
| **New Line Spawn Interval** | 3.0 | Интервал между добавлением тестовых строк (в секундах) |
| **Test Line Count** | 12 | Количество тестовых строк (по умолчанию 12) |

### WheelItemView

| Параметр | По умолчанию | Описание |
|---|---|---|
| **Angle Step** | 36 | Градусов на один элемент (360 / 10 элементов = 36°). Изменять если количество элементов на "экране" другое |
| **Cylinder Radius** | 100 | Радиус цилиндра (пиксели). ↑ более кривизна, ↓ более плоско |
| **Edge Horizontal Scale** | 0.85 | Минимальный масштаб X на краях. 0.85 = края сжимаются на 15% |
| **Alpha Falloff** | AnimationCurve | Кривая затухания альфы. По умолчанию ease-in-out для плавности |
| **Alpha Depth Power** | 1.5 | Степень для альфа-функции. ↑ более резкое затухание на краях |

## Пример настройки Canvas в сцене

```csharp
// В инспекторе DialogueWheelPrototype:
Wheel Container: [WheelContainer transform]
Item Template: [ItemTemplate TMP_Text]
Content Panel: [ContentPanel transform]
Audio Source: [AudioSource или null]
Wheel Click Clip: [AudioClip или null]

// Тестовые параметры:
Scroll Sensitivity: 0.1
Release Detection Time: 0.5
New Line Spawn Interval: 2.0
Test Line Count: 12
```

## Критерии готовности (проверка)

- [ ] 1. Строки выглядят как передняя поверхность цилиндра (изогнутая лента)
- [ ] 2. В центре нет искажения (ScaleX=1, ScaleY=1, Alpha=1)
- [ ] 3. У краёв текст сжимается по вертикали и горизонтали, плавно исчезает
- [ ] 4. Задняя сторона не видна (depth <= 0 отключает отрисовку)
- [ ] 5. AutoMove ведёт к новой live-строке (движение медленное и плавное)
- [ ] 6. Скролл мыши немедленно прекращает AutoMove
- [ ] 7. Новые строки не тянут колесо при Detached (остаётся на месте, обновляется live target)
- [ ] 8. После скролла есть инерция (скольжение с затуханием)
- [ ] 9. После инерции работает магнит к N+0.5 (Snap фаза)
- [ ] 10. Возврат к live center включает FollowingLive (если snap закончился там)
- [ ] 11. Click звучит только на целых границах (защита от наложения)
- [ ] 12. Snap внутри деления молчит (звук при пересечении границы, не при snap)
- [ ] 13. Колесо не уходит за диапазон элементов (clamped)
- [ ] 14. No legacy Input (используется IScrollHandler)
- [ ] 15. DialogueUI не изменён

## Рекомендации по тестированию

1. **Установить параметры для быстрого тестирования:**
   - New Line Spawn Interval: 1.0 (вместо 3.0)
   - Auto Move Speed: 2.0 (для видимого движения)

2. **Тестировать поведение:**
   - Запустить, подождать первой строки
   - Скроллить вверх/вниз колёсиком мыши
   - Отпустить и посмотреть, как колесо магнитится к центру
   - Наблюдать звуки WheelClick при пересечении целых границ

3. **Настроить ощущение после первого запуска:**
   - Если инерция слишком долгая → ↑ Inertia Friction (ближе к 1.0)
   - Если snap слишком быстрый → ↓ Snap Spring Strength
   - Если AutoMove незаметен → ↑ Auto Move Speed

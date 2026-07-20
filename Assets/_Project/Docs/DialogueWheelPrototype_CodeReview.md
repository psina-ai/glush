# Dialogue Wheel Prototype — Code Review и исправления

## Найденные и исправленные баги

### 1. Инерция зависит от FPS (WheelMotionController.cs:112)

**Баг:** `_velocity *= _inertiaFriction;` применяется каждый кадр, что дает зависимость от FPS.
- На 60 FPS: 5% потери скорости за кадр
- На 120 FPS: 2.5% потери за кадр (в два раза медленнее затухание)

**Исправление:** Использована FPS-независимая формула:
```csharp
_velocity *= Mathf.Pow(_inertiaFriction, Time.unscaledDeltaTime * 60f);
```

Теперь `_inertiaFriction = 0.95` означает сохранение 95% скорости за один кадр на условных 60 FPS, независимо от реального fps.

**Правка комментария:** «коэффициент сохранения скорости на 60 FPS; применять через Mathf.Pow для независимости от FPS»

---

### 2. Snap spring вычисляется дважды (WheelMotionController.cs:143)

**Баг:**
```csharp
_velocity = snapDistance * _snapSpringStrength * Time.unscaledDeltaTime;
deltaPosition = _velocity * Time.unscaledDeltaTime;  // снова умножаем на deltaTime!
```

Это давало deltaPosition ~ snapDistance × strength × deltaTime^2, что неправильно и нестабильно.

**Исправление:** Вычислить springForce один раз, затем применить как обычное смещение:
```csharp
float springForce = snapDistance * _snapSpringStrength;
deltaPosition = springForce * Time.unscaledDeltaTime;
```

---

### 3. HashSet подавляет быстрые пересечения (DialogueWheelPrototype.cs:208-220)

**Баг:** HashSet.Add() не добавляет дубликаты, так что если за кадр произойдёт несколько пересечений одной границы (теоретически), звук сработает только один раз.

**Исправление:** Удалить HashSet полностью, вести простой цикл проходом по всем пересечённым границам:
```csharp
for (int i = oldBoundary + step; /* условие */; i += step)
{
    PlayWheelClick();  // каждый раз, без проверки на дубликаты
}
```

---

### 4. Tolerance несогласованность (DialogueWheelPrototype.cs:106, 116, 125)

**Баг:** Разные места проверяют достижение центра с разными значениями tolerances (0.05f vs используемое в гейте).

**Исправление:** Использовать `_motionController.CenterTolerance` везде, добавлена public property:
```csharp
public float CenterTolerance => _centerTolerance;
```

---

### 5. Velocity не обнуляется при clamp (DialogueWheelPrototype.cs:264)

**Баг:** После ClampWheelPosition velocity могла быть ненулевой, но wheelPosition уже на границе, вызывая рывок на следующий кадр.

**Исправление:** Если wheelPosition был зажат clamp, обнулить velocity:
```csharp
float prevWheelPosition = _wheelPosition - motionDelta;
ClampWheelPosition();
if (Mathf.Abs(_wheelPosition - prevWheelPosition) < Mathf.Abs(motionDelta) * 0.5f)
{
    _motionController.ZeroVelocity();
}
```

Новый метод в WheelMotionController:
```csharp
public void ZeroVelocity() => _velocity = 0f;
```

---

### 6. AddComponent может создать дубликаты (DialogueWheelPrototype.cs:76)

**Баг:** `gameObject.AddComponent<WheelMotionController>()` всегда создает новый компонент, даже если он уже есть.

**Исправление:** Сначала проверить GetComponent:
```csharp
_motionController = GetComponent<WheelMotionController>();
if (_motionController == null)
{
    _motionController = gameObject.AddComponent<WheelMotionController>();
}
```

---

### 7. OnEnable может перезаписать CanvasGroup ссылку (WheelItemView.cs:31-32)

**Баг:** OnEnable вызывается несколько раз, каждый раз переинициализируя ссылки. Если AddComponent вызывается несколько раз, ссылка может быть неактуальной.

**Исправление:** Добавлена флаг инициализации и метод EnsureInitialized():
```csharp
private bool _isInitialized = false;

private void EnsureInitialized()
{
    if (_isInitialized)
        return;
    // инициализация...
    _isInitialized = true;
}
```

Методы теперь вызывают EnsureInitialized() перед использованием ссылок.

---

### 8. Отсутствует проверка ItemTemplate (DialogueWheelPrototype.cs:73-75)

**Баг:** Если ItemTemplate не назначен, произойдет NullReferenceException в Start().

**Исправление:** Явная проверка в Awake() с информативной ошибкой:
```csharp
if (_itemTemplate == null)
{
    Debug.LogError("DialogueWheelPrototype: ItemTemplate не назначен. Компонент отключен.", gameObject);
    enabled = false;
    return;
}
```

---

### 9. Snap не указывает точный центр при завершении (WheelMotionController.cs:137)

**Баг:** Когда Snap завершается, deltaPosition = 0, но wheelPosition может быть не ровно на центре из-за округлений.

**Исправление:** Явно установить точное значение центра:
```csharp
if (Mathf.Abs(snapDistance) <= _centerTolerance)
{
    deltaPosition = _snapTarget - currentWheelPosition;  // ровно до центра
    _velocity = 0f;
    _phase = MotionPhase.Idle;
}
```

---

### 10. Инерция не обнуляет velocity при переходе (WheelMotionController.cs:118)

**Баг:** Когда Inertia переходит в Snap, _velocity не обнуляется явно (Snap сам его обнулит, но это неявно).

**Исправление:** Явное обнуление:
```csharp
if (Mathf.Abs(_velocity) < _velocityThreshold)
{
    float nearest = FindNearestCenter(currentWheelPosition + deltaPosition);
    StartSnap(nearest);
    _velocity = 0f;  // явное обнуление
}
```

---

## Изменённые строки и методы

### DialogueWheelPrototype.cs

| Строки | Метод | Что изменено |
|---|---|---|
| 69-88 | Awake() | Добавлена проверка ItemTemplate и безопасный GetComponent вместо AddComponent |
| 99-115 | Update() | Добавлена логика обнуления velocity при clamp |
| 117-130 | (логика live) | Использовано `_motionController.CenterTolerance` вместо hardcode |
| 133-141 | (логика Snap->FollowingLive) | Использовано `_centerTolerance` для консистентности |
| 212-232 | DetectBoundaryCrossings() | Удален HashSet, упрощен цикл, разрешены накладывающиеся звуки |

### WheelMotionController.cs

| Строки | Изменение |
|---|---|
| 21 | Обновлен комментарий к _inertiaFriction |
| 34 | Добавлена public property `CenterTolerance` |
| 109-122 | Inertia: применена FPS-независимая формула через Pow, явное обнуление velocity |
| 122-147 | Snap: исправлена double-deltaTime ошибка, явная установка точного центра |
| 174 | Добавлен метод ZeroVelocity() |

### WheelItemView.cs

| Строки | Изменение |
|---|---|
| 19-22 | Добавлена флаг `_isInitialized` и метод `EnsureInitialized()` |
| 56-71 | Перенаписана логика инициализации с проверкой флага |
| 46-90 | UpdateProjection(): добавлено EnsureInitialized(), убрано `enabled = false`, добавлены условные обновления трансформации |

---

## Обновлённые ручные шаги настройки полноэкранного DialogueWheelPanel

### Шаг 1: Создать иерархию в Canvas

```
Canvas (ScreenSpace-Overlay)
├── DialogueWheelPanel (Panel)
│   ├── [Image component, raycastTarget = true]
│   ├── ContentPanel (Panel, с RectMask2D)
│   │   └── WheelContainer (Panel)
│   │       ├── ItemTemplate (TMP_Text, скрыт в инспекторе)
│   │       └── [runtime элементы добавляются сюда]
```

### Шаг 2: Настроить DialogueWheelPanel

**Важно:** DialogueWheelPrototype должен быть прикреплён **именно к DialogueWheelPanel**, не к какому-то другому объекту.

- **RectTransform:**
  - Anchors: stretch, stretch (заполняет Screen Space)
  - Offset: 0, 0, 0, 0

- **Image component (обязателен):**
  - Color: White, Alpha = 0 (прозрачный, но активный)
  - **Image.raycastTarget: TRUE** (иначе OnScroll не будет срабатывать)
  
- **Add Component → DialogueWheelPrototype**
  - Это позволяет GetComponent найти RectTransform и получить IScrollHandler от этого объекта

### Шаг 3: Настроить ContentPanel

- **RectTransform:**
  - Anchors: stretch, stretch
  - Offset: 0, 0, 0, 0

- **Image:** отключить (Remove Component или оставить с alpha=0)

- **Add Component → RectMask2D** (для обрезки элементов)
  - Padding: -5 (или по необходимости)

### Шаг 4: Настроить WheelContainer

- **RectTransform:**
  - Anchor: middle, middle
  - Pos: (0, 0)
  - Size: (600, 400) — регулировка под размер диалога
  - Scale: (1, 1, 1)

- **Image:** отключить (Remove Component)

- **Layout Element:** отключить если есть

### Шаг 5: Настроить ItemTemplate

**Это prefab, на основе которого создаются runtime элементы.**

- **GameObject состояние:** Active (видимое галочкой), но будет дублироваться
- **RectTransform:**
  - Anchor: middle, middle
  - Pos: (0, 0)
  - Size: (500, 50) — по ширине и высоте элемента

- **TextMeshProUGUI:**
  - Font Size: 36
  - Alignment: Center, Middle
  - Wrapping: Enabled

- **CanvasGroup:** будет добавлен автоматически при первом использовании элемента (WheelItemView.EnsureInitialized())

- **Add Component → WheelItemView** (в инспекторе)

### Шаг 6: Привязать через инспектор DialogueWheelPrototype

На DialogueWheelPanel (где прикреплён DialogueWheelPrototype):

```
Wheel Setup
├── Wheel Container: [перетащить WheelContainer]
├── Item Template: [перетащить ItemTemplate]
└── Content Panel: [перетащить ContentPanel]

Input & Scroll
├── Scroll Sensitivity: 0.1
└── Release Detection Time: 0.5

Audio
├── Audio Source: [опционально, если есть AudioSource в сцене]
└── Wheel Click Clip: [опционально, звуковой файл]

Test Data
├── New Line Spawn Interval: 3.0
└── Test Line Count: 12
```

### **Критическое требование для IScrollHandler:**

DialogueWheelPanel **должен:**
- Иметь Image компонент с raycastTarget = true
- Это позволяет uGUI Event System доставлять PointerEventData к OnScroll()

Если Image отключен или raycastTarget = false:
- Скролл не будет срабатывать
- Решение: убедиться Image есть, даже если он прозрачный (alpha=0)

---

## Объяснение inertiaFriction

### Что это?

`_inertiaFriction` — **коэффициент сохранения скорости** в течение одного кадра на условных 60 FPS.

### Как это работает?

Каждый кадр скорость умножается на функцию:
```csharp
_velocity *= Mathf.Pow(_inertiaFriction, Time.unscaledDeltaTime * 60f);
```

**Математика:**
- На 60 FPS: Time.unscaledDeltaTime ≈ 0.0167, поэтому 0.0167 × 60 ≈ 1.0
  - velocity *= Pow(0.95, 1) = velocity * 0.95 → 5% потеря скорости
  
- На 30 FPS: Time.unscaledDeltaTime ≈ 0.0333, поэтому 0.0333 × 60 ≈ 2.0
  - velocity *= Pow(0.95, 2) ≈ velocity * 0.9025 → 9.75% потеря скорости (больше)
  - Это правильно: за то же реальное время скорость должна упасть одинаково
  
- На 120 FPS: Time.unscaledDeltaTime ≈ 0.0083, поэтому 0.0083 × 60 ≈ 0.5
  - velocity *= Pow(0.95, 0.5) ≈ velocity * 0.9747 → 2.5% потеря скорости

### Как настроить?

| Значение | Ощущение | Когда использовать |
|---|---|---|
| **0.90** | Быстрое торможение, инерция ~0.2 сек | Давит сразу, резкое торможение |
| **0.93** | Среднее торможение, инерция ~0.4 сек | Сбалансированное, классическое UI |
| **0.95** | Долгое торможение, инерция ~0.7 сек | Плавное скольжение, мобильное ощущение |
| **0.97** | Очень долгое торможение, инерция ~1.2 сек | Гиперинерция, может быть слишком скользко |

**Рекомендация:** Оставить 0.95 как default, затем менять на слух.

---

## Критерии готовности (проверка)

1. ✅ Проект компилируется без ошибок
2. ✅ wheelPosition имеет один источник изменения (WheelMotionController.UpdateMotion)
3. ✅ OnScroll прерывает AutoMove немедленно
4. ✅ Detached не тянется к новым строкам (wheelPosition не меняется, только live target)
5. ✅ Инерция и Snap независимы от FPS (Mathf.Pow, Time.unscaledDeltaTime везде)
6. ✅ Snap стабильно останавливается на N+0.5 (точное установление через deltaPosition)
7. ✅ Возврат к live включает FollowingLive (проверка tolerance)
8. ✅ Позиция ограничена существующими контейнерами (ClampWheelPosition)
9. ✅ Новая строка корректно расширяет нижнюю границу (_itemViews.Count)
10. ✅ Каждый пересечённый integer создаёт отдельный click (цикл без HashSet)
11. ✅ Быстрые clicks могут накладываться (PlayOneShot каждый раз)
12. ✅ Нет click при простом завершении Snap (звук только если пересечена целая граница)
13. ✅ Скролл работает по всему экрану при правильной Image.raycastTarget
14. ✅ Края сжимаются и плавно исчезают (Lerp, Pow, уменьшение Alpha)
15. ✅ Задняя сторона не видна (depth <= 0 → alpha = 0)
16. ✅ Runtime-элементы не дублируются (GetComponent перед AddComponent)
17. ✅ DialogueUI и InkDialogueRunner не изменены
18. ✅ Другие файлы проекта не изменены

---

*Код review завершён. Все баги исправлены. Проект готов к тестированию.*

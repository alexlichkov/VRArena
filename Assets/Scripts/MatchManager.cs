using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Централизованный менеджер матча с конечным автоматом состояний.
/// Управляет фазами раунда (Preparation → Traps → Combat → RoundEnd → MatchOver),
/// отслеживает время в каждом состоянии и рассылает события подписчикам.
/// </summary>
public class MatchManager : MonoBehaviour
{
    /// <summary>
    /// Состояния матча.
    /// Idle      — ничего не происходит (до старта или после MatchOver).
    /// Preparation — игроки ставят баррикады на своём острове.
    /// Traps     — игроки ставят ловушки на острове противника.
    /// Combat    — активная фаза боя.
    /// RoundEnd  — подведение итогов раунда.
    /// MatchOver — матч завершён (все раунды сыграны).
    /// </summary>
    public enum MatchState
    {
        Idle,
        Preparation,
        Traps,
        Combat,
        RoundEnd,
        MatchOver
    }

    #region Singleton

    /// <summary>Простейший Singleton для глобального доступа.</summary>
    public static MatchManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    private void Awake()
    {
        // Мягкий Singleton: если уже есть Instance — уничтожаем дубль
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"[{nameof(MatchManager)}] Дубликат обнаружен на объекте {name}. " +
                $"Существующий экземпляр уже есть на {Instance.name}. Уничтожаю этот.",
                this);

            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Нормализуем количество раундов
        if (_totalRounds < 1)
            _totalRounds = 1;
    }

    #endregion

    #region Публичные свойства состояния / времени

    /// <summary>Текущее состояние матча.</summary>
    public MatchState CurrentState { get; private set; } = MatchState.Idle;

    /// <summary>Сколько времени прошло в текущем состоянии (сек).</summary>
    public float TimeInState { get; private set; }

    /// <summary>
    /// Сколько осталось до конца состояния (если есть длительность).
    /// Возвращает -1, если длительность не задана (бесконечное состояние).
    /// </summary>
    public float TimeRemainingInState =>
        _currentStateDuration > 0f ? Mathf.Max(_currentStateDuration - TimeInState, 0f) : -1f;

    #endregion

    #region Настройки автоматики

    [Header("Auto-flow settings")]
    [SerializeField]
    private bool _autoStartOnPlay = true;

    [SerializeField]
    private bool _autoAdvanceStates = true;

    #endregion

    #region Длительности фаз

    [Header("Phase durations (seconds)")]
    [SerializeField, Min(0f)]
    private float _preparationDuration = 30f;

    [SerializeField, Min(0f)]
    private float _trapsDuration = 30f;

    [SerializeField, Min(0f)]
    private float _combatDuration = 60f;

    [SerializeField, Min(0f)]
    private float _roundEndDuration = 10f;

    #endregion

    #region Счёт / раунды

    [Header("Score")]
    [SerializeField]
    private int _currentScore;

    /// <summary>Текущие очки (суммарный счёт матча, если нужен).</summary>
    public int CurrentScore => _currentScore;

    [Header("Rounds")]
    [SerializeField, Min(1)]
    private int _totalRounds = 3;

    /// <summary>Сколько раундов в матче (для UI/логики).</summary>
    public int TotalRounds => _totalRounds;

    /// <summary>Текущий раунд, начиная с 1.</summary>
    public int CurrentRound { get; private set; } = 1;

    /// <summary>
    /// Событие смены раунда: (текущий, всего).
    /// Можно повесить звук, UI-анимацию или сетевую синхронизацию.
    /// </summary>
    public event Action<int, int> RoundChanged;

    #endregion

    #region События

    [System.Serializable]
    public class MatchStateChangedEvent : UnityEvent<MatchState, MatchState> { }

    [Header("Events")]
    [Tooltip("UnityEvent для привязки логики из инспектора (UI, звуки и т.п.). " +
             "Параметры: (старое состояние, новое состояние).")]
    [SerializeField]
    private MatchStateChangedEvent _onMatchStateChanged = new MatchStateChangedEvent();

    /// <summary>
    /// UnityEvent для привязки из инспектора (например, UI, звуки).
    /// Параметры: (старое состояние, новое состояние).
    /// </summary>
    public MatchStateChangedEvent OnMatchStateChanged => _onMatchStateChanged;

    /// <summary>
    /// C#-событие для кода. Параметры: (старое состояние, новое состояние).
    /// </summary>
    public event Action<MatchState, MatchState> MatchStateChanged;

    /// <summary>
    /// Событие завершения матча (когда отыграны все раунды).
    /// </summary>
    public event Action MatchOver;

    #endregion

    #region Debug

    [Header("Debug")]
    [SerializeField]
    private bool _enableDebugHotkeys = false;

    #endregion

    private float _currentStateDuration;

    private void Start()
    {
        // Инициализируем раунд и состояние на старте
        CurrentRound = 1;
        RoundChanged?.Invoke(CurrentRound, TotalRounds);

        if (_autoStartOnPlay)
        {
            StartMatch();
        }
        else
        {
            // Явно выставляем Idle, чтобы не было сюрпризов
            ChangeState(MatchState.Idle);
        }
    }

    private void Update()
    {
        // В Idle и MatchOver ничего не делаем
        if (CurrentState == MatchState.Idle || CurrentState == MatchState.MatchOver)
            return;

        TimeInState += Time.deltaTime;

        // Автоматическое переключение по истечении длительности состояния
        if (_autoAdvanceStates && _currentStateDuration > 0f && TimeInState >= _currentStateDuration)
        {
            AdvanceStateByTime();
        }

        HandleDebugHotkeys();
    }

    #region API для внешнего кода

    /// <summary>
    /// Запуск матча с первого раунда. Можно вызвать из лобби/меню.
    /// </summary>
    public void StartMatch()
    {
        CurrentRound = 1;
        RoundChanged?.Invoke(CurrentRound, TotalRounds);

        ChangeState(MatchState.Preparation);
    }

    /// <summary>
    /// Явное завершение боевой фазы по уничтожению команды (а не по таймеру).
    /// Вызывается, когда одна из команд полностью выбита.
    /// </summary>
    public void EndCombatByElimination()
    {
        if (CurrentState != MatchState.Combat)
            return;

        ChangeState(MatchState.RoundEnd);
    }

    /// <summary>
    /// Добавить очки матчу. Можно вызывать из мишеней, убийств игроков и т.п.
    /// </summary>
    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        _currentScore += amount;
        Debug.Log($"[{nameof(MatchManager)}] Score +{amount} = {_currentScore}", this);
    }

    #endregion

    #region FSM: смена состояний

    /// <summary>
    /// Смена состояния матча.
    /// </summary>
    public void ChangeState(MatchState newState)
    {
        if (newState == CurrentState)
            return;

        var oldState = CurrentState;

        CurrentState = newState;
        TimeInState = 0f;
        _currentStateDuration = GetDurationForState(newState);

        Debug.Log(
            $"[{nameof(MatchManager)}] State changed: {oldState} → {newState} (duration = {_currentStateDuration})",
            this);

        _onMatchStateChanged?.Invoke(oldState, newState);
        MatchStateChanged?.Invoke(oldState, newState);
    }

    /// <summary>
    /// Переход к следующему состоянию по таймеру.
    /// Preparation → Traps → Combat → RoundEnd → (следующий раунд / MatchOver).
    /// </summary>
    private void AdvanceStateByTime()
    {
        switch (CurrentState)
        {
            case MatchState.Preparation:
                ChangeState(MatchState.Traps);
                break;

            case MatchState.Traps:
                ChangeState(MatchState.Combat);
                break;

            case MatchState.Combat:
                // Время боя вышло → конец раунда по таймеру
                ChangeState(MatchState.RoundEnd);
                break;

            case MatchState.RoundEnd:
                HandleRoundEndAndMaybeNextRound();
                break;
        }
    }

    /// <summary>
    /// Обработка конца раунда:
    /// либо идём на следующий раунд, либо завершаем матч.
    /// </summary>
    private void HandleRoundEndAndMaybeNextRound()
    {
        // Сообщаем, что раунд завершён
        // (можно добавить отдельное событие OnRoundEnded при необходимости)

        if (CurrentRound < _totalRounds)
        {
            // Следующий раунд
            CurrentRound++;
            RoundChanged?.Invoke(CurrentRound, TotalRounds);
            ChangeState(MatchState.Preparation);
        }
        else
        {
            // Все раунды сыграны — матч завершён
            ChangeState(MatchState.MatchOver);
            MatchOver?.Invoke();
        }
    }

    /// <summary>
    /// Длительность для каждого состояния.
    /// Если вернуть 0 — состояние будет "бесконечным" (пока не переключишь вручную).
    /// </summary>
    private float GetDurationForState(MatchState state)
    {
        switch (state)
        {
            case MatchState.Preparation:
                return _preparationDuration;
            case MatchState.Traps:
                return _trapsDuration;
            case MatchState.Combat:
                return _combatDuration;
            case MatchState.RoundEnd:
                return _roundEndDuration;
            case MatchState.Idle:
            case MatchState.MatchOver:
            default:
                return 0f;
        }
    }

    #endregion

    #region Debug hotkeys

    /// <summary>
    /// Простейшие хоткеи (F1–F4) для дебага во время Play Mode.
    /// </summary>
    private void HandleDebugHotkeys()
    {
        if (!_enableDebugHotkeys)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
            ChangeState(MatchState.Preparation);
        if (Input.GetKeyDown(KeyCode.F2))
            ChangeState(MatchState.Traps);
        if (Input.GetKeyDown(KeyCode.F3))
            ChangeState(MatchState.Combat);
        if (Input.GetKeyDown(KeyCode.F4))
            ChangeState(MatchState.RoundEnd);
    }

    #endregion
}

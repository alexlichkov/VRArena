using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Централизованный менеджер матча с простой конечной автоматикой состояний.
/// Управляет фазами раунда (Preparation → Combat → RoundEnd),
/// отслеживает время в каждом состоянии и рассылает события подписчикам.
/// 
/// Подход:
/// - Есть enum MatchState (конечное число состояний).
/// - MatchManager хранит текущий state и время, прошедшее в нём.
/// - При смене состояний вызывает C# event и UnityEvent, чтобы другие
///   системы могли подписаться (DefenseSlot, оружие, UI и т.д.).
/// 
/// Это реализация "глобального стейт-машина" через enum + switch:
/// https://gamedevbeginner.com/state-machines-in-unity-how-and-when-to-use-them/
/// </summary>
public class MatchManager : MonoBehaviour
{
    /// <summary>
    /// Состояния матча. Пока достаточно трёх:
    /// - Idle        — ничего не происходит (до старта или для паузы).
    /// - Preparation — игроки расставляют укрепления, ловушки и т.п.
    /// - Combat      — активная фаза боя.
    /// - RoundEnd    — подведение итогов, показ результатов.
    /// </summary>
    public enum MatchState
    {
        Idle,
        Preparation,
        Combat,
        RoundEnd
    }

    /// <summary>
    /// Простейший синглтон для глобального доступа.
    /// В проде обычно GameManager либо делают синглтоном,
    /// либо проворачивают DI / сервис-локатор. Здесь — мягкий Singleton.
    /// </summary>
    public static MatchManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

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

    [Header("Auto-flow settings")]
    [SerializeField]
    private bool _autoStartOnPlay = true;

    [SerializeField]
    private bool _autoAdvanceStates = true;

    [Header("Phase durations (seconds)")]
    [SerializeField, Min(0f)]
    private float _preparationDuration = 30f;

    [SerializeField, Min(0f)]
    private float _combatDuration = 60f;

    [SerializeField, Min(0f)]
    private float _roundEndDuration = 10f;

    [Header("Score")]
    [SerializeField]
    private int _currentScore;

    /// <summary>Текущие очки (например, суммарный счёт игрока или команды).</summary>
    public int CurrentScore => _currentScore;

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

    [Header("Debug")]
    [SerializeField]
    private bool _enableDebugHotkeys = false;

    private float _currentStateDuration;

    private void Awake()
    {
        // Мягкий Singleton: если уже есть Instance — уничтожаем дубль, чтобы не было двух менеджеров.
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
    }

    private void Start()
    {
        if (_autoStartOnPlay)
        {
            ChangeState(MatchState.Preparation);
        }

            // Нормализуем количество раундов
        if (_totalRounds < 1)
            _totalRounds = 1;

        CurrentRound = 1;
        RoundChanged?.Invoke(CurrentRound, TotalRounds);

        if (_autoStartOnPlay)
        {
            ChangeState(MatchState.Preparation);
        }
    }

    private void Update()
    {
        if (CurrentState == MatchState.Idle)
            return;

        TimeInState += Time.deltaTime;

        // Автоматическое переключение по истечении длительности состояния
        if (_autoAdvanceStates && _currentStateDuration > 0f && TimeInState >= _currentStateDuration)
        {
            AdvanceState();
        }

        HandleDebugHotkeys();
    }

    /// <summary>
    /// Смена состояния матча. Здесь центральная логика FSM:
    /// - запоминаем старое состояние;
    /// - обновляем CurrentState;
    /// - сбрасываем таймер TimeInState;
    /// - вычисляем длительность нового состояния;
    /// - оповещаем всех подписчиков (UnityEvent + C# event).
    /// </summary>
    public void ChangeState(MatchState newState)
    {
        if (newState == CurrentState)
            return;

        var oldState = CurrentState;

        CurrentState = newState;
        TimeInState = 0f;
        _currentStateDuration = GetDurationForState(newState);

        Debug.Log($"[{nameof(MatchManager)}] State changed: {oldState} → {newState} (duration = {_currentStateDuration})", this);

        OnMatchStateChanged?.Invoke(oldState, newState);
        MatchStateChanged?.Invoke(oldState, newState);
    }

    /// <summary>
    /// Переход к следующему состоянию по простому сценарию:
    /// Preparation → Combat → RoundEnd → Preparation (цикл).
    /// Это можно будет заменить на более сложную логику матчмейкинга / сетевого матча.
    /// </summary>
    private void AdvanceState()
    {
        switch (CurrentState)
        {
            case MatchState.Preparation:
                ChangeState(MatchState.Combat);
                break;

            case MatchState.Combat:
                ChangeState(MatchState.RoundEnd);
                break;

            case MatchState.RoundEnd:
                // Переход к следующему раунду.
                if (_totalRounds > 0)
                {
                    if (CurrentRound < _totalRounds)
                    {
                        CurrentRound++;
                        RoundChanged?.Invoke(CurrentRound, TotalRounds);
                    }
                    else
                    {
                        // Все раунды сыграны — здесь можно будет сделать отдельное состояние MatchOver.
                        // Пока просто начинаем "новый матч" с первого раунда.
                        CurrentRound = 1;
                        RoundChanged?.Invoke(CurrentRound, TotalRounds);
                    }
                }

                ChangeState(MatchState.Preparation);
                break;
        }
    }


    /// <summary>
    /// Настройка длительности для каждого состояния.
    /// Если вернуть 0 — состояние будет "бесконечным" (пока не переключишь вручную).
    /// </summary>
    private float GetDurationForState(MatchState state)
    {
        switch (state)
        {
            case MatchState.Preparation:
                return _preparationDuration;
            case MatchState.Combat:
                return _combatDuration;
            case MatchState.RoundEnd:
                return _roundEndDuration;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Добавить очки матчу. Можно вызывать из мишеней, убийства игроков и т.п.
    /// </summary>
    public void AddScore(int amount)
    {
        if (amount <= 0)
            return;

        _currentScore += amount;
        Debug.Log($"[{nameof(MatchManager)}] Score +{amount} = {_currentScore}", this);
    }

    /// <summary>
    /// Простейшие хоткеи (F1/F2/F3) для дебага во время Play Mode.
    /// Используем старый Input только для редактора/отладки — в проде лучше завести отдельный Dev-панель/UI.
    /// </summary>
    private void HandleDebugHotkeys()
    {
        if (!_enableDebugHotkeys)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
            ChangeState(MatchState.Preparation);
        if (Input.GetKeyDown(KeyCode.F2))
            ChangeState(MatchState.Combat);
        if (Input.GetKeyDown(KeyCode.F3))
            ChangeState(MatchState.RoundEnd);
    }

    [Header("Rounds")]
    [SerializeField, Min(1)]
    private int _totalRounds = 3;

    /// <summary>Сколько раундов в матче (для UI/логики).</summary>
    public int TotalRounds => _totalRounds;

    /// <summary>Текущий раунд, начиная с 1.</summary>
    public int CurrentRound { get; private set; } = 1;

    /// <summary>
    /// Событие смены раунда: (текущий, всего).
    /// В будущем сюда же можно повесить звук, UI-анимации и сетевую синхронизацию.
    /// </summary>
    public event Action<int, int> RoundChanged;

}

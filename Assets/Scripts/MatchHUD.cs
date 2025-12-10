using UnityEngine;
using TMPro;

/// <summary>
/// HUD матча: показывает фазу, таймер, раунд и счёт.
/// Слушает MatchManager и обновляет текстовые поля.
/// </summary>
public class MatchHUD : MonoBehaviour
{
    [Header("Text fields")]
    [SerializeField] private TextMeshProUGUI _phaseText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _roundText;
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("Formatting")]
    [SerializeField] private string _phaseFormat = "Фаза: {0}";
    [SerializeField] private string _roundFormat = "Раунд: {0} / {1}";
    [SerializeField] private string _scoreFormat = "Счёт: {0}";
    [SerializeField] private string _noTimerText = "--:--";

    private MatchManager _match;

    private void Start()
    {
        _match = MatchManager.Instance;
        if (_match == null)
        {
            Debug.LogError($"[{nameof(MatchHUD)}] Нет MatchManager в сцене.");
            enabled = false;
            return;
        }

        // Подписываемся на события изменения состояния и раундов.
        _match.MatchStateChanged += OnMatchStateChanged;
        _match.RoundChanged += OnRoundChanged;

        // Инициализируем UI текущими значениями.
        OnMatchStateChanged(MatchManager.MatchState.Idle, _match.CurrentState);
        OnRoundChanged(_match.CurrentRound, _match.TotalRounds);
        UpdateScore(_match.CurrentScore);
    }

    private void OnDestroy()
    {
        if (_match != null)
        {
            _match.MatchStateChanged -= OnMatchStateChanged;
            _match.RoundChanged -= OnRoundChanged;
        }
    }

    private void Update()
    {
        if (_match == null)
            return;

        UpdateTimer();
        UpdateScore(_match.CurrentScore);
    }

    private void OnMatchStateChanged(MatchManager.MatchState oldState, MatchManager.MatchState newState)
    {
        if (_phaseText == null)
            return;

        string phaseName = GetPhaseDisplayName(newState);
        _phaseText.text = string.Format(_phaseFormat, phaseName);
    }

    private void OnRoundChanged(int current, int total)
    {
        if (_roundText == null)
            return;

        _roundText.text = string.Format(_roundFormat, current, total);
    }

    private void UpdateScore(int score)
    {
        if (_scoreText == null)
            return;

        _scoreText.text = string.Format(_scoreFormat, score);
    }

    private void UpdateTimer()
    {
        if (_timerText == null)
            return;

        float remaining = _match.TimeRemainingInState;
        if (remaining < 0f)
        {
            _timerText.text = _noTimerText;
            return;
        }

        int seconds = Mathf.CeilToInt(remaining);
        int minutes = seconds / 60;
        int sec = seconds % 60;

        _timerText.text = $"{minutes:00}:{sec:00}";
    }

    private string GetPhaseDisplayName(MatchManager.MatchState state)
    {
        // Здесь имена, как они видятся игроку.
        switch (state)
        {
            case MatchManager.MatchState.Preparation:
                return "Подготовка (баррикады)";

            case MatchManager.MatchState.Traps:
                return "Фаза ловушек";

            case MatchManager.MatchState.Combat:
                return "Бой";

            case MatchManager.MatchState.RoundEnd:
                return "Завершение раунда";

            case MatchManager.MatchState.MatchOver:
                return "Матч завершён";

            case MatchManager.MatchState.Idle:
            default:
                return "Ожидание";
        }
    }
}

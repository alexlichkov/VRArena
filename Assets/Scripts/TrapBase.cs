using UnityEngine;

/// <summary>
/// Базовый класс для всех ловушек.
/// Хранит команду владельца и состояние "вооружена / разоружена".
/// Сам подписывается на MatchManager и:
/// - вооружается в фазе Combat;
/// - разоружается во всех остальных фазах.
/// </summary>
public abstract class TrapBase : MonoBehaviour
{
    [Header("Trap State")]
    [SerializeField]
    protected bool isArmed;

    [SerializeField]
    private PlayerTeam.Team ownerTeam = PlayerTeam.Team.None;

    /// <summary>Команда игрока, который поставил ловушку.</summary>
    public PlayerTeam.Team OwnerTeam => ownerTeam;

    /// <summary>Текущее состояние ловушки.</summary>
    public bool IsArmed => isArmed;

    protected virtual void OnEnable()
    {
        if (MatchManager.HasInstance)
            MatchManager.Instance.MatchStateChanged += OnMatchStateChanged;
    }

    protected virtual void OnDisable()
    {
        if (MatchManager.HasInstance)
            MatchManager.Instance.MatchStateChanged -= OnMatchStateChanged;
    }

    /// <summary>
    /// Вызывается слотами, когда ловушка помещена в TrapSlot.
    /// Здесь мы запоминаем, какой командой она была поставлена.
    /// </summary>
    public virtual void OnPlacedInSlot(TrapSlot slot)
    {
        if (slot != null)
        {
            ownerTeam = slot.AllowedBuilderTeam;
        }
    }

    /// <summary>
    /// Вызывается слотами, когда ловушку убрали из TrapSlot.
    /// По умолчанию просто разоружаем её.
    /// </summary>
    public virtual void OnRemovedFromSlot(TrapSlot slot)
    {
        Disarm();
    }

    /// <summary>
    /// Реакция на смену фазы матча.
    /// Обрати внимание: логика завязана на MatchManager.MatchState.
    /// </summary>
    protected virtual void OnMatchStateChanged(MatchManager.MatchState oldState, MatchManager.MatchState newState)
    {
        switch (newState)
        {
            case MatchManager.MatchState.Combat:
                Arm();
                break;

            case MatchManager.MatchState.Preparation:
            case MatchManager.MatchState.Traps:
            case MatchManager.MatchState.RoundEnd:
            case MatchManager.MatchState.MatchOver:
            case MatchManager.MatchState.Idle:
                Disarm();
                break;
        }
    }

    /// <summary>Вооружить ловушку (активировать).</summary>
    protected virtual void Arm()
    {
        isArmed = true;
    }

    /// <summary>Разоружить ловушку (сделать безопасной).</summary>
    protected virtual void Disarm()
    {
        isArmed = false;
    }
}

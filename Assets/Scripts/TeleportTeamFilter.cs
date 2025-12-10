using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Настраивает Interaction Layer Mask телепорт-рейя
/// в зависимости от:
/// 1) команды игрока;
/// 2) текущей фазы матча (через MatchManager).
///
/// Логика:
/// - В фазах:
///     Idle, Preparation, Combat, RoundEnd, MatchOver
///   игрок может телепортироваться только на СВОЙ остров.
/// - В фазе:
///     Traps
///   игрок уже принудительно перемещён на ОСТРОВ ПРОТИВНИКА
///   и может телепортироваться ТОЛЬКО внутри этого острова.
/// </summary>
public sealed class TeleportTeamFilter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Ссылка на PlayerTeam (обычно висит на XR Origin). " +
             "Если не указана, попробуем найти на объекте или в родителях.")]
    [SerializeField]
    private PlayerTeam _playerTeam;

    [Tooltip("XRRayInteractor, который отвечает за телепорт-луч. " +
             "Если не указан, попробуем найти на объекте или в детях.")]
    [SerializeField]
    private XRRayInteractor _teleportRayInteractor;

    [Header("Interaction Layers per island")]
    [Tooltip("Слои TeleportationArea для острова TeamA.")]
    [SerializeField]
    private InteractionLayerMask _teamAIslandLayers;

    [Tooltip("Слои TeleportationArea для острова TeamB.")]
    [SerializeField]
    private InteractionLayerMask _teamBIslandLayers;

    private void Awake()
    {
        // PlayerTeam: берём из поля, потом пытаемся найти на объекте/в родителях.
        if (_playerTeam == null)
        {
            _playerTeam = GetComponent<PlayerTeam>() ?? GetComponentInParent<PlayerTeam>();
            if (_playerTeam == null)
            {
                Debug.LogError(
                    $"[{nameof(TeleportTeamFilter)}] PlayerTeam не найден " +
                    $"ни на {name}, ни у родителей. Скрипт работать не сможет.",
                    this);
            }
        }

        // XRRayInteractor: берём из поля, потом пытаемся найти на объекте/в детях.
        if (_teleportRayInteractor == null)
        {
            _teleportRayInteractor = GetComponent<XRRayInteractor>() ??
                                     GetComponentInChildren<XRRayInteractor>();

            if (_teleportRayInteractor == null)
            {
                Debug.LogWarning(
                    $"[{nameof(TeleportTeamFilter)}] XRRayInteractor не найден " +
                    $"на {name} или в дочерних объектах. Телепорт не будет фильтроваться.",
                    this);
            }
        }
    }

    private void OnEnable()
    {
        if (MatchManager.HasInstance)
        {
            MatchManager.Instance.MatchStateChanged += OnMatchStateChanged;
        }

        // На момент включения компонента сразу привести маску к актуальному состоянию.
        ApplyMaskForCurrentState();
    }

    private void OnDisable()
    {
        if (MatchManager.HasInstance)
        {
            MatchManager.Instance.MatchStateChanged -= OnMatchStateChanged;
        }
    }

    private void OnMatchStateChanged(MatchManager.MatchState oldState, MatchManager.MatchState newState)
    {
        ApplyMaskForState(newState);
    }

    /// <summary>
    /// Применить маску слоёв для текущего состояния матча.
    /// </summary>
    private void ApplyMaskForCurrentState()
    {
        var state = MatchManager.HasInstance
            ? MatchManager.Instance.CurrentState
            : MatchManager.MatchState.Idle;

        ApplyMaskForState(state);
    }

    /// <summary>
    /// Основная логика выбора маски по фазе и команде.
    /// </summary>
    private void ApplyMaskForState(MatchManager.MatchState state)
    {
        if (_teleportRayInteractor == null)
            return;

        if (_playerTeam == null)
        {
            Debug.LogWarning(
                $"[{nameof(TeleportTeamFilter)}] PlayerTeam не задан, " +
                $"отключаю телепорт для {name}.", this);

            _teleportRayInteractor.interactionLayers = 0; // пустая маска
            return;
        }

        // Определяем, какие слои считаем "домашними" и "чужими" для этой команды.
        InteractionLayerMask homeIslandMask;
        InteractionLayerMask enemyIslandMask;

        switch (_playerTeam.CurrentTeam)
        {
            case PlayerTeam.Team.TeamA:
                homeIslandMask = _teamAIslandLayers;
                enemyIslandMask = _teamBIslandLayers;
                break;

            case PlayerTeam.Team.TeamB:
                homeIslandMask = _teamBIslandLayers;
                enemyIslandMask = _teamAIslandLayers;
                break;

            case PlayerTeam.Team.None:
            default:
                _teleportRayInteractor.interactionLayers = 0;
                Debug.Log(
                    $"[{nameof(TeleportTeamFilter)}] Команда None для {name}, " +
                    $"телепорт отключён.", this);
                return;
        }

        InteractionLayerMask targetMask;

        // Логика по фазам:
        // - Preparation / Combat / RoundEnd / Idle / MatchOver: можно телепортироваться только по своему острову.
        // - Traps: игрок физически находится на вражеском острове, телепорт только по чужому острову.
        switch (state)
        {
            case MatchManager.MatchState.Traps:
                targetMask = enemyIslandMask;
                break;

            case MatchManager.MatchState.Preparation:
            case MatchManager.MatchState.Combat:
            case MatchManager.MatchState.RoundEnd:
            case MatchManager.MatchState.Idle:
            case MatchManager.MatchState.MatchOver:
            default:
                targetMask = homeIslandMask;
                break;
        }

        _teleportRayInteractor.interactionLayers = targetMask;

#if UNITY_EDITOR
        Debug.Log(
            $"[{nameof(TeleportTeamFilter)}] State={state}, Team={_playerTeam.CurrentTeam}, " +
            $"maskValue={_teleportRayInteractor.interactionLayers.value} на объекте {name}",
            this);
#endif
    }

    /// <summary>
    /// Публичный метод для ручного обновления маски (на случай смены команды).
    /// </summary>
    public void RefreshTeamMask()
    {
        ApplyMaskForCurrentState();
    }
}

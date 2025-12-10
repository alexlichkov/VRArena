using UnityEngine;
using Unity.XR.CoreUtils; // для XROrigin

/// <summary>
/// Принудительный "телепорт" игрока по фазам матча.
///
/// Без TeleportationProvider: мы напрямую двигаем XR Origin (и CharacterController),
/// чтобы:
/// - в начале Preparation и Combat оказаться на СВОЁМ респавне;
/// - в начале Traps оказаться на ОСТРОВЕ ПРОТИВНИКА в точке расстановки ловушек.
/// </summary>
[RequireComponent(typeof(PlayerTeam))]
[RequireComponent(typeof(XROrigin))]
public sealed class PhaseTeleportMover : MonoBehaviour
{
    private PlayerTeam _playerTeam;
    private XROrigin _xrOrigin;
    private CharacterController _characterController;

    [Header("Spawn points (home islands)")]
    [Tooltip("Точка респавна для игроков TeamA на их острове (Preparation/Combat).")]
    [SerializeField] private Transform _teamAHomeSpawn;

    [Tooltip("Точка респавна для игроков TeamB на их острове (Preparation/Combat).")]
    [SerializeField] private Transform _teamBHomeSpawn;

    [Header("Trap phase spawn points (enemy islands)")]
    [Tooltip("Точка появления TeamA на острове TeamB в фазе Traps.")]
    [SerializeField] private Transform _teamATrapSpawnOnEnemyIsland;

    [Tooltip("Точка появления TeamB на острове TeamA в фазе Traps.")]
    [SerializeField] private Transform _teamBTrapSpawnOnEnemyIsland;

    private void Awake()
    {
        _playerTeam = GetComponent<PlayerTeam>();
        _xrOrigin = GetComponent<XROrigin>();
        _characterController = GetComponent<CharacterController>();

        if (_playerTeam == null)
            Debug.LogError($"[{nameof(PhaseTeleportMover)}] Нет PlayerTeam на {name}.", this);

        if (_xrOrigin == null)
            Debug.LogError($"[{nameof(PhaseTeleportMover)}] Нет XROrigin на {name}.", this);
    }

    private void OnEnable()
    {
        if (MatchManager.HasInstance)
            MatchManager.Instance.MatchStateChanged += OnMatchStateChanged;
    }

    private void OnDisable()
    {
        if (MatchManager.HasInstance)
            MatchManager.Instance.MatchStateChanged -= OnMatchStateChanged;
    }

    private void OnMatchStateChanged(MatchManager.MatchState oldState, MatchManager.MatchState newState)
    {
        Debug.Log(
            $"[{nameof(PhaseTeleportMover)}] OnMatchStateChanged: {oldState} -> {newState} (team={_playerTeam?.CurrentTeam})",
            this);

        switch (newState)
        {
            case MatchManager.MatchState.Preparation:
                TeleportToHomeSpawn();
                break;

            case MatchManager.MatchState.Traps:
                TeleportToEnemyTrapSpawn();
                break;

            case MatchManager.MatchState.Combat:
                // в начале боя тоже возвращаем на свой остров/респавн
                TeleportToHomeSpawn();
                break;

            case MatchManager.MatchState.RoundEnd:
            case MatchManager.MatchState.MatchOver:
                // по желанию можно вернуть на респавн — пока ничего не делаем
                break;
        }
    }

    private void TeleportToHomeSpawn()
    {
        if (!CheckBase())
            return;

        Transform target = null;

        switch (_playerTeam.CurrentTeam)
        {
            case PlayerTeam.Team.TeamA:
                target = _teamAHomeSpawn;
                break;

            case PlayerTeam.Team.TeamB:
                target = _teamBHomeSpawn;
                break;

            case PlayerTeam.Team.None:
            default:
                Debug.LogWarning($"[{nameof(PhaseTeleportMover)}] Команда None — домашний спавн неизвестен.", this);
                return;
        }

        Debug.Log(
            $"[{nameof(PhaseTeleportMover)}] TeleportToHomeSpawn team={_playerTeam.CurrentTeam}, " +
            $"target={(target != null ? target.name : "NULL")}",
            this);

        TeleportRigTo(target);
    }

    private void TeleportToEnemyTrapSpawn()
    {
        if (!CheckBase())
            return;

        Transform target = null;

        switch (_playerTeam.CurrentTeam)
        {
            case PlayerTeam.Team.TeamA:
                target = _teamATrapSpawnOnEnemyIsland;
                break;

            case PlayerTeam.Team.TeamB:
                target = _teamBTrapSpawnOnEnemyIsland;
                break;

            case PlayerTeam.Team.None:
            default:
                Debug.LogWarning($"[{nameof(PhaseTeleportMover)}] Команда None — точка Traps неизвестна.", this);
                return;
        }

        Debug.Log(
            $"[{nameof(PhaseTeleportMover)}] TeleportToEnemyTrapSpawn team={_playerTeam.CurrentTeam}, " +
            $"target={(target != null ? target.name : "NULL")}",
            this);

        TeleportRigTo(target);
    }

    /// <summary>
    /// Фактическое перемещение XR Origin (с отключением CharacterController на время).
    /// </summary>
    private void TeleportRigTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"[{nameof(PhaseTeleportMover)}] Цель телепорта не задана.", this);
            return;
        }

        // временно отключаем CharacterController, чтобы не мешал смещению
        bool ccWasEnabled = false;
        if (_characterController != null)
        {
            ccWasEnabled = _characterController.enabled;
            _characterController.enabled = false;
        }

        // вариант 1: использовать метод XROrigin
        // (он учитывает смещение камеры и высоту)
        _xrOrigin.MoveCameraToWorldLocation(target.position);
        _xrOrigin.transform.rotation = target.rotation;

        // возвращаем CC
        if (_characterController != null)
            _characterController.enabled = ccWasEnabled;

        Debug.Log(
            $"[{nameof(PhaseTeleportMover)}] TeleportRigTo: moved XR Origin to {target.position} ({target.name})",
            this);
    }

    private bool CheckBase()
    {
        if (_playerTeam == null || _xrOrigin == null)
            return false;

        return true;
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Настраивает Interaction Layer Mask Teleport Interactor'а
/// в зависимости от команды игрока.
/// Тем самым игрок может телепортироваться только на свои острова.
/// </summary>
public class TeleportTeamFilter : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private PlayerTeam _playerTeam;

    [SerializeField]
    private XRRayInteractor _teleportRayInteractor;

    [Header("Layer masks per team")]
    [SerializeField]
    private InteractionLayerMask _teamALayers;

    [SerializeField]
    private InteractionLayerMask _teamBLayers;

    private void Awake()
    {
        if (_playerTeam == null)
        {
            _playerTeam = GetComponent<PlayerTeam>();
            if (_playerTeam == null)
            {
                Debug.LogError($"[{nameof(TeleportTeamFilter)}] PlayerTeam не найден на объекте {name}", this);
            }
        }

        if (_teleportRayInteractor == null)
        {
            // Попробуем найти Teleport Interactor в детях.
            _teleportRayInteractor = GetComponentInChildren<XRRayInteractor>();
            
            if (_teleportRayInteractor == null)
            {
                Debug.LogWarning($"[{nameof(TeleportTeamFilter)}] XRRayInteractor не найден на объекте {name} или в дочерних объектах", this);
            }
        }
    }

    private void Start()
    {
        ApplyTeamMask();
    }

    public void ApplyTeamMask()
    {
        if (_teleportRayInteractor == null)
        {
            Debug.LogWarning($"[{nameof(TeleportTeamFilter)}] XRRayInteractor не установлен на {name}", this);
            return;
        }

        if (_playerTeam == null)
        {
            Debug.LogWarning($"[{nameof(TeleportTeamFilter)}] PlayerTeam не установлен на {name}", this);
            return;
        }

        switch (_playerTeam.CurrentTeam)
        {
            case PlayerTeam.Team.TeamA:
                _teleportRayInteractor.interactionLayers = _teamALayers;
                Debug.Log($"[{nameof(TeleportTeamFilter)}] Применена маска TeamA для {name}", this);
                break;
            case PlayerTeam.Team.TeamB:
                _teleportRayInteractor.interactionLayers = _teamBLayers;
                Debug.Log($"[{nameof(TeleportTeamFilter)}] Применена маска TeamB для {name}", this);
                break;
            case PlayerTeam.Team.None:
            default:
                _teleportRayInteractor.interactionLayers = InteractionLayerMask.GetMask();
                Debug.Log($"[{nameof(TeleportTeamFilter)}] Телепорт отключен для {name} (команда не задана)", this);
                break;
        }
    }

    /// <summary>
    /// Принудительно обновить маску (можно вызывать при смене команды)
    /// </summary>
    public void RefreshTeamMask()
    {
        ApplyTeamMask();
    }
}

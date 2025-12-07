using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Простое оружие (бластер), которое стреляет снарядами Projectile при активации.
/// Работает поверх XRGrabInteractable и использует его событие activated
/// (нажатие триггера на контроллере, пока оружие схвачено).
///
/// Дополнительная интеграция:
/// - стреляет только в определённых состояниях матча (по умолчанию — Combat).
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class BlasterGun : MonoBehaviour
{
    [Header("Projectile setup")]
    [SerializeField]
    private Projectile _projectilePrefab;

    [SerializeField]
    private Transform _muzzlePoint;

    [SerializeField, Min(0.1f)]
    private float _projectileSpeed = 20f;

    [Header("Fire rate (seconds between shots)")]
    [SerializeField, Min(0f)]
    private float _fireCooldown = 0.2f;

    [Header("Match integration")]
    [SerializeField]
    private bool _respectMatchState = true;

    [Tooltip("В каких состояниях матча разрешено стрелять.")]
    [SerializeField]
    private MatchManager.MatchState[] _fireAllowedStates =
    {
        MatchManager.MatchState.Combat
    };

    [Header("Feedback")]
    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip _fireClip;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;
    private float _nextFireTime;

    private void Awake()
    {
        _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (_muzzlePoint == null)
        {
            Debug.LogWarning(
                $"[{nameof(BlasterGun)}] Не задан MuzzlePoint на {name}. " +
                "Снаряды будут спавниться из позиции объекта.",
                this);
        }
    }

    private void OnEnable()
    {
        if (_grabInteractable == null)
            return;

        _grabInteractable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (_grabInteractable == null)
            return;

        _grabInteractable.activated.RemoveListener(OnActivated);
    }

    private void OnActivated(ActivateEventArgs args)
    {
        // Проверка по фазе матча: если есть MatchManager и интеграция включена —
        // стреляем только в разрешённых состояниях.
        if (_respectMatchState && MatchManager.HasInstance)
        {
            if (!IsFireAllowedForState(MatchManager.Instance.CurrentState))
                return;
        }

        if (Time.time < _nextFireTime)
            return;

        Fire();
        _nextFireTime = Time.time + _fireCooldown;
    }

    private bool IsFireAllowedForState(MatchManager.MatchState state)
    {
        if (_fireAllowedStates == null || _fireAllowedStates.Length == 0)
            return true;

        foreach (var s in _fireAllowedStates)
        {
            if (s == state)
                return true;
        }

        return false;
    }

    private void Fire()
    {
        if (_projectilePrefab == null)
        {
            Debug.LogWarning($"[{nameof(BlasterGun)}] Не задан Projectile Prefab на {name}.", this);
            return;
        }

        Vector3 spawnPosition = _muzzlePoint != null ? _muzzlePoint.position : transform.position;
        Quaternion spawnRotation = _muzzlePoint != null ? _muzzlePoint.rotation : transform.rotation;

        Projectile projectileInstance = Instantiate(_projectilePrefab, spawnPosition, spawnRotation);

        Vector3 direction = _muzzlePoint != null ? _muzzlePoint.forward : transform.forward;
        projectileInstance.Launch(direction, _projectileSpeed);

        PlayFireSfx();
    }

    private void PlayFireSfx()
    {
        if (_audioSource == null || _fireClip == null)
            return;

        _audioSource.PlayOneShot(_fireClip);
    }
}

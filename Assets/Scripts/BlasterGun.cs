using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Простая VR-пушка, стреляющая projectile-префабом из MuzzlePoint.
/// Стреляет по событию Activate у XRGrabInteractable.
///
/// Поддерживает:
/// - Кулдаун между выстрелами;
/// - Ограничение по состояниям матча (MatchManager);
/// - Простую звуковую отдачу.
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public sealed class BlasterGun : MonoBehaviour
{
    [Header("Projectile setup")]
    [Tooltip("Префаб снаряда, который будет создаваться при выстреле.")]
    [SerializeField] private Projectile projectilePrefab;

    [Tooltip("Точка, из которой вылетает снаряд.")]
    [SerializeField] private Transform muzzlePoint;

    [Tooltip("Начальная скорость снаряда вперёд от MuzzlePoint.")]
    [SerializeField] private float projectileSpeed = 20f;

    [Header("Fire rate (seconds between shots)")]
    [Tooltip("Минимальное время между выстрелами.")]
    [SerializeField] private float fireCooldown = 0.2f;

    private float _lastFireTime = -999f;

    [Header("Match integration")]
    [Tooltip("Если включено, стрельба будет разрешена только в определённых состояниях матча.")]
    [SerializeField] private bool respectMatchState = true;

    [Tooltip("Список состояний матча, в которых разрешена стрельба.")]
    [SerializeField] private MatchManager.MatchState[] fireAllowedStates =
    {
        MatchManager.MatchState.Combat
    };

    [Header("Feedback")]
    [Tooltip("Источник звука для выстрела (опционально).")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Клип, который проигрывается при выстреле (опционально).")]
    [SerializeField] private AudioClip fireClip;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;

    private void Awake()
    {
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (_grab == null)
        {
            Debug.LogError(
                $"[{nameof(BlasterGun)}] На объекте {name} нет XRGrabInteractable. " +
                $"Скрипт не сможет получать события Activate.",
                this);
        }

        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                $"[{nameof(BlasterGun)}] Projectile Prefab не задан на {name}. " +
                $"Стрелять будет нечем.",
                this);
        }

        if (muzzlePoint == null)
        {
            Debug.LogWarning(
                $"[{nameof(BlasterGun)}] MuzzlePoint не задан на {name}. " +
                $"Снаряды будут появляться в (0,0,0).",
                this);
        }
    }

    private void OnEnable()
    {
        if (_grab != null)
        {
            _grab.activated.AddListener(OnActivated);
        }
    }

    private void OnDisable()
    {
        if (_grab != null)
        {
            _grab.activated.RemoveListener(OnActivated);
        }
    }

    /// <summary>
    /// Обработчик события Activate у XRGrabInteractable.
    /// </summary>
    private void OnActivated(ActivateEventArgs args)
    {
        TryFire();
    }

    /// <summary>
    /// Проверяем все условия и, если можно, делаем выстрел.
    /// </summary>
    private void TryFire()
    {
        // 1. Кулдаун
        if (Time.time < _lastFireTime + fireCooldown)
            return;

        // 2. Проверка состояния матча
        if (respectMatchState && !IsMatchStateAllowed())
            return;

        // 3. Проверка наличия префаба
        if (projectilePrefab == null)
        {
            Debug.LogWarning(
                $"[{nameof(BlasterGun)}] Нельзя выстрелить: projectilePrefab не задан.",
                this);
            return;
        }

        // Все проверки пройдены — стреляем
        _lastFireTime = Time.time;

        FireProjectile();
    }

    /// <summary>
    /// Стреляет снарядом из MuzzlePoint.
    /// </summary>
    private void FireProjectile()
    {
        var spawnPos = muzzlePoint != null ? muzzlePoint.position : transform.position;
        var spawnRot = muzzlePoint != null ? muzzlePoint.rotation : transform.rotation;

        var projectileInstance = Instantiate(projectilePrefab, spawnPos, spawnRot);

        // Задаём начальную скорость через публичный метод projectile
        var initialVelocity = projectileInstance.transform.forward * projectileSpeed;
        projectileInstance.SetInitialVelocity(initialVelocity);

        Debug.Log($"[{nameof(BlasterGun)}] Fire from {name} at {Time.time}", this);

        if (audioSource != null && fireClip != null)
        {
            audioSource.PlayOneShot(fireClip);
        }
    }

    /// <summary>
    /// Проверяет, разрешено ли стрелять в текущем состоянии матча.
    /// </summary>
    private bool IsMatchStateAllowed()
    {
        if (!MatchManager.HasInstance)
            return true; // если матч-менеджера нет, не блокируем выстрелы

        var current = MatchManager.Instance.CurrentState;

        if (fireAllowedStates == null || fireAllowedStates.Length == 0)
            return true;

        for (int i = 0; i < fireAllowedStates.Length; i++)
        {
            if (fireAllowedStates[i] == current)
                return true;
        }

        return false;
    }
}

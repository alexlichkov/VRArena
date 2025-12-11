using UnityEngine;

/// <summary>
/// Простая мина:
/// - пока не вооружена — игнорирует коллизии;
/// - в фазе Combat (через TrapBase) становится вооружённой;
/// - при входе чего-либо с IDamageable наносит урон и (по умолчанию) уничтожает себя.
/// </summary>
[RequireComponent(typeof(Collider))]
public sealed class MineTrap : TrapBase
{
    [Header("Damage")]
    [SerializeField, Min(1f)]
    private float damage = 25f;

    [Header("One-shot settings")]
    [Tooltip("Если true — мина уничтожается после срабатывания. Если false — просто разоружается.")]
    [SerializeField]
    private bool destroyAfterTrigger = true;

    [Header("Visuals (optional)")]
    [SerializeField]
    private GameObject armedVisual;

    [SerializeField]
    private GameObject disarmedVisual;

    private Collider _trigger;

    protected override void OnEnable()
    {
        base.OnEnable();

        _trigger = GetComponent<Collider>();
        if (_trigger == null)
        {
            Debug.LogError($"[{nameof(MineTrap)}] Нет Collider на {name}.", this);
        }
        else
        {
            // Мина работает как триггер
            _trigger.isTrigger = true;
        }

        UpdateVisuals();
    }

    protected override void Arm()
    {
        base.Arm();
        UpdateVisuals();
    }

    protected override void Disarm()
    {
        base.Disarm();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (armedVisual != null)
            armedVisual.SetActive(isArmed);

        if (disarmedVisual != null)
            disarmedVisual.SetActive(!isArmed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isArmed)
            return;

        // MVP-логика: дамажим всё, что умеет IDamageable.
        // Позже сюда добавим фильтр по команде (чтобы не взрывать своих).
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            var hitPoint = transform.position;
            var hitNormal = Vector3.up;

            damageable.ApplyDamage(damage, hitPoint, hitNormal);
        }

        if (destroyAfterTrigger)
        {
            Destroy(gameObject);
        }
        else
        {
            Disarm();
        }
    }
}

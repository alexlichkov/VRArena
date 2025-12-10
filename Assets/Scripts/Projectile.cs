using UnityEngine;

/// <summary>
/// Базовый снаряд:
/// - летит по физике (Rigidbody);
/// - при первом столкновении ищет IDamageable на цели и наносит урон;
/// - уничтожает себя после попадания или по таймеру.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class Projectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Min(0.1f)]
    private float damage = 10f;

    [Header("Lifetime")]
    [SerializeField, Min(0.1f)]
    private float lifeTime = 5f;

    [Header("Debug")]
    [SerializeField]
    private bool logHits = false;

    private Rigidbody _rb;
    private bool _hasHit;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogError($"[{nameof(Projectile)}] Нет Rigidbody на {name}.", this);
        }

        // ВАЖНО: для OnCollisionEnter коллайдер должен быть НЕ IsTrigger.
        var col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[{nameof(Projectile)}] Нет Collider на {name}.", this);
        }
    }

    private void OnEnable()
    {
        // Самоуничтожение по таймеру, если никуда не попал
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    /// <summary>
    /// Unity-коллизия (НЕ триггер). Срабатывает, когда снаряд ударяется о что-то.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (_hasHit)
            return; // чтобы не дамажить несколько раз по рикошетам

        _hasHit = true;

        var other = collision.collider;
        var contact = collision.GetContact(0);

        if (logHits)
        {
            Debug.Log(
                $"[{nameof(Projectile)}] Hit {other.name} at {contact.point}, " +
                $"normal={contact.normal}, damage={damage}",
                this);
        }

        // Ищем IDamageable на цели или её родителях
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(damage, contact.point, contact.normal);
        }

        // После попадания уничтожаем снаряд
        Destroy(gameObject);
    }

    /// <summary>
    /// Позволяет внешнему коду задать начальную скорость.
    /// Удобно вызывать из оружия.
    /// </summary>
    public void SetInitialVelocity(Vector3 velocity)
    {
        if (_rb != null)
        {
            _rb.linearVelocity = velocity;
        }
    }
}

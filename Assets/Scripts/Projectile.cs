using UnityEngine;

/// <summary>
/// Базовый снаряд для оружия.
/// 
/// Ответственность:
/// - задать начальную скорость (Launch);
/// - нанести урон цели, если у неё есть IDamageable;
/// - удалить себя через заданное время жизни или при попадании.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Min(0f)]
    private float _damage = 10f;

    [Header("Lifetime")]
    [SerializeField, Min(0.1f)]
    private float _maxLifetime = 5f;

    private Rigidbody _rigidbody;
    private bool _launched;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError($"[{nameof(Projectile)}] Rigidbody не найден на {name}.", this);
        }
    }

    private void OnEnable()
    {
        // Подстраховка: уничтожим снаряд, если что-то пошло не так.
        // Для пуллинга можно будет заменить на "деактивировать", а не Destroy.
        Invoke(nameof(SelfDestruct), _maxLifetime);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(SelfDestruct));
    }

    /// <summary>
    /// Запуск снаряда в заданном направлении и с указанной скоростью.
    /// Обычно вызывается оружием сразу после Instantiate.
    /// </summary>
    public void Launch(Vector3 direction, float speed)
    {
        if (_rigidbody == null)
            return;

        _launched = true;
        _rigidbody.linearVelocity = direction.normalized * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_launched)
            return;

        // Пытаемся найти IDamageable на цели (на самом объекте или его родителе).
        var damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            var contact = collision.GetContact(0);
            damageable.ApplyDamage(_damage, contact.point, contact.normal);
        }

        // В простом прототипе после первого же попадания уничтожаем снаряд.
        SelfDestruct();
    }

    private void SelfDestruct()
    {
        // Здесь можно будет заменить на Object Pool (деактивация),
        // когда начнёшь оптимизировать.
        Destroy(gameObject);
    }
}

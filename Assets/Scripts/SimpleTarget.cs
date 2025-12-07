using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Простейшая "мишень" на арене.
/// Реализует IDamageable, чтобы реагировать на попадания Projectile и другого оружия.
///
/// Возможности:
/// - хранит здоровье (HP);
/// - даёт очки при уничтожении;
/// - стреляет ивентами OnHit и OnDestroyed (можно повесить звук, анимацию, начисление очков).
/// </summary>
public class SimpleTarget : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField, Min(1)]
    private float _maxHealth = 50f;

    [SerializeField]
    private bool _destroyOnDeath = true;

    [Header("Scoring")]
    [SerializeField]
    private int _scoreValue = 10;

    [Header("Events")]
    public UnityEvent<float> OnHit;          // передаём оставшееся HP
    public UnityEvent<int> OnDestroyed;      // передаём score

    private float _currentHealth;
    private bool _isDead;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    /// <summary>
    /// Реализация IDamageable.
    /// Вызывается Projectile'ами или другим оружием при попадании.
    /// </summary>
    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (_isDead)
            return;

        if (amount <= 0f)
            return;

        _currentHealth -= amount;
        _currentHealth = Mathf.Max(_currentHealth, 0f);

        // Можно здесь сделать спавн партиклов по hitPoint/hitNormal,
        // но пока ограничимся событиями.
        OnHit?.Invoke(_currentHealth);

        if (_currentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        if (_isDead)
            return;

        _isDead = true;
        OnDestroyed?.Invoke(_scoreValue);

        if (_destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            // Альтернатива уничтожению — выключить визуал и коллайдеры.
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }

            var colliders = GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                c.enabled = false;
            }
        }
    }
}

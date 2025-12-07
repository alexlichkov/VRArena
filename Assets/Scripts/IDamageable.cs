using UnityEngine;

/// <summary>
/// Минимальный интерфейс "может получать урон".
/// Через него Projectile / оружие могут общаться с целями, не зная их конкретный класс.
/// Это классический паттерн "программировать через контракт, а не через конкретный тип".
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Применить урон к объекту.
    /// </summary>
    /// <param name="amount">Величина урона.</param>
    /// <param name="hitPoint">Точка попадания (в мировых координатах).</param>
    /// <param name="hitNormal">Нормаль поверхности в точке попадания.</param>
    void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
}

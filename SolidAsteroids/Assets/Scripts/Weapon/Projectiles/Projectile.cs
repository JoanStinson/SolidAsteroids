using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Projectile : MonoBehaviour, IProjectile, IMovingEntity
{
    public abstract GameObject SpawnParticlesPrefab { get; set; }
    public abstract GameObject DeathParticlesPrefab { get; set; }
    public abstract int Damage { get; set; }
    public abstract float MoveSpeed { get; set; }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<LivingEntity>(out var livingEntity))
        {
            SpawnDeathParticles();
            livingEntity.TakeDamage(Damage);
        }
    }

    public virtual void Launch(Transform mountPoint)
    {
        Instantiate(SpawnParticlesPrefab, transform.position, Quaternion.identity);
    }

    public virtual void SpawnDeathParticles()
    {
        Instantiate(DeathParticlesPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
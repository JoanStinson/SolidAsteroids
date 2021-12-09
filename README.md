# Solid Asteroids
A mini space game made applying all SOLID Principles to serve as a reference of good code architecture.
* **🧊 Single Responsibility Principle**
    * A class should have only one responsibility.
* **🚪 Open-Closed Principle**
    * A software module should be open for extension but closed for modification.
* **🦆 Liskov Substitution Principle**
    * Derived classes must be substitutable for their base classes.
* **🤼 Interface Segregation Principle**
    * Clients should not be forced to depend upon the interfaces that they do not use.
* **↕️ Dependency Inversion Principle**
    * Program to an interface, not to an implementation.

## 🧊 Single Responsibility Principle
A class should have only one responsibility.

### ❌ Wrong Way
```csharp
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Player : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 25f;
    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private bool _isInvulnerable;
    [SerializeField] private Sprite _idleSprite;
    [SerializeField] private Sprite _movingUpSprite;
    [SerializeField] private Sprite _movingDownSprite;
    [SerializeField] private Transform _projectileSpawnPoint;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private GameObject _deathParticlesPrefab;

    private SpriteRenderer _spriteRenderer;
    private Vector3 _initialPosition;
    private const float _timeToRespawn = 2f;
    private int _health;

    private void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _initialPosition = transform.position;
        _health = _maxHealth;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Submit"))
        {
            ShootProjectile();
        }

        var vertical = Input.GetAxis("Vertical");
        transform.position += Vector3.up * vertical * _moveSpeed * Time.deltaTime;
        if (vertical == 0)
        {
            _spriteRenderer.sprite = _idleSprite;
        }
        else
        {
            _spriteRenderer.sprite = vertical > 0 ? _movingUpSprite : _movingDownSprite;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int damageAmount = 1;
        if (collision.collider.TryGetComponent<Asteroid>(out var asteroid))
        {
            TakeDamage(damageAmount);
        }
        else if (collision.collider.TryGetComponent<Enemy>(out var enemy))
        {
            TakeDamage(damageAmount * 5);
        }
        else if (collision.collider.TryGetComponent<Npc>(out var npc))
        {
            TakeDamage(0);
        }
    }

    private void ShootProjectile()
    {
        var spawnedProjectile = Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
        spawnedProjectile.transform.position = transform.position;
    }

    private void TakeDamage(int damage)
    {
        if (!_isInvulnerable)
        {
            _health -= damage;
            if (_health <= 0)
            {
                StartCoroutine(Respawn());
            }
        }
    }

    private IEnumerator Respawn()
    {
        _isInvulnerable = true;
        _spriteRenderer.enabled = false;
        Instantiate(_deathParticlesPrefab, transform.position, Quaternion.identity);
        yield return new WaitForSeconds(_timeToRespawn);
        transform.position = _initialPosition;
        _spriteRenderer.enabled = true;
        _isInvulnerable = false;
    }
```
### ✔️ Right Way
```csharp
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerDrawer : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 25f;
    [SerializeField] private Sprite _idleSprite;
    [SerializeField] private Sprite _movingUpSprite;
    [SerializeField] private Sprite _movingDownSprite;

    private PlayerInput _playerInput;
    private SpriteRenderer _spriteRenderer;
    private Vector3 _initialPosition;
    private const float _timeToMakePlayerVisibleAgain = 2f;

    private void Awake()
    {
        GetComponent<PlayerHealth>().OnPlayerRespawn += RespawnPlayer;
        _playerInput = GetComponent<PlayerInput>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _initialPosition = transform.position;
    }

    private void Update()
    {
        transform.position += Vector3.up * _playerInput.Vertical * _moveSpeed * Time.deltaTime;

        if (_playerInput.Vertical == 0)
        {
            _spriteRenderer.sprite = _idleSprite;
        }
        else
        {
            _spriteRenderer.sprite = _playerInput.Vertical > 0 ? _movingUpSprite : _movingDownSprite;
        }
    }

    private void RespawnPlayer()
    {
        StartCoroutine(Respawn(_timeToMakePlayerVisibleAgain));
    }

    private IEnumerator Respawn(float delayInSeconds)
    {
        _spriteRenderer.enabled = false;
        yield return new WaitForSeconds(delayInSeconds);
        transform.position = _initialPosition;
        _spriteRenderer.enabled = true;
    }
}
```
```csharp
public class PlayerInput : MonoBehaviour
{
    public float Vertical { get; private set; }
    public bool ShootProjectile { get; private set; }

    public event Action OnShootProjectile = delegate { };

    private void Update()
    {
        Vertical = Input.GetAxis("Vertical");
        ShootProjectile = Input.GetButtonDown("Submit");
        if (ShootProjectile)
        {
            OnShootProjectile();
        }
    }
}
```
```csharp
[RequireComponent(typeof(Collider2D))]
public class PlayerHealth : MonoBehaviour
{
    public event Action OnPlayerRespawn = delegate { };

    [SerializeField] private int _maxHealth = 100;
    [SerializeField] private bool _isInvulnerable;

    private const float _delayToDisableInvulnerability = 3f;
    private int _health;

    private void Awake()
    {
        _health = _maxHealth;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int damageAmount = 1;
        if (collision.collider.TryGetComponent<Asteroid>(out var asteroid))
        {
            TakeDamage(damageAmount);
        }
        else if (collision.collider.TryGetComponent<Enemy>(out var enemy))
        {
            TakeDamage(damageAmount * 5);
        }
        else if (collision.collider.TryGetComponent<Npc>(out var npc))
        {
            TakeDamage(0);
        }
    }

    private void TakeDamage(int damage)
    {
        if (!_isInvulnerable)
        {
            _health -= damage;
            if (_health <= 0)
            {
                RespawnPlayer();
            }
        }
    }

    private void RespawnPlayer()
    {
        _isInvulnerable = true;
        OnPlayerRespawn();
        StartCoroutine(DisableInvulnerability(_delayToDisableInvulnerability));
    }

    private IEnumerator DisableInvulnerability(float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);
        _isInvulnerable = false;
    }
}
```
```csharp
[RequireComponent(typeof(PlayerHealth))]
public class PlayerParticles : MonoBehaviour
{
    [SerializeField] 
    private GameObject _deathParticlesPrefab;

    private void Awake()
    {
        GetComponent<PlayerHealth>().OnPlayerRespawn += SpawnDeathParticles;
    }

    private void SpawnDeathParticles()
    {
        Instantiate(_deathParticlesPrefab, transform.position, Quaternion.identity);
    }
}
```
```csharp
[RequireComponent(typeof(PlayerInput))]
public class ProjectileLauncher : MonoBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _projectileSpawnPoint;

    private void Awake()
    {
        GetComponent<PlayerInput>().OnShootProjectile += SpawnProjectile;
    }

    private void SpawnProjectile()
    {
        var spawnedProjectile = Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
        spawnedProjectile.transform.position = transform.position;
    }
}
```

## 🚪 Open-Closed Principle
A software module (it can be a class or method) should be open for extension but closed for modification.

### ❌ Wrong Way
```csharp
[RequireComponent(typeof(PlayerInput))]
public class Weapon : MonoBehaviour
{
    [SerializeField] private float _fireWeaponRefreshRate = 1f;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private GameObject _missilePrefab;
    [SerializeField] private Transform _projectileSpawnPoint;

    private float _nextFireTime;

    private void Awake()
    {
        GetComponent<PlayerInput>().OnFireWeapon += FireWeapon;
    }

    private void FireWeapon()
    {
        if (!CanFire())
        {
            return;
        }

        _nextFireTime = Time.time + _fireWeaponRefreshRate;

        if (_bulletPrefab != null)
        {
            var spawnedBullet = Instantiate(_bulletPrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
            spawnedBullet.transform.position = transform.position;
        }
        else if (_missilePrefab != null)
        {
            var spawnedMissile = Instantiate(_missilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
            spawnedMissile.transform.position = transform.position;
        }
        // the list goes on...
    }

    private bool CanFire()
    {
        return Time.time >= _nextFireTime;
    }
}
```
### ✔️ Right Way
```csharp
[RequireComponent(typeof(ILauncher))]
[RequireComponent(typeof(PlayerInput))]
public class Weapon : MonoBehaviour
{
    public Transform WeaponMountPoint => _weaponMountPoint;

    [SerializeField] private float _fireWeaponRefreshRate = 0.25f;
    [SerializeField] private Transform _weaponMountPoint;

    private ILauncher _launcher;
    private float _nextFireTime;

    private void Awake()
    {
        _launcher = GetComponent<ILauncher>();
        GetComponent<PlayerInput>().OnFireWeapon += FireWeapon;
    }

    private void FireWeapon()
    {
        if (!CanFire())
        {
            return;
        }

        _nextFireTime = Time.time + _fireWeaponRefreshRate;
        _launcher.Launch(this);
    }

    private bool CanFire()
    {
        return Time.time >= _nextFireTime;
    }
}
```
```csharp
public interface ILauncher
{
    void Launch(Weapon weapon);
}
```
```csharp
public class BulletLauncher : MonoBehaviour, ILauncher
{
    [SerializeField] 
    private Bullet _bulletPrefab;

    public void Launch(Weapon weapon)
    {
        var spawnedBullet = Instantiate(_bulletPrefab);
        spawnedBullet.Launch(weapon.WeaponMountPoint);
    }
}
```
```csharp
public class MissileLauncher : MonoBehaviour, ILauncher
{
    [SerializeField] private Missile _missilePrefab;
    [SerializeField] private float _missileSelfDestructTimer = 5f;

    public void Launch(Weapon weapon)
    {
        var target = FindObjectOfType<Asteroid>();
        var spawnedMissile = Instantiate(_missilePrefab);
        spawnedMissile.SetTarget(weapon.WeaponMountPoint, target.transform);
        StartCoroutine(spawnedMissile.SelfDestructAfterDelay(_missileSelfDestructTimer));
    }
}
```

## 🦆 Liskov Substitution Principle
Derived classes must be substitutable for their base classes.

### ❌ Wrong Way
```csharp
[RequireComponent(typeof(Collider2D))]
public class PlayerHealth : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        int damageAmount = 1;
        if (collision.collider.TryGetComponent<Asteroid>(out var asteroid))
        {
            TakeDamage(damageAmount);
        }
        else if (collision.collider.TryGetComponent<Enemy>(out var enemy))
        {
            TakeDamage(damageAmount * 5);
        }
        // the list goes on...
    }
}
```
### ✔️ Right Way
```csharp
[RequireComponent(typeof(Collider2D))]
public class PlayerHealth : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.TryGetComponent<LivingEntity>(out var livingEntity))
        {
            TakeDamage(livingEntity.Damage);
        }
    }
}
```
```csharp
public abstract class LivingEntity : MonoBehaviour
{
    public abstract int Damage { get; }

    [SerializeField]
    protected int _maxHealth = 100;

    protected int _health;

    private void Awake()
    {
        _health = _maxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        _health -= damage;
    }
}
```
```csharp
public class Asteroid : LivingEntity
{
    public override int Damage => 200;

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        if (_health <= 0)
        {
            var spawnedAsteroidPiece = Instantiate(_asteroidPiecePrefab);
            spawnedAsteroidPiece.transform.position = Transform.position;
            Destroy(gameObject);
        }
    }
}
```
```csharp
public class Enemy : LivingEntity
{
    public override int Damage => 100;

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);
        if (_health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
```
## 🤼 Interface Segregation Principle
Clients should not be forced to depend upon the interfaces that they do not use.

### ❌ Wrong Way
```csharp
public interface IEntity
{
    GameObject DeathParticlesPrefab { get; }
    Sprite IdleSprite { get; }
    Sprite MovingUpSprite { get; }
    Sprite MovingDownSprite { get; }
    float MoveSpeed { get; }
    int Health { get; }
    int MaxHealth { get; }
    int Damage { get; }

    void SpawnDeathParticles();
    void TakeDamage(int damage);
    void LaunchWeapon(Weapon weapon);
    void LaunchProjectile(Transform mountPoint);
}
```
```csharp
public class Asteroid : IEntity
{
    // implement all interface members
}
```
```csharp
public class BulletLauncher : IEntity
{
    // implement all interface members
}
```
```csharp
public class EnemyShip : IEntity
{
    // implement all interface members
}
```
```csharp
public class Missile : IEntity
{
    // implement all interface members
}
```
### ✔️ Right Way
```csharp
public interface IMovingEntity
{
    GameObject DeathParticlesPrefab { get; }
    float MoveSpeed { get; }
    int Damage { get; }

    void SpawnDeathParticles();
}
```
```csharp
public interface IAnimatedShip
{
    Sprite IdleSprite { get; }
    Sprite MovingUpSprite { get; }
    Sprite MovingDownSprite { get; }
}
```
```csharp
public interface IHaveHealth
{
    int Health { get; }
    int MaxHealth { get; }

    void TakeDamage(int damage);
}
```
```csharp
public interface ILauncher
{
    void Launch(Weapon weapon);
}
```
```csharp
public interface IProjectile
{
    void Launch(Transform mountPoint);
}
```
```csharp
public class Asteroid : IMovingEntity, IHaveHealth
{
    // implement only needed interfaces
}
```
```csharp
public class BulletLauncher : ILauncher
{
    // implement only needed interfaces
}
```
```csharp
public class EnemyShip : IMovingEntity, IAnimatedShip, IHaveHealth
{
    // implement only needed interfaces
}
```
```csharp
public class Missile : IMovingEntity, IProjectile
{
    // implement only needed interfaces
}
```

## ↕️ Dependency Inversion Principle
Program to an interface, not to an implementation.

### ❌ Wrong Way

### ✔️ Right Way

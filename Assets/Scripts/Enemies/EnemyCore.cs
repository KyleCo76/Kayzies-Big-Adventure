using Kayzie.Player;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]

public class EnemyCore : MonoBehaviour
{

    [FoldoutGroup("A* Pathfinding Settings")]
    [Tooltip("The transform of the player that the enemy will follow. This should be assigned in the Inspector.")]
    [SerializeField] protected Transform playerTransform;

    [FoldoutGroup("Wander Settings")]
    [Tooltip("Should the enemy wander randomly when not following the player?")]
    [SerializeField] protected bool canWander = false;
    [FoldoutGroup("Wander Settings")]
    [ShowIf("canWander")]
    [Tooltip("Range within which the enemy can wander randomly (in units)")]
    [SerializeField] private float wanderRange = 5f; // Range within which the enemy can wander randomly

    // References to components
    protected Rigidbody2D enemyBody;
    protected Animator enemyAnimator;
    protected Collider2D enemyCollider;
    protected SpriteRenderer enemySpriteRender;

    protected bool isFlipped;
    private Vector3 lastKnownPosition;
    private int stuckCounter = 0;
    private const int MaxStuckCount = 3; // Maximum number of frames to consider the enemy stuck
    protected Vector2 wanderTarget; // Target position for wandering
    protected Vector2 wanderTargetLeft;
    protected Vector2 wanderTargetRight;
    protected SpriteRenderer spriteRendererComponent;
    protected bool isFollowing;

    [FoldoutGroup("Sleep Settings")]
    [Tooltip("Should the enemy sleep when off-screen to save performance?")]
    [SerializeField] protected bool canSleep = true;
    [FoldoutGroup("Sleep Settings")]
    [Tooltip("Distance beyond screen bounds before enemy sleeps")]
    [SerializeField] protected float sleepDistance = 5f;

    [ToggleGroup("hasMultiAudios"), SerializeField, Tooltip("Does the enemy have multiple audio sources?")]
    protected bool hasMultiAudios = false;
    [ToggleGroup("hasMultiAudios"), SerializeField, Tooltip("Audio source for the enemy when idle")]
    protected EnemyAudioSettings enemyAudioIdle;
    [ToggleGroup("hasMultiAudios"), SerializeField, Tooltip("Audio source for the enemy when near the player")]
    protected EnemyAudioSettings enemyAudioNearPlayer;
    [ToggleGroup("hasMultiAudios"), SerializeField, Tooltip("Distance at which the audio near player will trigger")]
    protected float enemyAudioNearPlayerDistance = 1f;
    [ToggleGroup("hasMultiAudios"), SerializeField, Tooltip("Audio source for the enemy when damaged")]
    protected EnemyAudioSettings enemyAudioDamaged;
    [ToggleGroup("hasMultiAudios"), SerializeField, Tooltip("Audio source for the enemy when following the player")]
    protected EnemyAudioSettings enemyAudioFollowing;
    [ToggleGroup("hasMultiAudios"), SerializeField, Tooltip("Audio source for the enemy when attacking")]
    protected EnemyAudioSettings enemyAudioAttack;
    [HideIf("hasMultiAudios"), SerializeField, Tooltip("Single audio source for the enemy")]
    protected EnemyAudioSettings enemyAudioSingle;

    protected AudioClip currentAudio;
    protected AudioSource enemyAudioSource;

    protected bool isSleeping = false;
    private Vector2 sleepPosition; // Store position when sleeping
    protected bool isDamaged = false;
    protected float tempFollowDistance = 0f; // Temporary follow distance when damaged

    protected virtual void OnEnable()
    {
        Managers.Instance.SaveGameManager.OnSaveGame += GameSaved; // Subscribe to save game event to update A* grid
        Managers.Instance.SaveGameManager.OnLoadGame += GameLoaded; // Subscribe to load game event to update A* grid
    }

    protected virtual void OnDisable()
    {
        Managers.Instance.SaveGameManager.OnSaveGame -= GameSaved; // Unsubscribe from save game event
        Managers.Instance.SaveGameManager.OnLoadGame -= GameLoaded; // Unsubscribe from load game event
    }

    protected virtual void Awake()
    {
        if (!TryGetComponent<Rigidbody2D>(out enemyBody)) {
            Debug.LogError("Rigidbody2D component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<Animator>(out enemyAnimator)) {
            Debug.LogError("Animator component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<SpriteRenderer>(out spriteRendererComponent)) {
            Debug.LogError("Sprite Renderer component is missing on " + gameObject.name);
        }

        if (canWander) {
            Vector3 wanderStart = transform.position; // Set the starting position for wandering
            wanderTargetLeft = new Vector2(wanderStart.x - wanderRange, wanderStart.y); // Set the left target position
            wanderTargetRight = new Vector2(wanderStart.x + wanderRange, wanderStart.y); // Set the right target position
            // Randomly choose a direction to start wandering and set the initial wander target
            sbyte direction = Random.value < 0.5f ? (sbyte)-1 : (sbyte)1;
            if (direction < 0) {
                wanderTarget = wanderTargetLeft;
                FlipSprite(); // Flip the sprite to face left
            } else {
                wanderTarget = wanderTargetRight;
            }
        }
        if (!TryGetComponent<Collider2D>(out enemyCollider)) {
            Debug.LogError("Collider2D component is missing on " + gameObject.name);
        }
        if (!TryGetComponent<AudioSource>(out enemyAudioSource)) {
            Debug.LogWarning("AudioSource component is missing on " + gameObject.name);
        }
        if (!hasMultiAudios && enemyAudioSingle.clip != null) {
            ChangeAudio(enemyAudioSingle);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (Time.frameCount % 30 == 0 && enemyAudioSource) {
            CheckSleepState(enemyAudioSource);
        }
        if (isSleeping) return;

        StartCoroutine(EndOfFixedUpdate());
    }

    private void FindCorrectAudio()
    {
        if (isDamaged && enemyAudioDamaged != null) {
            ChangeAudio(enemyAudioDamaged);
        } else if (enemyAudioNearPlayer != null && playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < enemyAudioNearPlayerDistance) {
            ChangeAudio(enemyAudioNearPlayer);
        } else if (isFollowing && enemyAudioFollowing != null) {
            ChangeAudio(enemyAudioFollowing);
        } else if (enemyAudioIdle != null) {
            ChangeAudio(enemyAudioIdle);
        }
    }

    protected void ChangeAudio(EnemyAudioSettings newAudio)
    {
        if (enemyAudioSource == null) {
            enemyAudioSource = GetComponent<AudioSource>();
            if (enemyAudioSource == null) {
                Debug.LogError("AudioSource component is missing on " + gameObject.name);
                return;
            }
        }
        if (enemyAudioSource.clip != newAudio.clip) {
            currentAudio = newAudio.clip;
            enemyAudioSource.Stop(); // Stop the current audio if it's different
            enemyAudioSource.clip = newAudio.clip;
            enemyAudioSource.volume = newAudio.volume;
            enemyAudioSource.pitch = newAudio.pitch;
            enemyAudioSource.Play();
        }
    }

    protected bool CheckStuck()
    {
        if (transform.position != lastKnownPosition) {
            lastKnownPosition = transform.position;
            return false;
        }

        stuckCounter++;
        if (stuckCounter >= MaxStuckCount) {
            stuckCounter = 0;
            return true;
        } else {
            lastKnownPosition = transform.position;
            return false;
        }

    }

    private IEnumerator EndOfFixedUpdate()
    {
        yield return new WaitForEndOfFrame();
        if (CheckStuck()) {
            ResetTargetForObstacle();
        }
        // Handle changing audio if neccessary
        if (hasMultiAudios) {
            FindCorrectAudio();
        }
    }

    private void GameLoaded(SaveGameData[] saveData)
    {
        if (saveData == null || saveData.Length == 0) {
            Debug.LogError("No Save Data Found");
            return;
        }

        foreach (SaveGameData data in saveData) {
            if (data.CharacterName == this.name) {
                if (TryGetComponent<EnemyHealthManager>(out var healthManager)) {
                    healthManager.LoadHealthValues(data.MaxHealth, data.Health);
                }
                
                transform.SetPositionAndRotation(new Vector3(data.PositionX, data.PositionY, data.PositionZ), new Quaternion(data.RotationX, data.RotationY, data.RotationZ, data.RotationW));
                //enemyBody.linearVelocity = Vector2.zero; // Reset velocity to prevent unwanted movement
                Managers.Instance.SaveGameManager.ObjectsManaged++;
                return;
            }
        }
    }

    private void GameSaved(string saveName)
    {
        float maxHealth = 0;
        float health = 0;
        if (TryGetComponent<EnemyHealthManager>(out var healthManager)) {
            maxHealth = healthManager.MaxHealth;
            health = healthManager.CurrentHealth;
        } else {
            Debug.LogWarning("EnemyHealthManager component is missing on " + gameObject.name);
        }
        SaveGameData saveData = new() {
            SaveName = saveName,
            CharacterName = gameObject.name,
            PositionX = transform.position.x,
            PositionY = transform.position.y,
            PositionZ = transform.position.z,
            RotationX = transform.rotation.x,
            RotationY = transform.rotation.y,
            RotationZ = transform.rotation.z,
            RotationW = transform.rotation.w,
            Health = Mathf.RoundToInt(health),
            MaxHealth = Mathf.RoundToInt(maxHealth)
        };
        Managers.Instance.SaveGameManager.SaveObjectData(saveData);
    }

    protected void ResetTargetForObstacle()
    {
        if (isFlipped) {
            wanderTarget = wanderTargetLeft = transform.position;// + new Vector3(0.3f, 0f, 0f);
        } else {
            wanderTarget = wanderTargetRight = transform.position;// - new Vector3(0.3f, 0f, 0f);

        }
    }

    protected void CheckSleepState(AudioSource audioSourceComponent = null, Vector3 swarmPosition = default)
    {
        bool shouldSleep = !Managers.Instance.GameManager.IsWithinGameWindow(transform, sleepDistance);

        if (shouldSleep && !isSleeping) {
            SleepEnemy(audioSourceComponent, swarmPosition); // Fixed: was "transform.gameObject.wa;"
            return;
        } else if (!shouldSleep && isSleeping) {
            WakeEnemy(); // Wake the enemy if within game window
        }
    }

    private void SleepEnemy(AudioSource audioSourceComponent = null, Vector3 swarmPosition = default)
    {
        if (!canSleep || isSleeping) return;

        isSleeping = true;
        sleepPosition = Vector3.Distance(swarmPosition, Vector3.zero) < 0.1f ? transform.position : swarmPosition; // Store current position

        // Disable movement and audio
        enemyBody.linearVelocity = Vector2.zero;
        enemyBody.bodyType = RigidbodyType2D.Kinematic;

        if (audioSourceComponent != null && audioSourceComponent.isPlaying) {
            audioSourceComponent.Stop();
        }

        // Disable animator to save performance
        if (enemyAnimator != null) {
            enemyAnimator.enabled = false;
        }
    }

    /// <summary>
    /// Wakes the enemy when they come back on-screen
    /// </summary>
    private void WakeEnemy()
    {
        if (!isSleeping) return;

        isSleeping = false;

        // Re-enable physics
        enemyBody.bodyType = RigidbodyType2D.Dynamic;

        // Re-enable animator
        if (enemyAnimator != null) {
            enemyAnimator.enabled = true;
        }

        // Restore position (in case of any drift)
        transform.position = sleepPosition;
    }

    protected bool TryFollowPlayer(Transform playerTransform, float followRange)
    {
        // Calculate horizontal distance to player
        float distance = Mathf.Abs(transform.position.x - playerTransform.position.x);
        distance = Mathf.Max(distance, Mathf.Abs(transform.position.y - playerTransform.position.y)); // Use the maximum distance to account for vertical position
        if (distance <= followRange) {
            return true; // Player is within follow range
        }
        return false;
    }

    protected bool TryFollowPlayer(out float followDistance, bool isFollowing, float followActivation, float followBoostMulti, Transform playerTransform, bool isDamaged, float tempFollowDistance)
    {
        followDistance = isFollowing ? followActivation * followBoostMulti : followActivation;
        float dx = Mathf.Abs(transform.position.x - playerTransform.position.x);
        float dy = Mathf.Abs(transform.position.y - playerTransform.position.y);
        float distance = Mathf.Max(dx, dy);
        return distance <= (isDamaged ? tempFollowDistance : followDistance);
    }

    protected virtual void StopFollowingPlayer(out bool isFollowing, ref float tempFollowDistance, ref bool isDamaged, float wanderTarget, float followDistance)
    {
        isFollowing = false;
        float pointDirection = Mathf.Sign(wanderTarget - transform.position.x);
        if (pointDirection < 0 && !isFlipped) {
            FlipSprite(); // Flip the sprite to face left
        } else if (pointDirection > 0 && isFlipped) {
            FlipSprite(); // Flip the sprite to face right
        }
        if (isDamaged) {
            isDamaged = false;
            tempFollowDistance = followDistance;
        }
    }

    /// <summary>
    /// Flips the sprite horizontally by inverting its local scale along the X-axis.
    /// </summary>
    /// <remarks>This method toggles the flipped state of the sprite. If the sprite is currently flipped,  it
    /// will be restored to its original orientation. If the sprite is not flipped, it will  be inverted horizontally.
    /// The flipped state is determined by the <c>isFlipped</c> field.</remarks>
    //public void FlipSprite()
    //{
    //    if (enemyBody.linearVelocityX < 0f) {
    //        Vector3 rotation = new(transform.rotation.x, 180f, transform.rotation.z);
    //        transform.rotation = Quaternion.Euler(rotation); // Flip the sprite by rotating it 180 degrees around the Y-axis
    //        isFlipped = true;
    //    } else if (enemyBody.linearVelocityX > 0f) {
    //        Vector3 rotation = new(transform.rotation.x, 0f, transform.rotation.z);
    //        transform.rotation = Quaternion.Euler(rotation); // Restore the sprite to its original orientation
    //        isFlipped = false;
    //    }
    //}

    protected void FlipSprite()
    {
        if (spriteRendererComponent == null) {
            Debug.LogWarning("SpriteRenderer component is missing on " + gameObject.name);
            return;
        }
        spriteRendererComponent.flipX = !isFlipped; // Toggle the flip state of the sprite
        isFlipped = spriteRendererComponent.flipX; // Update the isFlipped state based on the sprite renderer's flip state
    }

    protected void UpdateAStarGrid()
    {
        Bounds area = enemyCollider.bounds;
        area.Expand(1.0f); // Expand the bounds to include a margin for pathfinding
        AstarPath.active.UpdateGraphs(area); // Update the A* pathfinding graphs
    }

    public void WasDamaged()
    {
        isDamaged = true;
        tempFollowDistance = Vector2.Distance(transform.position, playerTransform.position) + 1f; // Store the distance to the player when damaged with a small buffer
    }
}

[System.Serializable]
public class EnemyAudioSettings
{
    [Range(0, 1)]
    public float volume = 0.5f;

    [Range(0.5f, 2f)]
    public float pitch = 1.0f;

    public AudioClip clip;
}

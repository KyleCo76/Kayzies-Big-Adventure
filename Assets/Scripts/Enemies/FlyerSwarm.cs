using Pathfinding;
using UnityEngine;

public class FlyerSwarm : FlyerFunctions
{

    private Transform[] swarmMembers;
    protected Vector3 startPosition;

    protected virtual void Start()
    {
        // Set all parameters for the swarm members
        swarmMembers = GetComponentsInChildren<Transform>();
        foreach (Transform flyer in swarmMembers) {
            wanderTargetLeft = flyer.position + new Vector3(-wanderDistance, 0, 0);
            wanderTargetRight = flyer.position + new Vector3(wanderDistance, 0, 0);
            wanderTarget = swarmToRight ? wanderTargetRight : wanderTargetLeft;
            FlyerSwarmMember member = flyer.GetComponent<FlyerSwarmMember>();
            AssignSerializedFieldsToMember(member);
        }

        followDistance = followActivation;
    }

    private void AssignSerializedFieldsToMember(FlyerSwarmMember member)
    {
        if (member == null) return;

        member.enemyCollider = member.GetComponent<Collider2D>();
        member.enemyBody = member.GetComponent<Rigidbody2D>();
        member.enemyAnimator = member.GetComponent<Animator>();
        member.enemySpriteRender = member.GetComponent<SpriteRenderer>();
        member.damageAmount = damageAmount;
        member.moveSpeed = moveSpeed;
        member.maxWanderSpeed = maxWanderSpeed;
        member.maxSpeed = maxSpeed;
        member.followActivation = followActivation;
        member.followBoostMulti = followBoostMulti;
        member.hasFollowAnimation = hasFollowAnimation;
        member.groundLayer = groundLayer;
        member.baseRotation = baseRotation;
        member.idleRotation = idleRotation;
        member.canWander = canWander;
        member.wanderDistance = wanderDistance;
        member.hasIdle = hasIdle;
        member.idleUp = idleUp;
        member.swarmToRight = swarmToRight;
        member.playerTransform = playerTransform;
        member.seeker = member.GetComponent<Seeker>();
        member.wanderTarget = wanderTarget;
        member.wanderTargetLeft = wanderTargetLeft;
        member.wanderTargetRight = wanderTargetRight;
        if (member.TryGetComponent<SpriteRenderer>(out var spriteRenderer)) {
            member.spriteRendererComponent = spriteRenderer;
        } else {
            Debug.LogWarning("SpriteRenderer component is missing on " + member.name);
        }
        member.startPosition = member.transform.position;
    }

    protected override void Awake()
    {

    }

    protected override void FixedUpdate()
    {
        swarmMembers = GetComponentsInChildren<Transform>();
        if (swarmMembers == null || swarmMembers.Length == 0) {
            Debug.LogWarning("No swarm members found. Exiting FixedUpdate.");
            return; // Exit if no swarm members are found
        }

        // Check if we are moving to idle position and move all members of the swarm to idle position
        if (movingToIdle) {
            bool stillMoving = false;
            foreach (Transform flyer in swarmMembers) {
                if (flyer.TryGetComponent<FlyerSwarmMember>(out var member)) {
                    stillMoving = stillMoving || member.MoveToIdle(); // Move each member of the swarm to idle position
                }
            }
            if (!stillMoving) {
                movingToIdle = false; // If no member is moving, stop moving to idle
            }
        }

        // Check if any member of the swarm can follow the player
        bool canFollow = false;
        foreach (Transform flyer in swarmMembers) {
            if (flyer.TryGetComponent<FlyerSwarmMember>(out var member) && member.TryFollowPlayer(out followDistance, isFollowing, followActivation, followBoostMulti, playerTransform, isDamaged, tempFollowDistance)) {
                canFollow = true;
                break; // Exit early if any member can follow the player
            }
        }
        // If any member can follow the player, set the swarm to following state and make all members follow otherwise ensure we disable following
        if (canFollow) {
            isFollowing = true;
            isIdle = false;
            foreach (Transform flyer in swarmMembers) {
                if (flyer.TryGetComponent<FlyerSwarmMember>(out var member)) {
                    member.FollowPlayer();
                }
            }
            return; // Exit early after following
        } else if (isFollowing) {
            isFollowing = false; // If not within follow distance, stop following the player
            foreach (Transform flyer in swarmMembers) {
                if (flyer.TryGetComponent<FlyerSwarmMember>(out var member)) {
                    member.StopFollowingPlayer();
                }
            }
        }

        // If not following the player, check if we can wander or idle
        if (canWander) {
            isIdle = false;
            //Wander(true);
            //enemyBody.MovePosition(new(1.0f, 1.0f));
            foreach (Transform flyer in swarmMembers) {
                if (flyer.TryGetComponent<FlyerSwarmMember>(out var member)) {
                    bool flip = member.Wander(true); // Wander if allowed
                    if (flip) {
                        member.FlipSprite(); // Flip the sprite to face the direction of wandering
                    }
                }
            }

        } else if (!isIdle && hasIdle) {
            isIdle = true;
            foreach (Transform flyer in swarmMembers) {
                if (flyer.TryGetComponent<FlyerSwarmMember>(out var member)) {
                    member.Idle(); // If not wandering, check if we can idle and Idle if possible
                }
            }
        }
    }
}

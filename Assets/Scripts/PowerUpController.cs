using Kayzie.Player;
using UnityEngine;

public class PowerUpController : MonoBehaviour
{

    [Header("Jump Power Up Settings")]
    [Tooltip("The amount of jump power to add when the power-up is collected.")]
    [SerializeField] private float jumpPowerMultiplier = 1f;
    
    [Space]
    [Header("Speed Power Up Settings")]
    [Tooltip("The amount of speed to multiply when the power-up is collected.")]
    [SerializeField] private float speedMultiplier = 1f;

    [Space]
    [Header("Health Power Up Settings")]
    [Tooltip("The amount of health to restore when the power-up is collected.")]
    [SerializeField] private float healthRestoreAmount = 0;
    [Tooltip("The amount of maximum health to increase when the power-up is collected.")]
    [SerializeField] private float maxHealthIncrease = 0;

    [Space]
    [Header("Stamina Power Up Settings")]
    [Tooltip("The amount of stamina to restore when the power-up is collected.")]
    [SerializeField] private float staminaRestoreAmount = 0;
    [Tooltip("The multiplier amount of maximum stamina to increase when the power-up is collected.")]
    [SerializeField] private float maxStaminaMultipler = 1.0f;
    [Tooltip("The multiplier amount for stamina regeneration rate.")]
    [SerializeField] private float staminaRegenRateMultiplier = 1.0f;
    [Tooltip("The multiplier amount for stamina consumption rate.")]
    [SerializeField] private float staminaConsumptionRateMultiplier = 1.0f;

    [Space]
    [Header("Player Damage Settings")]
    [Tooltip("The multiplier amount to increase the player's damage output")]
    [SerializeField] private float DamageMultiplier = 1.0f;

    [Space]
    [Header("Power Up Duration Settings")]
    [Tooltip("The duration for which the power-up effects last. Set to 0 for permanent effects.")]
    [SerializeField] private float powerUpDuration = 0.0f;
    [Tooltip("Sets whether the power-up is a reacuring aura or a one-time effect.")]
    [SerializeField] private bool isAura = false;
    [Tooltip("Sets how often the aura applies its effects, if applicable. Set to 0 for no aura effect.")]
    [SerializeField] private float auraTickRate = 0.0f; // How often the aura applies its effects, if applicable

    private float auraTimer = 0.0f; // Timer for aura effects
    private bool canTick = true; // Flag to control aura ticking

/*
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
*/

    // Update is called once per frame
    void Update()
    {
        if (isAura) {
            AuraUpdate();
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerControllerV2 player = collision.GetComponent<PlayerControllerV2>();
            if (player != null)
            {
                ApplyPowerUp(player);
                if (!isAura)
                {
                    Destroy(gameObject); // Destroy the power-up after applying it
                }
            }
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) {
            PlayerControllerV2 player = collision.GetComponent<PlayerControllerV2>();
            if (player != null) {
                ApplyPowerUp(player);
                if (!isAura) {
                    Destroy(gameObject); // Destroy the power-up after applying it
                }
            }
        }
    }


    private void AuraUpdate()
    {
        auraTimer += Time.deltaTime;
        if (auraTimer >= auraTickRate)
        {
            auraTimer = 0.0f; // Reset the timer
            canTick = true; // Allow ticking again
        }
    }

    private void ApplyPowerUp(PlayerControllerV2 player)
    {
        if (isAura && !canTick)
        {
            return; // If it's an aura and not ready to tick, exit
        }

        if (jumpPowerMultiplier > 0)
        {
            player.ChangeJumpForce(jumpPowerMultiplier, powerUpDuration);
        }
        if (speedMultiplier > 0)
        {
            player.ChangeMoveSpeed(speedMultiplier, powerUpDuration);
        }
        if (healthRestoreAmount > 0)
        {
            player.AdjustHealth(healthRestoreAmount);
        }
        if (maxHealthIncrease > 0)
        {
            player.ModifyMaxHealth(maxHealthIncrease, powerUpDuration);
        }
        if (staminaRestoreAmount > 0)
        {
            player.ChangeStamina(staminaRestoreAmount, powerUpDuration);
        }
        if (maxStaminaMultipler > 0)
        {
            player.ChangeMaxStamina(maxStaminaMultipler, powerUpDuration);
        }
        if (staminaRegenRateMultiplier > 0)
        {
            player.ChangeStaminaRegenRate(staminaRegenRateMultiplier, powerUpDuration);
        }
        if (staminaConsumptionRateMultiplier > 0)
        {
            player.ChangeStaminaConsumptionRate(staminaConsumptionRateMultiplier, powerUpDuration);
        }
        if (DamageMultiplier > 0)
        {
            player.ChangeDamage(DamageMultiplier, powerUpDuration);
        }
        canTick = false; // Prevent further ticking until the next aura tick

    }
}

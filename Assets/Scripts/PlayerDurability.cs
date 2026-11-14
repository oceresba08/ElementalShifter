using System;
using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class PlayerDurability : MonoBehaviour
{
    public PlayerElements elements;
    public float maxHealth = 100f;
    public float invulnerabilitySeconds = .8f;
    public float CurrentHealth { get; private set; }
    public event Action<float, float> HealthChanged;
    public event Action PlayerDefeated;
    float nextHurt;
    void Awake() { if (elements == null) elements = GetComponent<PlayerElements>(); CurrentHealth = maxHealth; }
    public void TakeDamage(float damage, Vector3 knockback)
    {
        if (Time.time < nextHurt || CurrentHealth <= 0) return;
        nextHurt = Time.time + invulnerabilitySeconds;
        float defense = elements != null && elements.Current != null ? elements.Current.defenseMultiplier : 1f;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage / Mathf.Max(.1f, defense));
        GetComponent<Rigidbody>().AddForce(knockback, ForceMode.VelocityChange);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (CurrentHealth <= 0) PlayerDefeated?.Invoke();
    }
    public void Heal(float value)
    { CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + value); HealthChanged?.Invoke(CurrentHealth, maxHealth); }
}

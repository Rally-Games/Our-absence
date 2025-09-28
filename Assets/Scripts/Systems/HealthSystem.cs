using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private Slider uiSlider;
    [SerializeField] private GameObject enemyGameObject;
    [SerializeField] private int health = 100;
    [SerializeField] private int maxHealth = 100;
    private Dictionary<string, int> healthModifiers = new Dictionary<string, int>();
    //private Dictionary<string, Item> armorModifiers = new Dictionary<string, Item>();
    private Dictionary<string, int> damageEffects = new Dictionary<string, int>();
    private Dictionary<string, int> healthEffects = new Dictionary<string, int>();

    void Start()
    {
        uiSlider.maxValue = maxHealth;
        uiSlider.value = health;
    }

    private void Update()
    {
        effectTimer += Time.deltaTime;
        if (effectTimer >= 1f)
        {
            ApplyEffects();
            effectTimer = 0f;
        }

        if (health <= 0)
        {
            Death();
        }
    }

    private float effectTimer = 0f;


    public int Health { get { return health; } }
    public int MaxHealth { get { return maxHealth; } }
    public Dictionary<string, int> HealthModifiers { get { return healthModifiers; } }
    public Dictionary<string, int> DamageEffects { get { return damageEffects; } }
    public Dictionary<string, int> HealthEffects { get { return healthEffects; } }

    private void ApplyEffects()
    {
        foreach (var effect in damageEffects)
        {
            TakeDamage(effect.Value);
        }
        foreach (var effect in healthEffects)
        {
            Heal(effect.Value);
        }
        // Optionally update UI after applying effects
        UpdateHealthUI();
    }

    public void TakeDamage(int damage)
    {
        int totalDamage = damage;
        // Apply modifiers // e.g., armor, buffs
        health -= totalDamage;
        if (health < 0) health = 0;

        UpdateHealthUI();
    }

    public void Heal(int amount)
    {
        health += amount;
        if (health > maxHealth) health = maxHealth;

        UpdateHealthUI();
    }

    public void AddHealthModifier(string name, int modifier)
    {
        healthModifiers[name] = modifier;
    }

    public void ClearDamageEffects()
    {
        damageEffects.Clear();
    }

    public void ClearHealthEffects()
    {
        healthEffects.Clear();
    }

    public void AddDamageEffect(string name, int effect)
    {
        damageEffects[name] = effect;
    }

    public void AddHealthEffect(string name, int effect)
    {
        healthEffects[name] = effect;
    }

    public void RemoveHealthModifier(string name)
    {
        if (healthModifiers.ContainsKey(name))
        {
            healthModifiers.Remove(name);
        }
    }

    public void Death()
    {
        foreach (var component in enemyGameObject.GetComponents<MonoBehaviour>())
        {
            component.enabled = false;
        }
        var animator = enemyGameObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
        var collider = enemyGameObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        var characterController = enemyGameObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        enemyGameObject.tag = "Die";
    }

    public void RemoveDamageEffect(string name)
    {
        if (damageEffects.ContainsKey(name))
        {
            damageEffects.Remove(name);
        }
    }
    public void RemoveHealthEffect(string name)
    {
        if (healthEffects.ContainsKey(name))
        {
            healthEffects.Remove(name);
        }
    }

    private void UpdateHealthUI()
    {
        if (uiSlider == null) return;
        uiSlider.value = health;
    }

}

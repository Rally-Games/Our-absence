using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    //[SerializeField] private DeathSystem deathSystem;
    private int health = 100;
    private int maxHealth = 100;
    private Dictionary<string, int> healthModifiers = new Dictionary<string, int>();
    //private Dictionary<string, Item> armorModifiers = new Dictionary<string, Item>();
    private Dictionary<string, int> damageEffects = new Dictionary<string, int>();
    private Dictionary<string, int> healthEffects = new Dictionary<string, int>();

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }
        effectTimer += Time.deltaTime;
        if (effectTimer >= 1f)
        {
            ApplyEffects();
            effectTimer = 0f;
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
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        var healthLabel = root.Q<Label>("HealthLabel");
        if (healthLabel != null)
        {
            healthLabel.text = $"Health: {health}/{maxHealth}";
        }
    }

}

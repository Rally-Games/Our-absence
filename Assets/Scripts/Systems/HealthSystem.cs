using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public Player_controller Player_controller;
    public UIDocument UIDoc;

    private Label m_HealthLabel;
    private VisualElement m_HealthBarMask;

    [SerializeField] private UnityEngine.UI.Slider uiSlider;
    [SerializeField] private int health = 100;
    [SerializeField] private int maxHealth = 100;
    private Dictionary<string, int> healthModifiers = new Dictionary<string, int>();
    //private Dictionary<string, Item> armorModifiers = new Dictionary<string, Item>();
    private Dictionary<string, int> damageEffects = new Dictionary<string, int>();
    private Dictionary<string, int> healthEffects = new Dictionary<string, int>();

    void Start()
    {
        if (uiSlider)
        {
            uiSlider.maxValue = maxHealth;
            uiSlider.value = health;
        }

        Player_controller.OnHealthChange += UpdateHealthUI;
        m_HealthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");
        m_HealthBarMask = UIDoc.rootVisualElement.Q<VisualElement>("HealthBarMask");

        UpdateHealthUI();
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
        foreach (var component in gameObject.GetComponents<MonoBehaviour>())
        {
            component.enabled = false;
        }
        var animator = gameObject.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
        }
        var collider = gameObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        var characterController = gameObject.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }
        gameObject.tag = "Die";
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
        m_HealthLabel.text = $"{Player_controller.CurrentHealth}/{Player_controller.MaxHealth}";

        float healthRatio = (float)Player_controller.CurrentHealth / Player_controller.MaxHealth;
        float healthPrecent = Mathf.Lerp(1, 99, healthRatio);
        m_HealthBarMask.style.width = Length.Percent(healthPrecent);
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponDamageSystem : MonoBehaviour
{
    [SerializeField] private ItemDataInstance WeaponItem;

    float damage;

    // Start is called before the first frame update
    void Start()
    {
        damage = WeaponItem.damageModifier;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            HealthSystem enemyHealth = other.GetComponent<HealthSystem>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(Mathf.RoundToInt(damage));
            }
        }
    }
}

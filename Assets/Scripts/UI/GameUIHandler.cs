using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GameUIHandler : MonoBehaviour
{
    public Player_controller Player_controller;
    public UIDocument UIDoc;

    private Label m_HealthLabel;

    // Start is called before the first frame update
    private void Start()
    {
        Player_controller.OnHealthChange += HealthChanged;
        m_HealthLabel = UIDoc.rootVisualElement.Q<Label>("HealthLabel");

        HealthChanged();
    }

    void HealthChanged()
    {
        m_HealthLabel.text = $"{Player_controller.CurrentHealth}/{Player_controller.MaxHealth}";
    }
}

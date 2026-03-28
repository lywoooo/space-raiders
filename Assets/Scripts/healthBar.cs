using UnityEngine;
using UnityEngine.UI;

public class healthBar : MonoBehaviour
{
    public Image healthFill;
    public Gradient healthGradient;
    public float maxHealth = 100f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void UpdateHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateUI();
    }

    void UpdateUI()
    {
        float fillPercent = currentHealth / maxHealth;

        healthFill.fillAmount = fillPercent;

        healthFill.color = healthGradient.Evaluate(fillPercent);
    }
}


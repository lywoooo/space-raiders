using UnityEngine;
using UnityEngine.UI;

public class speedBar : MonoBehaviour
{
    public Image speedFill;
    public Gradient speedGradient;
    public float maxSpeed = 100f;
    private float currentSpeed;

    void Start()
    {
        currentSpeed = 0;
        UpdateUI();
    }

    public void UpdateHealth(float amount)
    {
        currentSpeed = Mathf.Clamp(currentSpeed + amount, 0, maxSpeed);
        UpdateUI();
    }

    void UpdateUI()
    {
        float fillPercent = currentSpeed / maxSpeed;

        speedFill.fillAmount = fillPercent;

        speedFill.color = speedGradient.Evaluate(fillPercent);
    }
}


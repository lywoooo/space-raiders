using UnityEngine;
using UnityEngine.UI;

public class buttonHitboxManager : MonoBehaviour
{
    public Button playButton;
    public Button instructionsButton;
    public Button quitButton;

    [Range(0.01f, 1f)]
    public float hitThreshold = 0.1f;

    void Awake()
    {
        ApplyThreshold(playButton);
        ApplyThreshold(instructionsButton);
        ApplyThreshold(quitButton);
    }

    void ApplyThreshold(Button btn)
    {
        if (btn != null && btn.targetGraphic is Image img)
        {
            img.alphaHitTestMinimumThreshold = hitThreshold;
        }
    }
}


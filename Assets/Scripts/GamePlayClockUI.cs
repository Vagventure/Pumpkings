using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayClockUI : MonoBehaviour
{
    [SerializeField] private GameTimerManager gameTimerManager;
    [SerializeField] private Image clockImage;
    [SerializeField] private TextMeshProUGUI clockTimer;

    private void Update()
    {
        if (gameTimerManager == null)
        {
            return;
        }

        GameTimerManager.GameTime currentTime = gameTimerManager.CurrentTime;

        if (clockImage != null)
        {
            clockImage.fillAmount = currentTime.Hour / 24f;
        }

        if (clockTimer != null)
        {
            clockTimer.text = $"{currentTime.Hour:00}:00";
        }
    }
}

using UnityEngine;
using TMPro;

public class GameClockTimerUI : MonoBehaviour
{
    [SerializeField] private GameTimerManager timerManager;
    [SerializeField] private TMP_Text hours;
    [SerializeField] private TMP_Text day;
    [SerializeField] private TMP_Text month;
    [SerializeField] private TMP_Text year;


    private void OnEnable()
    {
        timerManager.OnTimeChanged += HandleTimeChanged;
    }

    private void OnDisable()
    {
        timerManager.OnTimeChanged -= HandleTimeChanged;
    }

    private void HandleTimeChanged(GameTimerManager.GameTime time)
    {
        hours.text = time.Hour.ToString("00") + ":00";
        month.text = time.MonthAbbreviation;
        day.text = time.Day.ToString();
        year.text = time.Year.ToString();
    }
}
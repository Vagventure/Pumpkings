using UnityEngine;
using System;

public class GameTimerManager : MonoBehaviour
{
    public readonly struct GameTime
    {
        public readonly int Hour;
        public readonly int Day;
        public readonly int Month;
        public readonly int Year;

        public GameTime(int hour, int day, int month, int year)
        {
            Hour = hour;
            Day = day;
            Month = month;
            Year = year;
        }

        // Index 0 unused so Month (1-12) can index directly
        private static readonly string[] MonthAbbreviations =
        {
            "", "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };

        public string MonthAbbreviation =>
            (Month >= 1 && Month <= 12) ? MonthAbbreviations[Month] : Month.ToString();

       
    }

    [Header("1 real minute = 1 game hour by default")]
    [SerializeField] private float realSecondsPerGameHour = 60f;

    private float elapsedRealSeconds = 0f;

    private int gameHour = 0;
    private int gameDay = 14;
    private int gameMonth = 3;
    private int gameYear = 2025;

    public GameTime CurrentTime => new GameTime(gameHour, gameDay, gameMonth, gameYear);

    public event Action<GameTime> OnTimeChanged;

    private int lastLoggedHourTotal = -1;
    private int lastTotalDaysPassed = 0;

    private static readonly int[] DaysInMonthLookup =
    {
        0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31
    };

    private static bool IsLeapYear(int year)
    {
        return (year % 4 == 0 && year % 100 != 0) || (year % 400 == 0);
    }

    private static int GetDaysInMonth(int month, int year)
    {
        if (month == 2 && IsLeapYear(year))
        {
            return 29;
        }

        return DaysInMonthLookup[month];
    }

    private void AdvanceOneDay()
    {
        gameDay++;

        int daysInCurrentMonth = GetDaysInMonth(gameMonth, gameYear);
        if (gameDay > daysInCurrentMonth)
        {
            gameDay = 1;
            gameMonth++;

            if (gameMonth > 12)
            {
                gameMonth = 1;
                gameYear++;
            }
        }
    }

    void Update()
    {
        elapsedRealSeconds += Time.deltaTime;

        int totalGameHoursPassed = Mathf.FloorToInt(elapsedRealSeconds / realSecondsPerGameHour);

        gameHour = totalGameHoursPassed % 24;

        int totalDaysPassed = totalGameHoursPassed / 24;

        if (totalDaysPassed != lastTotalDaysPassed)
        {
            int daysToAdvance = totalDaysPassed - lastTotalDaysPassed;
            for (int i = 0; i < daysToAdvance; i++)
            {
                AdvanceOneDay();
            }
            lastTotalDaysPassed = totalDaysPassed;
        }

        if (totalGameHoursPassed != lastLoggedHourTotal)
        {
            lastLoggedHourTotal = totalGameHoursPassed;
            OnTimeChanged?.Invoke(CurrentTime);
        }

        Debug.Log($"{gameYear}-{gameMonth:00}-{gameDay:00} {gameHour:00}:00\"");
    }
}
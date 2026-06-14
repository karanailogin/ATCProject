using UnityEngine;
using System;

public class WorldClockManager : MonoBehaviour
{
    public static WorldClockManager Instance;

    [Header("Virtual Time Settings")]
    [SerializeField] private int startYear = 2026;
    [SerializeField] private int startMonth = 6;
    [SerializeField] private int startDay = 13;
    [SerializeField] private int startHour = 10;
    [SerializeField] private int startMinute = 15;

    [Header("Time Simulation Baseline")]
    public float baseTimeSpeed = 10f; // 10 simulated minutes per real second at 1x

    private DateTime _currentTime;
    public DateTime CurrentTime => _currentTime;

    public float TimeScale { get; set; } = 1f; // 0x (pause), 1x, 2x, 4x

    public float currentVirtualTime
    {
        get
        {
            return (float)_currentTime.TimeOfDay.TotalMinutes;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _currentTime = DateTime.Now;
            baseTimeSpeed = 1f; // 1 real-world second equals 1 in-game minute
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        // Progress virtual time based on baseSpeed and scale factor
        float minutesToAdvance = baseTimeSpeed * TimeScale * Time.deltaTime;
        _currentTime = _currentTime.AddMinutes(minutesToAdvance);
    }

    public string GetFormattedTime()
    {
        return _currentTime.ToString("dd MMM yyyy HH:mm");
    }
}
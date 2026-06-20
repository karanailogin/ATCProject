using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Player Economy")]
    public double funds = 125000; // Starting player funds ($125,000)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string GetFormattedFunds()
    {
        double absoluteFunds = System.Math.Abs(funds);
        string sign = funds < 0 ? "-" : "";

        if (absoluteFunds >= 1000000d)
        {
            return $"{sign}${(absoluteFunds / 1000000d).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}M";
        }

        if (absoluteFunds >= 1000d)
        {
            return $"{sign}${(absoluteFunds / 1000d).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)}K";
        }

        return $"{sign}${absoluteFunds.ToString("0", System.Globalization.CultureInfo.InvariantCulture)}";
    }
}

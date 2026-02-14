using UnityEngine;

public class DifficultyDirector : MonoBehaviour
{
    public static DifficultyDirector Instance;

    [Header("Scaling")]
    public float difficultyCoefficient = 1f;
    public float difficultyPerSecond = 0.02f;
    public AnimationCurve scalingCurve = AnimationCurve.Linear(0, 1, 60, 3);

    private float runTime;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        runTime += Time.deltaTime;

        difficultyCoefficient = scalingCurve.Evaluate(runTime);
    }

    public float GetDifficulty()
    {
        return difficultyCoefficient;
    }
}
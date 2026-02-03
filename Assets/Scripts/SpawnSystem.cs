using UnityEngine;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.UI;

public class SpawnSystem : MonoBehaviour
{
    [Header("Waves")]
    public float waveInterval = 15f;
    public int bigWaveEvery = 5;

    [Header("Difficulty")]
    public float difficultyInterval = 30f;

    [Header("Zombies")]
    public List<ZombieType> zombieTypes;

    public MobSpawner[] mobSpawners;

    public int difficultyLevel { get; private set; }
    public int waveIndex { get; private set; }

    float waveTimer;
    float difficultyTimer;

    UnityEngine.UI.Slider waveBar;
    UnityEngine.UI.Slider difficultyBar;

    Tween waveTween;
    Tween difficultyTween;

    public static Action<float> OnWaveProgress;
    public static Action<float> OnDifficultyProgress;
    public static Action<ZombieType> OnNewZombieUnlocked;

    [System.Serializable]
    public class ZombieType
    {
        public string id;
        public ObjectPool pool;
        public int unlockDifficulty;
    }

    void Start()
    {
        waveBar = GameObject.FindGameObjectWithTag("Wave Bar")?.GetComponent<Slider>();
        difficultyBar = GameObject.FindGameObjectWithTag("Difficulty Bar")?.GetComponent<Slider>();

        if (waveBar != null)
        {
            waveBar.minValue = 0;
            waveBar.maxValue = 1;
            waveBar.value = 0;
        }

        if (difficultyBar != null)
        {
            difficultyBar.minValue = 0;
            difficultyBar.maxValue = 1;
            difficultyBar.value = 0;
        }

        mobSpawners = GetComponentsInChildren<MobSpawner>();
    }

    void Update()
    {
        UpdateWaveTimer();
        UpdateDifficultyTimer();
    }

    void UpdateWaveTimer()
    {
        waveTimer += Time.deltaTime;
        OnWaveProgress?.Invoke(waveTimer / waveInterval);

        if (waveTimer >= waveInterval)
        {
            waveTimer = 0;
            SpawnWave();
        }
    }

    void UpdateDifficultyTimer()
    {
        difficultyTimer += Time.deltaTime;
        OnDifficultyProgress?.Invoke(difficultyTimer / difficultyInterval);

        if (difficultyTimer >= difficultyInterval)
        {
            difficultyTimer = 0;
            IncreaseDifficulty();
        }
    }

    void SpawnWave()
    {
        waveIndex++;

        bool isBigWave = waveIndex % bigWaveEvery == 0;

        foreach (var spawner in mobSpawners)
        {
            spawner.SpawnWave(difficultyLevel, isBigWave);
        }
    }

    void IncreaseDifficulty()
    {
        difficultyLevel++;

        foreach (var z in zombieTypes)
        {
            if (z.unlockDifficulty == difficultyLevel)
                OnNewZombieUnlocked?.Invoke(z);
        }
    }

    List<ObjectPool> GetAvailableZombiePools()
    {
        List<ObjectPool> pools = new();

        foreach (var z in zombieTypes)
            if (difficultyLevel >= z.unlockDifficulty)
                pools.Add(z.pool);

        return pools;
    }

    void OnEnable()
    {
        SpawnSystem.OnWaveProgress += UpdateWaveBar;
        SpawnSystem.OnDifficultyProgress += UpdateDifficultyBar;
    }

    void OnDisable()
    {
        SpawnSystem.OnWaveProgress -= UpdateWaveBar;
        SpawnSystem.OnDifficultyProgress -= UpdateDifficultyBar;
    }

    void UpdateWaveBar(float progress)
    {
        if (waveBar == null) return;

        waveTween?.Kill();
        waveTween = waveBar
            .DOValue(progress, 0.25f)
            .SetEase(Ease.OutCubic);
    }

    void UpdateDifficultyBar(float progress)
    {
        if (difficultyBar == null) return;

        difficultyTween?.Kill();
        difficultyTween = difficultyBar
            .DOValue(progress, 0.25f)
            .SetEase(Ease.OutCubic);
    }

}

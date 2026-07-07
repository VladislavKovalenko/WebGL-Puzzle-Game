// FILE: Scripts/GameData/GameSessionModel.cs
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using _1GameProject.Scripts.GameFlow.Level.LevelGenerator.SO;

namespace _1GameProject.Scripts.GameData
{
    public class GameSessionModel
    {
        public const int MaxLives = 5;

        // --- Глобальные ресурсы ---
        public ReactiveProperty<int> GlobalLives { get; } = new(MaxLives);
        public ReactiveProperty<int> Gold { get; } = new(0);
        public List<string> Perks { get; } = new();

        // --- Данные текущего забега ---
        public int CurrentLevel { get; set; } = 1;
        public int CurrentNode { get; set; } = 0;
        public LevelConfigSO CurrentConfig { get; set; }
        
        // --- Флаги текущего уровня ---
        public bool TookDamageThisLevel { get; set; } = false;

        public void StartNewRun()
        {
            GlobalLives.Value = MaxLives;
            Gold.Value = 0;
            CurrentLevel = 1;
            CurrentNode = 0;
            Perks.Clear();
            ResetLevelFlags();
        }

        public void ResetLevelFlags()
        {
            TookDamageThisLevel = false;
        }

        public void TakeDamage()
        {
            GlobalLives.Value--;
            TookDamageThisLevel = true;
        }

        public void Heal(int amount)
        {
            GlobalLives.Value = Mathf.Min(GlobalLives.Value + amount, MaxLives);
        }
    }
}
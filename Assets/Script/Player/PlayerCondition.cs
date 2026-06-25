using GlobalEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Script.Player
{
    public class PlayerCondition : NetworkBehaviour, IDamagable
    {
        [Header("Paramètres de survie (assigner les StatSO)")]
        [SerializeField] private StatSO[] _statConfig;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy == value) return;

                _isBusy = value;

                if (_isBusy)
                    OnBusy?.Invoke();
            }
        }

        public bool _isBusy = false;
        private readonly float OnRecup = 2.5f;

        public bool IsDead
        {
            get => _isDead; set
            {
                if (_isDead == value) return;

                _isDead = value;
            }
        }
        private bool _isDead = false;
        public MovementState _movementState;
        public PoseState _poseState;

        // ─── Instances runtime ────────────────────────────────────────────
        private Dictionary<PlayerStat, StatInstance> _stats = new();        // Stat du joueur ( HP, Stamina, etc..)

        // ─── Accès rapide ─────────────────────────────────────────────────
        public StatInstance GetSurvivalStat(PlayerStat value) => Get(value);

        // ─── Événements ───────────────────────────────────────────────────
        public event Action<StatInstance, float>            OnStatChanged;
        public event Action<StatInstance, CriticalLevel>    OnStatLevelChanged;
        public event Action<StatInstance, float, bool>      OnBuffAdditive;
        public event Action                                 OnPlayerDied;
        public event Action                                 OnBusy;
        public event Action                                 Outline;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void HandleBusy() => StartCoroutine(OnRecuperation());

        private void Awake()
        {
            foreach (var _config in _statConfig)
                InitStat(_config);
            
            Get(PlayerStat.Energie).OnReachedZero += HandleBusy;
        }

        void Start()
        {
            foreach (var _config in _statConfig)
                InitStat(_config);
        }

        private void InitStat(StatSO config)
        {
            if (config == null) return;

            var instance = new StatInstance(config);
            instance.OnValueChanged += (s, d) => OnStatChanged?.Invoke(s, d);
            instance.OnLevelChanged += (s, l) => OnStatLevelChanged?.Invoke(s, l);
            instance.OnDeadReachedZero += HandleStatReachedZero;

            _stats[config.statType] = instance;
        }

        // ═════════════════════════════════════════════════════════════════
        // UPDATE
        // ═════════════════════════════════════════════════════════════════

        private void Update()
        {
            float dt = Time.deltaTime;

            // ── 1. Tick de chaque stat (dégradation naturelle) ────────────
            foreach (var stat in _stats.Values)
                stat.Tick(dt);
        }

        IEnumerator OnRecuperation()
        {
            IsBusy = true;
            float dt = 0;
            while (IsBusy)
            {
                dt += Time.deltaTime;
                if (dt >= OnRecup)
                    IsBusy = false;
                yield return null;
            }
            yield return null;
        }

        // ═════════════════════════════════════════════════════════════════
        // MORT
        // ═════════════════════════════════════════════════════════════════

        private void HandleStatReachedZero(StatInstance stat)
        {
            if (stat.Config.statType == PlayerStat.Health)
                OnPlayerDied?.Invoke();
            else if (stat.Config.causesDeathAtZero)
                OnPlayerDied?.Invoke();

            IsDead = true;
        }

        // ═════════════════════════════════════════════════════════════════
        // API PUBLIQUE
        // ═════════════════════════════════════════════════════════════════

        public StatInstance Get(PlayerStat type)
        => _stats.TryGetValue(type, out var s) ? s : null;

        /// <summary>Sprint — consomme de la stamina et de l'énergie.</summary>
        public void OnConsume(PlayerStat type, float dt) 
            => Get(type).Modify(-dt);

        public bool Has(PlayerStat type) => _stats.ContainsKey(type);

        /// <summary>Remet toutes les stats à leur valeur de départ (respawn).</summary>
        public void ResetAll()
        {
            foreach (var stat in _stats.Values)
            {
                stat.Set(stat.Config.startValue);
                stat.ResetModifiers();
            }
            //SpeedMultiplier = 1f;
            //StaminaMultiplier = 1f;
        }

        /// <summary>
        /// Appelé par CharacterToECSBridge quand un DamageEvent arrive.
        /// Calcule la résistance du joueur (compétences) avant d'appliquer.
        /// </summary>
        public void Damage(float dmg)
        {
            if (IsDead) return;

            var stat = Get(PlayerStat.Health);
            if (stat == null) return;

            stat.Modify(-dmg);
        }
    }
}
using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Character : MonoBehaviour
    {
        public MovingEntity Entity { get; private set; }

        [Header("Attributes")]
        public Sprite Avatar;
        public float MaxActionPoints;
        public float MaxStamina;
        public float StaminaRegeneration;

        // Current stats
        public float ActionPoints { get; private set; }
        public float Stamina { get; private set; }

        private void Awake()
        {
            Entity = GetComponent<MovingEntity>(); 
        }

        public void OnStartGame()
        {
            ActionPoints = MaxActionPoints;
            Stamina = MaxStamina;
        }

        public void OnStartTurn()
        {
            ActionPoints = MaxActionPoints;
            Stamina += StaminaRegeneration;
            if (Stamina > MaxStamina) Stamina = MaxStamina;
        }
    }
}

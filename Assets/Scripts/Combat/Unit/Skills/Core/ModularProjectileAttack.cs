using System;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Ataque básico À DISTÂNCIA modular: a entrega é decisão de CÓDIGO (dispara um projétil no
    /// hit-frame; o golpe — <see cref="ModularBaseAttack.DoStrike"/> — roda na CHEGADA). A subclasse só
    /// implementa <see cref="ModularBaseAttack.OnStrike"/> (dano/efeitos); o único parâmetro de asset é
    /// o projétil (clipe + velocidade).
    /// </summary>
    public abstract class ModularProjectileAttack : ModularBaseAttack
    {
        [Header("Projétil (clipe + velocidade)")]
        [SerializeField] private ProjectileSpec projectile = ProjectileSpec.Default;

        // Delegate cacheado p/ a chegada (evita alocar closure por disparo).
        private Action<SkillContext> onArrive;

        public override void OnHit(SkillContext context)
        {
            UnitController target = context.target;
            if (target == null || context.owner == null) return;

            Vector3 origin = context.owner.transform.position;
            float speed = projectile.speedBase + projectile.speedPerSpeed * (context.ownerStats?.Speed ?? DefaultSpeed);

            onArrive ??= DoStrike;
            Projectile p = ProjectilePool.Instance.Spawn(origin);
            p.Launch(projectile.clip, origin, target, speed, projectile.sortingOrder, projectile.scale,
                     projectile.faceTravelDirection, onArrive, context);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Playables;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Projétil direcionado (homing) de alvo único, gerado por código (sem prefab) e poolado.
    /// O visual é um AnimationClip tocado diretamente (Playables). O dano (onArrive) só dispara
    /// na chegada; se o alvo morrer/sumir em voo, é descartado sem efeito.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Animator))]
    public class Projectile : MonoBehaviour
    {
        private const float ArriveDistance = 0.15f;

        private SpriteRenderer spriteRenderer;
        private Animator animator;
        private PlayableGraph graph;

        private UnitController target;
        private StatsModule targetStats; // cacheado no Launch — evita GetModule por frame na validação do alvo
        private float speed;
        private bool faceTravelDirection;
        // Callback + payload por valor: evita alocar uma closure por disparo (o caller passa um
        // delegate cacheado e o contexto como struct).
        private Action<SkillContext> onArrive;
        private SkillContext arrivePayload;
        private bool active;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        public void Launch(AnimationClip clip, Vector3 startPosition, UnitController targetUnit,
                           float projectileSpeed, int sortingOrder, float scale, bool faceDirection,
                           Action<SkillContext> onArriveCallback, SkillContext arriveContext)
        {
            transform.position = startPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * (scale <= 0f ? 1f : scale);

            target = targetUnit;
            targetStats = targetUnit != null ? targetUnit.Stats : null;
            speed = projectileSpeed;
            faceTravelDirection = faceDirection;
            onArrive = onArriveCallback;
            arrivePayload = arriveContext;

            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;
            spriteRenderer.sortingOrder = sortingOrder;
            active = true;

            if (clip != null) graph = ClipPlayer.Play(animator, clip);
            else DebugManager.LogWarning("Projectile: clip nulo — projétil sem visual.", DebugCategory.Combat);
        }

        private void Update()
        {
            if (!active) return;

            if (!IsTargetValid())
            {
                Despawn(invokeArrive: false);
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;

            if (toTarget.sqrMagnitude <= ArriveDistance * ArriveDistance)
            {
                Despawn(invokeArrive: true);
                return;
            }

            float distance = toTarget.magnitude;
            Vector3 dir = toTarget / distance;
            transform.position += dir * (speed * Time.deltaTime);

            if (faceTravelDirection)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
                // Evita o projétil "de ponta cabeça" ao viajar para a esquerda (ângulo > 90°).
                spriteRenderer.flipY = Mathf.Abs(angle) > 90f;
            }
        }

        private bool IsTargetValid()
        {
            if (target == null) return false;
            var stats = target.GetModule<StatsModule>();
            return stats != null && !stats.IsDead;
        }

        private void Despawn(bool invokeArrive)
        {
            active = false;
            ClipPlayer.Stop(ref graph);

            Action<SkillContext> callback = onArrive;
            SkillContext payload = arrivePayload;
            onArrive = null;
            arrivePayload = default;
            target = null;
            targetStats = null;

            if (invokeArrive) callback?.Invoke(payload);

            ProjectilePool.Instance.Return(this);
        }

        private void OnDestroy() => ClipPlayer.Stop(ref graph);
    }
}

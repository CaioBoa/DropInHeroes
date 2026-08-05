using System.Collections.Generic;
using UnityEngine;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    /// <summary>
    /// Pool de projéteis genéricos gerados por código (sem prefab). Reutiliza instâncias para
    /// evitar alocação no hot path de combate. Auto-instancia um host na cena se nenhum existir.
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        private static ProjectilePool instance;
        public static ProjectilePool Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("ProjectilePool");
                    instance = go.AddComponent<ProjectilePool>();
                }
                return instance;
            }
        }

        private readonly Queue<Projectile> pool = new Queue<Projectile>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void OnDestroy()
        {
            // Evita o campo estático apontar para um objeto destruído após troca de cena.
            if (instance == this) instance = null;
        }

        // Reset para sessões de Play com domain reload desabilitado.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        /// <summary>Pega (ou cria) um projétil pronto para Launch, posicionado em 'position'.</summary>
        public Projectile Spawn(Vector3 position)
        {
            Projectile projectile = Rent();
            Transform t = projectile.transform;
            t.SetParent(transform);
            t.position = position;
            projectile.gameObject.SetActive(true);
            return projectile;
        }

        public void Return(Projectile projectile)
        {
            if (projectile == null) return;
            projectile.gameObject.SetActive(false);
            pool.Enqueue(projectile);
        }

        private Projectile Rent()
        {
            while (pool.Count > 0)
            {
                Projectile pooled = pool.Dequeue();
                if (pooled != null) return pooled;
            }
            // RequireComponent adiciona SpriteRenderer + Animator automaticamente.
            var go = new GameObject("Projectile", typeof(Projectile));
            return go.GetComponent<Projectile>();
        }
    }
}

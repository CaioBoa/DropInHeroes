using System.Collections.Generic;
using DropInHeroes.Data;
using DropInHeroes.Utils;

namespace DropInHeroes.Combat
{

    public struct SkillContext
    {
        public UnitController owner;
        public UnitController target;
        public StatsModule ownerStats;
        public VisualModule ownerVisual;
        public SkillsModule skills;

        // Inimigos do dono (a equipe oposta). Usado por skills de área/multi-alvo e auras passivas.
        public IReadOnlyList<UnitController> enemies;

        // Aliados do dono (a própria equipe, incluindo ele mesmo). Usado por cura, buffs e auras.
        public IReadOnlyList<UnitController> allies;

        // Se true, os efeitos de dano deste run PODEM causar crítico. Setado pelo shell da skill:
        // ataques básicos e habilidades/supremos = true; efeitos on-hit (riders) = false.
        // DoT nunca crita (não passa pelo DamageCalculator). Equipamentos futuros podem liberar on-hit.
        public bool allowCrit;

        // True enquanto o run atual é a MECÂNICA ON-HIT (riders registrados no OnHitModule). Riders
        // não critam e não contam como "ataque recebido" — são caroneiros do golpe, não o golpe.
        public bool isOnHit;
    }
}

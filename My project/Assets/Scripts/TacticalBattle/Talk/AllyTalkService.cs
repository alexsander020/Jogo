using System;
using System.Collections.Generic;
using TacticalBattle.Core;

namespace TacticalBattle.Talk
{
    public static class AllyTalkService
    {
        // ==========================================
        // 6.3 FLUXO DE TALK COM ALIADOS (BUFFS)
        // ==========================================
        // Tabela configurável de mapeamento de afinidade/personalidade para bônus de combate
        public static AllyTalkResult ResolveAllyTalk(float allyAffinityLevel, PersonalityTrait personality)
        {
            // Clamping preventivo
            float affinity = Math.Clamp(allyAffinityLevel, 0f, 100f);

            int tier = affinity switch
            {
                >= 80f => 3, // Afinidade Máxima
                >= 40f => 2, // Afinidade Média
                _ => 1       // Afinidade Inicial
            };

            return personality switch
            {
                PersonalityTrait.Brave => new AllyTalkResult
                {
                    buffType = AllyBuffType.AttackUp,
                    magnitude = tier * 10, // +10, +20, +30 ATK
                    durationTurns = 2
                },
                PersonalityTrait.Cautious => new AllyTalkResult
                {
                    buffType = AllyBuffType.SpRegen,
                    magnitude = tier * 5, // +5, +10, +15 SP
                    durationTurns = 3
                },
                PersonalityTrait.Kind => new AllyTalkResult
                {
                    buffType = AllyBuffType.InstantHeal,
                    magnitude = tier * 30, // 30, 60, 90 HP
                    durationTurns = 0
                },
                PersonalityTrait.Aggressive => new AllyTalkResult
                {
                    buffType = AllyBuffType.AttackUp,
                    magnitude = tier * 15,
                    durationTurns = 1
                },
                PersonalityTrait.Logical => new AllyTalkResult
                {
                    buffType = AllyBuffType.HpRegen,
                    magnitude = tier * 10,
                    durationTurns = 3
                },
                _ => new AllyTalkResult
                {
                    buffType = AllyBuffType.AttackUp,
                    magnitude = 10,
                    durationTurns = 2
                }
            };
        }
    }
}

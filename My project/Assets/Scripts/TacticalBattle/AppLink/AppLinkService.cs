using System;
using System.Collections.Generic;
using TacticalBattle.Appmon;
using UnityEngine;

namespace TacticalBattle.AppLink
{
    [Serializable]
    public class StatBonusEntry
    {
        public StatEnum stat;
        public int basePercent;
        public float finalPercent;
        public int flatBonusValue;

        public StatBonusEntry(StatEnum stat, int basePercent, float finalPercent, int flatBonusValue)
        {
            this.stat = stat;
            this.basePercent = basePercent;
            this.finalPercent = finalPercent;
            this.flatBonusValue = flatBonusValue;
        }
    }

    [Serializable]
    public class AppLinkBonusCalculation
    {
        public EvolutionRank rank;
        public bool hasCompatibility;
        public float compatibilityMultiplier;
        public List<StatBonusEntry> statBonuses = new List<StatBonusEntry>();
        public SkillData transferredSkill;
        public string transferredPassiveDesc;

        public string GetBonusSummary()
        {
            List<string> parts = new List<string>();
            foreach (var b in statBonuses)
            {
                parts.Add($"+{b.finalPercent:0.#}% {b.stat} (+{b.flatBonusValue})");
            }
            return string.Join(", ", parts);
        }
    }

    [Serializable]
    public class AppLinkRecord
    {
        public Unit fieldUnit;
        public AppmonData bagAppmon;
        public AppLinkBonusCalculation calculation;
        public SkillData transferredSkill;
        public DateTime linkTime;

        public AppLinkRecord(Unit fieldUnit, AppmonData bagAppmon, AppLinkBonusCalculation calculation, SkillData transferredSkill)
        {
            this.fieldUnit = fieldUnit;
            this.bagAppmon = bagAppmon;
            this.calculation = calculation;
            this.transferredSkill = transferredSkill;
            this.linkTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Gerenciador central do Sistema de App-Link.
    /// Controla vínculos, escalonamento por rank, bônus de compatibilidade, exclusividade e distribuição de XP.
    /// </summary>
    public static class AppLinkService
    {
        public const int MAX_ACTIVE_LINKS = 6;

        // Registro de links ativos indexados pela Unidade em campo
        private static readonly Dictionary<Unit, AppLinkRecord> activeLinks = new Dictionary<Unit, AppLinkRecord>();

        // Eventos para UI e telemetria
        public static event Action<Unit, AppmonData> OnAppLinkCreated;
        public static event Action<Unit, AppmonData> OnAppLinkRemoved;
        public static event Action<int, int> OnXpDistributed;

        public static int ActiveLinkCount
        {
            get
            {
                CleanStaleLinks();
                return activeLinks.Count;
            }
        }

        public static IReadOnlyDictionary<Unit, AppLinkRecord> ActiveLinks
        {
            get
            {
                CleanStaleLinks();
                return activeLinks;
            }
        }

        /// <summary>
        /// Remove referências nulas, destruídas ou inativas de unidades do registro de links.
        /// </summary>
        public static void CleanStaleLinks()
        {
            List<Unit> deadKeys = new List<Unit>();
            foreach (var kvp in activeLinks)
            {
                try
                {
                    if (kvp.Key == null || kvp.Key.gameObject == null || !kvp.Key.gameObject.activeInHierarchy || !kvp.Key.IsAlive)
                    {
                        deadKeys.Add(kvp.Key);
                    }
                }
                catch
                {
                    deadKeys.Add(kvp.Key);
                }
            }
            foreach (var k in deadKeys)
            {
                activeLinks.Remove(k);
            }
        }

        /// <summary>
        /// Verifica se o Appmon da reserva está atualmente vinculado a qualquer unidade de campo.
        /// </summary>
        public static bool IsAppmonLinked(AppmonData appmon)
        {
            if (appmon == null) return false;
            return GetLinkedFieldUnit(appmon) != null;
        }

        /// <summary>
        /// Retorna a unidade de campo que está linkada ao Appmon da Bag, ou null se livre.
        /// Inspeciona tanto o registro activeLinks quanto as unidades em combate no BattleController.
        /// </summary>
        public static Unit GetLinkedFieldUnit(AppmonData appmon)
        {
            if (appmon == null) return null;
            CleanStaleLinks();

            // 1. Inspeciona o dicionário de links ativos
            foreach (var kvp in activeLinks)
            {
                if (kvp.Key != null && kvp.Value != null && kvp.Value.bagAppmon != null)
                {
                    if ((!string.IsNullOrEmpty(appmon.id) && kvp.Value.bagAppmon.id.Equals(appmon.id, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(appmon.name) && kvp.Value.bagAppmon.name.Equals(appmon.name, StringComparison.OrdinalIgnoreCase)))
                    {
                        return kvp.Key;
                    }
                }
            }

            // 2. Fallback robusto: inspeciona diretamente todas as unidades de combate ativas
            if (BattleController.Instance != null && BattleController.Instance.allUnits != null)
            {
                foreach (var u in BattleController.Instance.allUnits)
                {
                    if (u != null && u.gameObject != null && u.gameObject.activeInHierarchy && u.IsAlive && u.IsLinked && u.linkedBagAppmon != null)
                    {
                        if ((!string.IsNullOrEmpty(appmon.id) && u.linkedBagAppmon.id.Equals(appmon.id, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(appmon.name) && u.linkedBagAppmon.name.Equals(appmon.name, StringComparison.OrdinalIgnoreCase)))
                        {
                            return u;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Verifica se o Appmon está atualmente em combate ativo no campo de batalha.
        /// </summary>
        public static bool IsAppmonDeployed(AppmonData appmon)
        {
            if (appmon == null || BattleController.Instance == null) return false;

            foreach (var u in BattleController.Instance.allUnits)
            {
                if (u != null && u.gameObject != null && u.gameObject.activeInHierarchy)
                {
                    var comp = u.GetComponent<AppmonCharacter>();
                    if (comp != null && comp.appmonData != null && comp.appmonData.id.Equals(appmon.id, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    if (u.unitName.Equals(appmon.name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Retorna a lista de Appmons da reserva (Bag) do jogador que não estão em campo.
        /// Se for informado forUnit, exclui completamente Appmons vinculados a OUTROS combatentes,
        /// preservando o parceiro atual da própria unidade (caso ela já possua um).
        /// </summary>
        public static List<AppmonData> GetBagAppmons(Unit forUnit = null)
        {
            CleanStaleLinks();
            List<AppmonData> all = new List<AppmonData>(AppmonDatabase.GetAll());
            List<AppmonData> bag = new List<AppmonData>();

            foreach (var app in all)
            {
                if (app == null) continue;

                // 1. Appmon em combate no campo não conta como Bag
                if (IsAppmonDeployed(app)) continue;

                // 2. Exclusividade Absoluta 1-para-1: Se já estiver vinculado a outro personagem aliado,
                // este Appmon NÃO aparece na Bag desta unidade
                Unit linkedHolder = GetLinkedFieldUnit(app);
                if (linkedHolder != null && (forUnit == null || linkedHolder != forUnit))
                {
                    continue;
                }

                bag.Add(app);
            }
            return bag;
        }

        /// <summary>
        /// Valida se uma unidade de campo pode se conectar ao Appmon da Bag.
        /// Permite substituição limpa (swap) se a unidade já possuir um vínculo ativo.
        /// </summary>
        public static bool CanLink(Unit fieldUnit, AppmonData bagAppmon, out string reason)
        {
            CleanStaleLinks();

            if (fieldUnit == null)
            {
                reason = "Unidade de campo inválida.";
                return false;
            }

            if (bagAppmon == null)
            {
                reason = "Appmon de suporte inválido.";
                return false;
            }

            if (IsAppmonDeployed(bagAppmon))
            {
                reason = "Este Appmon já está desdobrado em combate no campo!";
                return false;
            }

            // Exclusividade 1-para-1: se outro personagem já estiver usando este Appmon
            Unit linkedHolder = GetLinkedFieldUnit(bagAppmon);
            if (linkedHolder != null && linkedHolder != fieldUnit)
            {
                reason = $"Este Appmon já está [LINKADO] a {linkedHolder.unitName}!";
                return false;
            }

            // Se for exatamente o parceiro já conectado a este personagem
            if (fieldUnit.IsLinked && fieldUnit.linkedBagAppmon != null &&
                fieldUnit.linkedBagAppmon.id.Equals(bagAppmon.id, StringComparison.OrdinalIgnoreCase))
            {
                reason = "Este Appmon já é o parceiro vinculado a este personagem.";
                return false;
            }

            // Capacidade máxima da equipe
            if (!activeLinks.ContainsKey(fieldUnit) && activeLinks.Count >= MAX_ACTIVE_LINKS)
            {
                reason = $"Limite máximo de {MAX_ACTIVE_LINKS} App-Links ativos atingido no time!";
                return false;
            }

            reason = "Pronto para conectar.";
            return true;
        }

        /// <summary>
        /// Calcula os bônus e habilidades transferidas segundo as regras de escalonamento e compatibilidade.
        /// </summary>
        public static AppLinkBonusCalculation CalculateBonus(Unit fieldUnit, AppmonData bagAppmon)
        {
            AppLinkBonusCalculation calc = new AppLinkBonusCalculation();
            if (fieldUnit == null || bagAppmon == null) return calc;

            calc.rank = bagAppmon.rank;

            // 4. Bônus de Compatibilidade: Mesmo tipo de atributo funcional -> +50% multiplicador
            calc.hasCompatibility = (fieldUnit.category == bagAppmon.primaryCategory) ||
                                    (bagAppmon.secondaryCategory.HasValue && fieldUnit.category == bagAppmon.secondaryCategory.Value);
            calc.compatibilityMultiplier = calc.hasCompatibility ? 1.50f : 1.00f;

            // Determina os atributos principais do parceiro de campo
            StatEnum primaryStat = GetHighestStat(fieldUnit);
            StatEnum secondaryStat = GetSecondHighestStat(fieldUnit, primaryStat);

            // 3. Tabela de Escalonamento de Ganhos
            switch (bagAppmon.rank)
            {
                case EvolutionRank.Standard:
                    // Standard: +5% em um atributo aleatório/específico + 1 Passiva básica
                    StatEnum standardStat = PickStandardStat(bagAppmon);
                    int baseStandard = 5;
                    float finalStandard = baseStandard * calc.compatibilityMultiplier;
                    int curValStd = fieldUnit.stats != null ? fieldUnit.stats.GetStat(standardStat) : 20;
                    int flatStd = Mathf.Max(1, Mathf.RoundToInt(curValStd * (finalStandard / 100f)));

                    calc.statBonuses.Add(new StatBonusEntry(standardStat, baseStandard, finalStandard, flatStd));
                    calc.transferredSkill = CreateTransferredSkill(bagAppmon, isPassiveOnly: true);
                    calc.transferredPassiveDesc = bagAppmon.passiveDescription;
                    break;

                case EvolutionRank.Super:
                    // Super: +10% no atributo principal do parceiro + 1 Habilidade Ativa básica ou Passiva
                    int baseSuper = 10;
                    float finalSuper = baseSuper * calc.compatibilityMultiplier;
                    int curValSup = fieldUnit.stats != null ? fieldUnit.stats.GetStat(primaryStat) : 25;
                    int flatSup = Mathf.Max(2, Mathf.RoundToInt(curValSup * (finalSuper / 100f)));

                    calc.statBonuses.Add(new StatBonusEntry(primaryStat, baseSuper, finalSuper, flatSup));
                    calc.transferredSkill = CreateTransferredSkill(bagAppmon, isPassiveOnly: false);
                    break;

                case EvolutionRank.Ultimate:
                    // Ultimate: +15% distribuídos nos 2 principais atributos (+8% e +7%) + 1 Ativa avançada
                    int baseUlt1 = 8;
                    int baseUlt2 = 7;
                    float finalUlt1 = baseUlt1 * calc.compatibilityMultiplier;
                    float finalUlt2 = baseUlt2 * calc.compatibilityMultiplier;

                    int curValU1 = fieldUnit.stats != null ? fieldUnit.stats.GetStat(primaryStat) : 30;
                    int curValU2 = fieldUnit.stats != null ? fieldUnit.stats.GetStat(secondaryStat) : 25;
                    int flatU1 = Mathf.Max(2, Mathf.RoundToInt(curValU1 * (finalUlt1 / 100f)));
                    int flatU2 = Mathf.Max(2, Mathf.RoundToInt(curValU2 * (finalUlt2 / 100f)));

                    calc.statBonuses.Add(new StatBonusEntry(primaryStat, baseUlt1, finalUlt1, flatU1));
                    calc.statBonuses.Add(new StatBonusEntry(secondaryStat, baseUlt2, finalUlt2, flatU2));
                    calc.transferredSkill = CreateTransferredSkill(bagAppmon, isPassiveOnly: false, preferAdvanced: true);
                    break;

                case EvolutionRank.God:
                case EvolutionRank.Demon:
                default:
                    // God / Demon: +25% no atributo principal + Habilidade Assinatura / Passiva de Elite
                    int baseGod = 25;
                    float finalGod = baseGod * calc.compatibilityMultiplier;
                    int curValGod = fieldUnit.stats != null ? fieldUnit.stats.GetStat(primaryStat) : 40;
                    int flatGod = Mathf.Max(3, Mathf.RoundToInt(curValGod * (finalGod / 100f)));

                    calc.statBonuses.Add(new StatBonusEntry(primaryStat, baseGod, finalGod, flatGod));
                    calc.transferredSkill = CreateTransferredSkill(bagAppmon, isPassiveOnly: false, preferSignature: true);
                    break;
            }

            return calc;
        }

        /// <summary>
        /// Aplica o App-Link à unidade de campo e consome a ação do turno.
        /// Se a unidade já possuir um parceiro vinculado, realiza a substituição direta (1 link por personagem).
        /// </summary>
        public static bool ApplyLink(Unit fieldUnit, AppmonData bagAppmon, out string error)
        {
            CleanStaleLinks();

            if (!CanLink(fieldUnit, bagAppmon, out error))
            {
                return false;
            }

            // REGRA: Apenas 1 Link por personagem!
            // Se a unidade já possui um parceiro anterior, remove-o completamente antes de vincular o novo
            if (activeLinks.ContainsKey(fieldUnit) || fieldUnit.IsLinked)
            {
                bool hadActed = fieldUnit.hasActed;
                RemoveLink(fieldUnit);
                fieldUnit.hasActed = hadActed;
            }

            AppLinkBonusCalculation calc = CalculateBonus(fieldUnit, bagAppmon);

            // 1. Aplica modificadores de status
            if (fieldUnit.stats != null)
            {
                foreach (var bonus in calc.statBonuses)
                {
                    fieldUnit.stats.ModifyStat(bonus.stat, bonus.flatBonusValue);
                    if (bonus.stat == StatEnum.HP) fieldUnit.stats.ModifyStat(StatEnum.MaxHp, bonus.flatBonusValue);
                    if (bonus.stat == StatEnum.MP) fieldUnit.stats.ModifyStat(StatEnum.MaxMp, bonus.flatBonusValue);
                    if (bonus.stat == StatEnum.SP) fieldUnit.stats.ModifyStat(StatEnum.MaxSp, bonus.flatBonusValue);
                }
            }

            // 2. Transfere a habilidade
            if (calc.transferredSkill != null)
            {
                if (fieldUnit.skills == null) fieldUnit.skills = new List<SkillData>();
                if (!fieldUnit.skills.Contains(calc.transferredSkill))
                {
                    fieldUnit.skills.Add(calc.transferredSkill);
                }
            }

            // 3. Registra o vínculo
            AppLinkRecord record = new AppLinkRecord(fieldUnit, bagAppmon, calc, calc.transferredSkill);
            activeLinks[fieldUnit] = record;
            fieldUnit.linkedBagAppmon = bagAppmon;
            fieldUnit.currentAppLink = record;

            // 4. Marca o turno como agido (gasta a ação)
            fieldUnit.hasActed = true;

            OnAppLinkCreated?.Invoke(fieldUnit, bagAppmon);
            Debug.Log($"[App-Link] {fieldUnit.unitName} conectou-se com sucesso a {bagAppmon.name}! Bônus: {calc.GetBonusSummary()} | Compatibilidade: {calc.hasCompatibility}");
            return true;
        }

        /// <summary>
        /// Remove e desvincula o App-Link da unidade de campo, restaurando seus atributos.
        /// </summary>
        public static bool RemoveLink(Unit fieldUnit)
        {
            CleanStaleLinks();

            if (fieldUnit == null) return false;

            activeLinks.TryGetValue(fieldUnit, out AppLinkRecord record);
            if (record == null && !fieldUnit.IsLinked) return false;

            AppmonData bagAppmon = record != null ? record.bagAppmon : fieldUnit.linkedBagAppmon;

            // 1. Reverte bônus de status
            if (fieldUnit.stats != null && record != null && record.calculation != null)
            {
                foreach (var bonus in record.calculation.statBonuses)
                {
                    fieldUnit.stats.ModifyStat(bonus.stat, -bonus.flatBonusValue);
                    if (bonus.stat == StatEnum.HP) fieldUnit.stats.ModifyStat(StatEnum.MaxHp, -bonus.flatBonusValue);
                    if (bonus.stat == StatEnum.MP) fieldUnit.stats.ModifyStat(StatEnum.MaxMp, -bonus.flatBonusValue);
                    if (bonus.stat == StatEnum.SP) fieldUnit.stats.ModifyStat(StatEnum.MaxSp, -bonus.flatBonusValue);
                }
            }

            // 2. Remove habilidade transferida
            if (record != null && record.transferredSkill != null && fieldUnit.skills != null)
            {
                fieldUnit.skills.Remove(record.transferredSkill);
            }

            // 3. Limpa referências
            activeLinks.Remove(fieldUnit);
            fieldUnit.linkedBagAppmon = null;
            fieldUnit.currentAppLink = null;

            // Gasta a ação do turno se estiver em combate ativo
            fieldUnit.hasActed = true;

            if (bagAppmon != null)
            {
                OnAppLinkRemoved?.Invoke(fieldUnit, bagAppmon);
                Debug.Log($"[App-Link] Conexão desfeita entre {fieldUnit.unitName} e {bagAppmon.name}.");
            }
            return true;
        }

        /// <summary>
        /// Distribuição de XP pós-batalha: 100% para os Appmons de campo e 50% para suportes [LINKADO] na Bag.
        /// </summary>
        public static void DistributeBattleXp(int baseBattleXp)
        {
            int fieldXp = baseBattleXp;
            int bagXp = Mathf.RoundToInt(baseBattleXp * 0.50f);

            Debug.Log($"[App-Link XP] Distribuindo experiência de batalha: {fieldXp} XP para combatentes em campo, {bagXp} XP (50%) para parceiros vinculados na Bag.");

            OnXpDistributed?.Invoke(fieldXp, bagXp);
        }

        /// <summary>
        /// Limpa todos os links ativos (ex: reset de batalha ou play mode na Unity).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetAllLinks()
        {
            foreach (var kvp in new List<KeyValuePair<Unit, AppLinkRecord>>(activeLinks))
            {
                if (kvp.Key != null)
                {
                    RemoveLink(kvp.Key);
                }
            }
            activeLinks.Clear();
        }

        // =========================================================================
        // MÉTODOS AUXILIARES DE CÁLCULO E HABILIDADES
        // =========================================================================

        private static StatEnum GetHighestStat(Unit unit)
        {
            if (unit == null || unit.stats == null) return StatEnum.ATK;

            StatEnum[] candidates = new StatEnum[] { StatEnum.ATK, StatEnum.DEF, StatEnum.INT, StatEnum.SPI, StatEnum.SPEED };
            StatEnum best = StatEnum.ATK;
            int maxVal = int.MinValue;

            foreach (var s in candidates)
            {
                int v = unit.stats.GetStat(s);
                if (v > maxVal)
                {
                    maxVal = v;
                    best = s;
                }
            }
            return best;
        }

        private static StatEnum GetSecondHighestStat(Unit unit, StatEnum highest)
        {
            if (unit == null || unit.stats == null) return StatEnum.DEF;

            StatEnum[] candidates = new StatEnum[] { StatEnum.ATK, StatEnum.DEF, StatEnum.INT, StatEnum.SPI, StatEnum.SPEED };
            StatEnum secondBest = StatEnum.DEF;
            int maxVal = int.MinValue;

            foreach (var s in candidates)
            {
                if (s == highest) continue;
                int v = unit.stats.GetStat(s);
                if (v > maxVal)
                {
                    maxVal = v;
                    secondBest = s;
                }
            }
            return secondBest;
        }

        private static StatEnum PickStandardStat(AppmonData appmon)
        {
            if (appmon == null) return StatEnum.ATK;

            // Mapeamento por categoria para atributos temáticos
            return appmon.primaryCategory switch
            {
                FunctionalCategory.Social => StatEnum.HP,
                FunctionalCategory.Navi => StatEnum.SPEED,
                FunctionalCategory.Tool => StatEnum.DEF,
                FunctionalCategory.Game => StatEnum.CRT,
                FunctionalCategory.Entertainment => StatEnum.MP,
                FunctionalCategory.Life => StatEnum.HP,
                FunctionalCategory.System => StatEnum.INT,
                FunctionalCategory.Security => StatEnum.SPI,
                _ => StatEnum.ATK
            };
        }

        private static SkillData CreateTransferredSkill(AppmonData bagAppmon, bool isPassiveOnly, bool preferAdvanced = false, bool preferSignature = false)
        {
            if (bagAppmon == null) return null;

            // Se o Appmon já possui habilidades cadastradas
            if (bagAppmon.skills != null && bagAppmon.skills.Count > 0)
            {
                if (preferSignature && bagAppmon.skills.Count >= 2)
                {
                    return bagAppmon.skills[bagAppmon.skills.Count - 1];
                }
                if (preferAdvanced && bagAppmon.skills.Count >= 2)
                {
                    return bagAppmon.skills[1];
                }
                return bagAppmon.skills[0];
            }

            // Fallback elegante com base na passiva do Appmon
            string skillName = !string.IsNullOrEmpty(bagAppmon.passiveName) ? $"[Link] {bagAppmon.passiveName}" : $"[Link] {bagAppmon.name}";
            string desc = !string.IsNullOrEmpty(bagAppmon.passiveDescription) 
                ? bagAppmon.passiveDescription 
                : $"Poder sincronizado transferido de {bagAppmon.name}.";

            int power = isPassiveOnly ? 0 : 95;
            int spCost = isPassiveOnly ? 0 : 20;

            return SkillData.CreateSpecialSkill(
                skillName, 
                power, 
                spCost, 
                bagAppmon.primaryCategory, 
                desc, 
                2, 
                TacticalBattle.Core.AttackShapeType.Single, 
                0, 
                "🔗", 
                "UI_Skill_Icon_Slash"
            );
        }
    }
}

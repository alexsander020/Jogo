using System;
using System.Collections.Generic;
using UnityEngine;

namespace TacticalBattle.Appmon
{
    [Serializable]
    public class AppmonData
    {
        [Header("Identificação Básica")]
        public string id;
        public string name;
        [TextArea(2, 4)]
        public string lore;
        public EvolutionRank rank = EvolutionRank.Standard;

        [Header("Categorias e Atributos")]
        public FunctionalCategory primaryCategory = FunctionalCategory.System;
        public FunctionalCategory? secondaryCategory = null; // Para tipos duplos (Guardiões / Demônios)
        public ProtocolTrinity protocol = ProtocolTrinity.Firewall;

        [Header("Estatísticas Base do Compêndio")]
        public int hp;
        public int mp;
        public int atk;
        public int def;
        public int intStat; // Ataque Mágico / Inteligência
        public int spi;     // Defesa Mágica / Espírito
        public int spd;     // Velocidade de Ação
        public int crt;     // Taxa Crítica Base (%)
        public int mov = 3; // Orçamento de movimentação no grid

        [Header("Passiva Exclusiva")]
        public string passiveName;
        [TextArea(2, 3)]
        public string passiveDescription;
        public string passiveId;

        [Header("Habilidades")]
        public List<SkillData> skills = new List<SkillData>();

        [Header("Fusão de Algoritmo & Linhagem")]
        public List<string> recipeIngredients = new List<string>();
        public List<string> inheritedSkillNames = new List<string>();

        public bool IsDualType => secondaryCategory.HasValue;

        public AppmonData() { }

        public AppmonData(
            string id,
            string name,
            EvolutionRank rank,
            FunctionalCategory primaryCategory,
            int hp, int mp, int atk, int def, int intStat, int spi, int spd, int crt,
            string passiveName, string passiveDesc, string passiveId,
            string lore = "",
            FunctionalCategory? secondaryCategory = null,
            ProtocolTrinity protocol = ProtocolTrinity.Firewall,
            int mov = 3)
        {
            this.id = id;
            this.name = name;
            this.rank = rank;
            this.primaryCategory = primaryCategory;
            this.secondaryCategory = secondaryCategory;
            this.protocol = protocol;
            this.hp = hp;
            this.mp = mp;
            this.atk = atk;
            this.def = def;
            this.intStat = intStat;
            this.spi = spi;
            this.spd = spd;
            this.crt = crt;
            this.mov = mov;
            this.passiveName = passiveName;
            this.passiveDescription = passiveDesc;
            this.passiveId = passiveId;
            this.lore = lore;
            this.skills = new List<SkillData>();
            this.recipeIngredients = new List<string>();
            this.inheritedSkillNames = new List<string>();
        }

        public string GetTypeDisplayString()
        {
            if (secondaryCategory.HasValue)
            {
                return $"{primaryCategory} / {secondaryCategory.Value}";
            }
            return primaryCategory.ToString();
        }

        public void ApplyToUnit(Unit unit)
        {
            if (unit == null) return;

            unit.unitName = this.name;
            unit.category = this.primaryCategory;
            unit.protocol = this.protocol;
            unit.rank = this.rank;
            unit.skills = new List<SkillData>(this.skills);
            unit.passiveSkill = new PassiveSkillData(this.passiveName, this.passiveDescription);

            if (unit.stats == null)
            {
                unit.stats = unit.GetComponentInChildren<Stats>();
            }

            if (unit.stats != null)
            {
                unit.stats.InitializeStatsIfEmpty();
                unit.stats.SetStat(StatEnum.HP, this.hp);
                unit.stats.SetStat(StatEnum.MaxHp, this.hp);
                unit.stats.SetStat(StatEnum.MP, this.mp);
                unit.stats.SetStat(StatEnum.MaxMp, this.mp);
                unit.stats.SetStat(StatEnum.SP, 100);
                unit.stats.SetStat(StatEnum.MaxSp, 100);
                unit.stats.SetStat(StatEnum.ATK, this.atk);
                unit.stats.SetStat(StatEnum.DEF, this.def);
                unit.stats.SetStat(StatEnum.INT, this.intStat);
                unit.stats.SetStat(StatEnum.SPI, this.spi);
                unit.stats.SetStat(StatEnum.SPEED, this.spd);
                unit.stats.SetStat(StatEnum.SPD, this.spd);
                unit.stats.SetStat(StatEnum.CRT, this.crt);
                unit.stats.SetStat(StatEnum.MOV, this.mov);
            }
        }
    }
}

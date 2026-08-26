using System;
using System.Collections.Generic;
using UnityEngine;

public class Stats : MonoBehaviour
{
    public List<Stat> stats = new List<Stat>();

    void Awake()
    {
        InitializeStatsIfEmpty();
    }

    public void InitializeStatsIfEmpty()
    {
        if (stats == null)
        {
            stats = new List<Stat>();
        }

        Array enumValues = Enum.GetValues(typeof(StatEnum));
        foreach (StatEnum statType in enumValues)
        {
            if (GetStatObj(statType) == null)
            {
                Stat temp = new Stat();
                temp.type = statType;
                temp.value = GetDefaultValueFor(statType);
                stats.Add(temp);
            }
        }
    }

    int GetDefaultValueFor(StatEnum statType)
    {
        switch (statType)
        {
            case StatEnum.HP:
            case StatEnum.MaxHp:
                return 100;
            case StatEnum.SP:
            case StatEnum.MaxSp:
                return 50;
            case StatEnum.MP:
            case StatEnum.MaxMp:
                return 30;
            case StatEnum.ATK:
                return 20;
            case StatEnum.DEF:
                return 10;
            case StatEnum.MATK:
                return 15;
            case StatEnum.MDEF:
                return 10;
            case StatEnum.SPEED:
                return 10;
            case StatEnum.MOV:
                return 3;
            default:
                return 10;
        }
    }

    public Stat GetStatObj(StatEnum type)
    {
        return stats.Find(x => x.type == type);
    }

    public int GetStat(StatEnum type)
    {
        Stat s = GetStatObj(type);
        return s != null ? s.value : 0;
    }

    public void SetStat(StatEnum type, int value)
    {
        Stat s = GetStatObj(type);
        if (s != null)
        {
            s.value = value;
        }
        else
        {
            Stat temp = new Stat { type = type, value = value };
            stats.Add(temp);
        }
    }

    public void ModifyStat(StatEnum type, int amount)
    {
        Stat s = GetStatObj(type);
        if (s != null)
        {
            s.value += amount;
        }
    }
}

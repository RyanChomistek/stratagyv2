using System;
using System.Collections.ObjectModel;
using UnityEngine;

public class CombatResult
{
    public Soldier Attacker;
    public Soldier Defender;
    public float DamageToAttacker;
    public float DamageToDefender;
    public bool AttackSuccess;

    public CombatResult()
    {
    }

    public CombatResult(Soldier attacker, Soldier defender, float damageToAttacker, float damageToDefender, bool attackSuccess)
    {
        Attacker = attacker;
        Defender = defender;
        DamageToAttacker = damageToAttacker;
        DamageToDefender = damageToDefender;
        AttackSuccess = attackSuccess;
    }
}

public enum SoldierType
{
    Officer, Melee, Ranged, Seige
}

[System.Serializable]
public class Soldier : IEquatable<Soldier>
{
    private static int IdCount = 0;
    public int Id = -1;
    public float Speed = 5000;
    public float BaseHitStrength = .1f;
    public float HitStrength { get { return BaseHitStrength * Health; } }
    public float Health = 1;
    public float MinRange = 0f;
    public float MaxRange = 4;
    public float SightDistance = 6;
    public float Supply = 0;
    public float SupplyUsePerSec = .1f;
    public float MaxSupply = 10;
    public float Experience = 0;
    public SoldierType Type = SoldierType.Melee;

    public Soldier(Soldier Other)
    {
        Id = Other.Id;
        Supply = Other.Supply;
        Experience = Other.Experience;
    }

    public Soldier()
    {
        Id = IdCount++;
        Supply = MaxSupply * .5f;
        Experience = UnityEngine.Random.Range(.1f, .25f);
    }
    
    public static float DoDamage(Soldier attacker, Soldier defender)
    {
        var damageDone = Mathf.Min(defender.Health, attacker.HitStrength * GameManager.DeltaTime);
        damageDone *= UnityEngine.Random.Range(0, attacker.Experience);
        attacker.Experience = Mathf.Min(1, attacker.Experience + damageDone);
        defender.Health -= damageDone;

        //to fix floating point errors
        if(defender.Health < .01f)
        {
            defender.Health = -.01f;
        }

        return damageDone;
    }

    public virtual CombatResult Attack(ref ControlledDivision division, float distanceToTarget)
    {
        CombatResult result = new CombatResult();

        if (distanceToTarget < MinRange || distanceToTarget > MaxRange)
        {
            result.AttackSuccess = false;
            return result;
        }

        result.Attacker = this;

        foreach (var defender in division.Soldiers)
        {
            if(defender.Health > 0)
            {
                result.DamageToDefender = DoDamage(this, defender);
                result.Defender = defender;
                result.DamageToAttacker = defender.Defend(this, distanceToTarget);
                break;
            }
        }

        return result;
    }
    
    public virtual float Defend(Soldier attacker, float distanceToTarget)
    {
        if (distanceToTarget < MinRange || distanceToTarget > MaxRange)
        {
            return 0;
        }
        
        var damageDone = DoDamage(this, attacker);
        attacker.Health -= damageDone;
        return damageDone;
    }

    //should be called once per second
    public virtual void UseSupply(Officer officer)
    {
        Supply -= SupplyUsePerSec * officer.SupplyUsage.Value;
    }

    public virtual void GatherSupplies(TerrainMapTile tile)
    {
        tile.Supply -= 1;
        Supply += 1;
        Supply = Mathf.Min(MaxSupply, Supply);
    }

    public override bool Equals(object obj)
    {
        if (obj == null) return false;
        Soldier objAsSoldier = obj as Soldier;
        if (objAsSoldier == null) return false;
        else return Equals(objAsSoldier);
    }

    public bool Equals(Soldier other)
    {
        return this.Id == other.Id;
    }

    public override int GetHashCode()
    {
        return this.Id;
    }

    public override string ToString()
    {
        return Type.ToString() + " E:" + (int)(Experience*100);
    }
}
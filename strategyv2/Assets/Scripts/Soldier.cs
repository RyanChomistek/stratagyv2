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
    Melee, Ranged, Seige
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
    public SoldierType Type = SoldierType.Melee;

    public Soldier()
    {
        Id = IdCount++;
        Supply = MaxSupply * .5f;
    }

    public Soldier(Soldier other)
    {
        this.Speed = other.Speed;
        this.BaseHitStrength = other.BaseHitStrength;
        this.Health = other.Health;
        this.MinRange = other.MinRange;
        this.MaxRange = other.MaxRange;
        this.SightDistance = other.SightDistance;
        this.Type = other.Type;
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
                var damageDone = Mathf.Min(defender.Health, this.HitStrength * GameManager.DeltaTime) ;
                defender.Health -= damageDone;
                result.DamageToDefender = damageDone;
                result.Defender = defender;
                result.DamageToAttacker = defender.Defend(this, distanceToTarget);
                break;
            }
        }

        return result;
    }
    
    public virtual float Defend(Soldier Attacker, float distanceToTarget)
    {
        if (distanceToTarget < MinRange || distanceToTarget > MaxRange)
        {
            return 0;
        }
        
        var damageDone = Mathf.Min(Attacker.Health, this.HitStrength * GameManager.DeltaTime);
        Attacker.Health -= damageDone;
        return damageDone;
    }

    //should be called once per second
    public virtual void UseSupply()
    {
        Supply -= SupplyUsePerSec;
    }

    public virtual void GatherSupplies(MapTerrainTile tile)
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
}
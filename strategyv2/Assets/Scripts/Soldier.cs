using System;
using System.Collections.ObjectModel;
using UnityEngine;

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
    public float HitStrength = .1f;
    public float HitRange = 1;
    public float Health = 1;
    public float MinRange = 0f;
    public float MaxRange = 2;
    public float SightDistance = 6;
    public float Supply = 0;
    public float MaxSupply = 10;
    public SoldierType Type = SoldierType.Melee;

    public Soldier()
    {
        Id = IdCount++;
    }

    public Soldier(Soldier other)
    {
        this.Speed = other.Speed;
        this.HitStrength = other.HitStrength;
        this.Health = other.Health;
        this.MinRange = other.MinRange;
        this.MaxRange = other.MaxRange;
        this.SightDistance = other.SightDistance;
        this.Type = other.Type;
        this.HitRange = other.HitRange;
    }

    //returns damage done
    public virtual float Attack(ref ControlledDivision division)
    {
        foreach(var otherSoldier in division.Soldiers)
        {
            if(otherSoldier.Health > 0)
            {
                var damageDone = Mathf.Min(otherSoldier.Health, this.HitStrength);
                otherSoldier.Health -= this.HitStrength * GameManager.Instance.DeltaTime;
                return damageDone;
            }
        }
        return 0;
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
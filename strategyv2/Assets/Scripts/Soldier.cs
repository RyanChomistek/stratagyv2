public enum SoldierType
{
    Melee, Ranged, Seige
}

[System.Serializable]
public class Soldier
{
    //public int Count = 0;
    public float Speed = 1;
    public float HitStrength = 1;
    public float Range = 1;
    public float SightDistance = 4;
    public SoldierType Type = SoldierType.Melee;

    public Soldier()
    {
    }

    public Soldier(Soldier other)
    {
        //this.Count = other.Count;
        this.Speed = other.Speed;
        this.HitStrength = other.HitStrength;
        this.Range = other.Range;
        this.SightDistance = other.SightDistance;
        this.Type = other.Type;
    }

    public Soldier(float speed, float hitStrength, float range, float sightDistance, SoldierType type)
    {
        //this.Count = count;
        this.Speed = speed;
        this.HitStrength = hitStrength;
        this.Range = range;
        this.SightDistance = sightDistance;
        this.Type = type;
    }
}
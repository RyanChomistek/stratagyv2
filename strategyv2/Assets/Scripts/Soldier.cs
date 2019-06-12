public enum SoldierType
{
    Melee, Ranged, Seige
}

[System.Serializable]
public class Soldier
{
    //public int Count = 0;
    public float Speed = 5000;
    public float HitStrength = 1;
    public float Health = 1;
    public float MinRange = 0f;
    public float MaxRange = 2;
    public float SightDistance = 6;
    public float Supply = 0;
    public float MaxSupply = 10;
    public SoldierType Type = SoldierType.Melee;

    public Soldier()
    {
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
    }
}
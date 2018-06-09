public enum SoldierType
{
    Melee, Ranged, Seige
}

[System.Serializable]
public class Soldier
{
    public int Count = 0;
    public float Speed = 1;
    public float HitStrength = 1;
    public float Range = 1;
    public float SightDistance = 1;
    public SoldierType Type = SoldierType.Melee;

    public Soldier(int count, float speed, float hitStrength, float range, float sightDistance, SoldierType type)
    {
        this.Count = count;
        this.Speed = speed;
        this.HitStrength = hitStrength;
        this.Range = range;
        this.SightDistance = sightDistance;
        this.Type = type;
    }
}
public enum Faction
{
    Player,
    Enemy,
    Ally
}

public static class FactionUtil
{
    public static bool AreHostile(Faction a, Faction b)
    {
        if (a == b) return false;

        // Player and Ally are friendly
        if ((a == Faction.Player && b == Faction.Ally) ||
            (a == Faction.Ally && b == Faction.Player))
            return false;

        return true;
    }
}
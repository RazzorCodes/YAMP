namespace YAMP
{
    public interface IPodContainer
    {
        System.Collections.Generic.List<Verse.Thing> Get();
        Verse.Pawn GetPawn();
        Verse.ThingOwner GetDirectlyHeldThings();
    }
}

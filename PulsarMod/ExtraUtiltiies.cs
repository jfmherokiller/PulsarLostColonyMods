namespace PulsarMod
{
    public class ExtraUtiltiies
    {
        public static bool ScanPawnCheckIfValid(PLPawnBase inPawn)
        {
            var PawnIsNotNull = inPawn != null;

            var PawnDoesNotExistOutSideTeleporter = inPawn.MyCurrentTLI != null;

            var PawnViewedIsNotNull = PLNetworkManager.Instance.ViewedPawn != null;

            var pawnExistsInSameTeleportDesignation =
                inPawn.MyCurrentTLI == PLNetworkManager.Instance.ViewedPawn.MyCurrentTLI;


            var PawnExistsInSameRoom = inPawn.MyInterior == PLNetworkManager.Instance.ViewedPawn.MyInterior;

            var Unknown1 = !inPawn.PreviewPawn;
            
            
            return PawnIsNotNull && PawnDoesNotExistOutSideTeleporter && PawnViewedIsNotNull &&
                   pawnExistsInSameTeleportDesignation && PawnExistsInSameRoom && Unknown1;
        }
    }
}
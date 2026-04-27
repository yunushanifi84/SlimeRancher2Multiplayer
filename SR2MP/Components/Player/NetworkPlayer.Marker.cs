using Il2CppMonomiPark.SlimeRancher.UI;

namespace SR2MP.Components.Player;

public partial class NetworkPlayer
{
    private void SetupMarker()
    {
        // Local players don't need a separate marker
        if (IsLocal)
        {
            return;
        }

        var markerComponent = gameObject.AddComponent<RadarTrackedPointOfInterest>();
        markerComponent.enabled = false;
        markerComponent._worldRadarPrefab = null;
        markerComponent._compassRadarPrefab = markerPrefab;
        markerComponent._isOptional = false;
        markerComponent._overflowMode = RadarCompassOverflowMode.CLAMP;
        markerComponent._ranchBehaviour = RadarEntryRanchHandling.SHOW_IN_RANCH_AS_WELL;
        markerComponent.enabled = true;

        SrLogger.LogMessage($"Remote player marker added: {model!.PlayerId}", SrLogTarget.Both);
    }
}
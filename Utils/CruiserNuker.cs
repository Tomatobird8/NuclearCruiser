using UnityEngine;

namespace NuclearCruiser.Utils;

public class CruiserNuker : MonoBehaviour
{
    internal void Explode()
    {
        GameObject? nukeObject = Instantiate(NuclearCruiser.nukeObject);
        if (nukeObject != null)
        {
            nukeObject.transform.position = gameObject.transform.position;
        }
    }

    private void Start()
    {
        if (NuclearCruiser.nuclearCruiserRadiationWarning && !StartOfRound.Instance.inShipPhase)
        {
            HUDManager.Instance.RadiationWarningHUD();
        }
        if (NuclearCruiser.nuclearCruiserWarning && !StartOfRound.Instance.inShipPhase)
        {
            if (NuclearCruiser.infiniteBoosts)
            {
                HUDManager.Instance.DisplayTip("NUCLEAR CRUISER SPAWNED", "Using nuclear power the cruiser can boost infinitely. Handle with extreme care.", true);
            }
            else
            {
                HUDManager.Instance.DisplayTip("NUCLEAR CRUISER SPAWNED", "Handle with extreme care.", true);
            }
        }
        VehicleController? vc = GetComponent<VehicleController>();
        if (vc == null) return;
        if (NuclearCruiser.infiniteBoosts)
        {
            vc.turboBoosts = 5;
            vc.AddTurboBoost();
        }
        if (vc.vehicleID != 0) return;
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in meshRenderers)
        {
            if (renderer.transform.name.Contains("MainBody") || renderer.transform.name == "CarHoodMesh" || renderer.transform.name == "Door")
            {
                renderer.materials[0].mainTexture = NuclearCruiser.cruiserTexture;
            }
        }
    }
}

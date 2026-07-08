using UnityEngine;

namespace NuclearCruiser.Utils;

public class CruiserNuker : MonoBehaviour
{
    public void Explode()
    {
        GameObject? nukeObject = Instantiate(NuclearCruiser.nukeObject);
        if (nukeObject != null)
        {
            nukeObject.transform.position = gameObject.transform.position;
        }
    }

    public void Start()
    {
        // moved this to the top of Start, for one you have much bigger issues if a VehicleController is not found at this point
        // for two, we don't want to patch custom vehicles at all, aside from the litany of issues this would cause, the popup is only tailored to the Cruiser anyways
        if (!TryGetComponent<VehicleController>(out var vehicleController))
        {
            return;
        }
        if (vehicleController.vehicleID != 0)
        {
            return;        
        }    
        if (NuclearCruiser.nuclearCruiserRadiationWarning && !StartOfRound.Instance.inShipPhase)
        {
            HUDManager.Instance.RadiationWarningHUD();
        }
        if (NuclearCruiser.nuclearCruiserWarning && !StartOfRound.Instance.inShipPhase)
        {
            if (NuclearCruiser.infiniteBoosts)
            {
                HUDManager.Instance.DisplayTip("NUCLEAR CRUISER SPAWNED", "Using nuclear power, the cruiser can boost infinitely. Handle with extreme care.", true);
            }
            else
            {
                HUDManager.Instance.DisplayTip("NUCLEAR CRUISER SPAWNED", "Handle with extreme care.", true);
            }
        }
        if (NuclearCruiser.infiniteBoosts)
        {
            vehicleController.turboBoosts = 5;
            vehicleController.AddTurboBoost();
        }
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i].transform.name.Contains("MainBody") || 
                meshRenderers[i].transform.name == "CarHoodMesh" || 
                meshRenderers[i].transform.name == "Door")
            {
                meshRenderers[i].materials[0].mainTexture = NuclearCruiser.cruiserTexture;
            }
        }
        /*
        foreach (var renderer in meshRenderers)
        {
            if (renderer.transform.name.Contains("MainBody") || renderer.transform.name == "CarHoodMesh" || renderer.transform.name == "Door")
            {
                renderer.materials[0].mainTexture = NuclearCruiser.cruiserTexture;
            }
        }
        */
    }
}

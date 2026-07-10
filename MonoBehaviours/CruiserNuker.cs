using System.Collections;
using UnityEngine;

namespace NuclearCruiser.MonoBehaviours;

public class CruiserNuker : MonoBehaviour
{
    public bool isProtected = true;
    public void Explode()
    {
        GameObject? nukeObject = Instantiate(NuclearCruiser.nukeObject);
        if (nukeObject == null) return;
        nukeObject.transform.position = gameObject.transform.position;
        Vector3 cameraPos = StartOfRound.Instance.activeCamera.transform.position;
        if (Vector3.Distance(nukeObject.transform.position, cameraPos) < 100f * NuclearCruiser.nukeScale)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
        }
        else
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
        }
    }

    public void Start()
    {
        if (!TryGetComponent(out VehicleController vehicleController))
        {
            return;
        }
        if (vehicleController.vehicleID != 0)
        {
            return;        
        }    
        if (NuclearCruiser.infiniteBoosts)
        {
            vehicleController.turboBoosts = 5;
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
        if (vehicleController.destroyedTruckMesh.TryGetComponent(out MeshRenderer destroyedMesh))
        {
            destroyedMesh.materials[0].mainTexture = NuclearCruiser.destroyedCruiserTexture;
        }

        ItemDropship ship = FindAnyObjectByType<ItemDropship>(FindObjectsInactive.Exclude);

        if (NuclearCruiser.protectionTime <= 0) isProtected = false;

        if (ship != null && ship.deliveringVehicle) 
        {
            StartCoroutine(InvulnerabilityCountdown(ship));
            if (NuclearCruiser.nuclearCruiserRadiationWarning)
            {
                HUDManager.Instance.RadiationWarningHUD();
            }
            if (NuclearCruiser.nuclearCruiserWarning)
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
        }
        else
        {
            isProtected = false;
        }
    }
    IEnumerator InvulnerabilityCountdown(ItemDropship ship)
    {
        yield return new WaitUntil(() => ship.untetheredVehicle);
        yield return new WaitForSeconds(NuclearCruiser.protectionTime);
        isProtected = false;
    }
}

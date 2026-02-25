using UnityEngine;

namespace NuclearCruiser.Utils
{
    internal class CruiserNuker : MonoBehaviour
    {
        internal void Explode()
        {
            GameObject? nuke = Instantiate(NuclearCruiser.nukeObject);
            if (nuke != null) 
            {
                nuke.transform.position = gameObject.transform.position;
            }
        }
        
        private void Start()
        {
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in meshRenderers)
            {
                if (renderer.transform.name == "MainBody" || renderer.transform.name == "CarHoodMesh" || renderer.transform.name == "Door" || renderer.transform.name == "MainBody (1)" || renderer.transform.name == "MainBody (2)")
                {
                    renderer.materials[0].mainTexture = NuclearCruiser.cruiserTexture;
                }
            }
            if (NuclearCruiser.nuclearCruiserRadiationWarning)
            {
                HUDManager.Instance.RadiationWarningHUD();
            }
            if (NuclearCruiser.nuclearCruiserWarning)
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
            if (NuclearCruiser.infiniteBoosts)
            {
                GetComponent<VehicleController>().turboBoosts = 5;
                GetComponent<VehicleController>().AddTurboBoost();
            }
        }
    }
}

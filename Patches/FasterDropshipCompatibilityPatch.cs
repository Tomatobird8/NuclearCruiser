using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace NuclearCruiser.Patches;
[HarmonyPatch(typeof(VehicleController))]
public static class FasterDropshipCompatibilityPatch
{
    [HarmonyPatch(nameof(VehicleController.Start))]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Start_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);

        CodeMatch[] pattern =
        {
            new(OpCodes.Call, AccessTools.PropertyGetter(typeof(StartOfRound), nameof(StartOfRound.Instance))),
            new(OpCodes.Ldfld, AccessTools.Field(typeof(StartOfRound), nameof(StartOfRound.inShipPhase)))
        };

        matcher.MatchForward(false, pattern)
            .ThrowIfNotMatch("Couldn't find pattern in VehicleController.Start")
            .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(FasterDropshipCompatibilityPatch), nameof(DropshipDoesntExist)))
            .RemoveInstruction();

        return matcher.InstructionEnumeration();
    }
    
    public static bool DropshipDoesntExist()
    {
        return !Object.FindAnyObjectByType<ItemDropship>();
    }
}

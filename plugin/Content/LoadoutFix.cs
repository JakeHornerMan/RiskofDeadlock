using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Events;

namespace DeadlockOfRain
{
    /// <summary>The character-select loadout panel always adds a skins row and its gamepad navigation indexes into it;
    /// our heroes have one look (no SkinDefs), so their row gets a single "Default" entry instead of staying empty.</summary>
    [HarmonyPatch]
    static class LoadoutSkinRowPatch
    {
        static Type rowType = AccessTools.Inner(typeof(LoadoutPanelController), "Row");

        static MethodBase TargetMethod() => AccessTools.Method(rowType, "FromSkin");

        static void Postfix(object __result, LoadoutPanelController owner, BodyIndex bodyIndex)
        {
            if (__result == null) return;
            var data = (IList)AccessTools.Field(rowType, "rowData").GetValue(__result);
            if (data == null || data.Count > 0) return;
            var body = BodyCatalog.GetBodyPrefabBodyComponent(bodyIndex);
            var hero = body ? body.GetComponent<DeadlockHero>() : null;
            if (!hero) return;
            var row = Sheet.Hero(hero.HeroId);
            var icon = DeadlockCache.Sprite(row.ImageSmall);
            Survivors.Tokens["DLR_SKIN_DEFAULT"] = "Default";
            AccessTools.Method(rowType, "AddButton").Invoke(__result, new object[]
            {
                owner, icon, "DLR_SKIN_DEFAULT", "", Sheet.Color(row.PrimaryColor), (UnityAction)(() => { }), "", null, false, 0,
            });
        }
    }
}

using System;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;

namespace VenomTweaks
{
    // -----------------------------------------------------------
    // 1. Mod Settings Data
    // -----------------------------------------------------------
    public class VenomSettings : ModSettings
    {
        public float buildupMultiplier = 1.0f;
        public float recoveryMultiplier = 1.0f;

        public bool enableBloodTweaks = true;
        public float transfusionReductionMin = 0.20f;
        public float transfusionReductionMax = 0.60f;

        public float tendQualityForFullSuppression = 1.0f;
        public float tendSuppressionOffset = 0.0f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref buildupMultiplier, "buildupMultiplier", 1.0f);
            Scribe_Values.Look(ref recoveryMultiplier, "recoveryMultiplier", 1.0f);

            Scribe_Values.Look(ref enableBloodTweaks, "enableBloodTweaks", true);
            Scribe_Values.Look(ref transfusionReductionMin, "transfusionReductionMin", 0.20f);
            Scribe_Values.Look(ref transfusionReductionMax, "transfusionReductionMax", 0.60f);

            Scribe_Values.Look(ref tendQualityForFullSuppression, "tendQualityForFullSuppression", 1.0f);
            Scribe_Values.Look(ref tendSuppressionOffset, "tendSuppressionOffset", 0.0f);
        }
    }

    // -----------------------------------------------------------
    // 2. Main Mod Class & UI
    // -----------------------------------------------------------
    public class VenomMod : Mod
    {
        public static VenomSettings settings;

        public VenomMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<VenomSettings>();
            var harmony = new Harmony("com.aonova.venomtweaks");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            // Base Venom Tweaks - Explicitly cast the label to (string) to resolve ambiguity
            listing.Label((string)$"Buildup rate mult: {settings.buildupMultiplier:F2}x", -1f, (TipSignal)"Multiplies the rate at which venom severity increases from active wounds (both tended and untended). Note that wounds will only tick up venom for the first 4-7 hours since inflicted (this is defined by CE).\n\nDefault: 1.0 (matches CE default of ~3%/hour per point of damage for normal human pawns)");
            settings.buildupMultiplier = listing.Slider(settings.buildupMultiplier, 0.1f, 5.0f);

            listing.Label((string)$"Recovery rate mult: {settings.recoveryMultiplier:F2}x", -1f, (TipSignal)"Multiplies the natural recovery rate of venom buildup.\n\nDefault: 1.0 (matches CE default of 8%/day).");
            settings.recoveryMultiplier = listing.Slider(settings.recoveryMultiplier, 0.1f, 5.0f);

            listing.Gap(32);

            listing.Label((string)$"Tend quality target for full negation: {settings.tendQualityForFullSuppression * 100f:F0}%", -1f, (TipSignal)"The tend quality target that will completely negate venom gain from a wound.\n\nReducing this will let pawns survive with less skilled doctors or lower tech meds\n\nDefault: 100% (matches CE default)");
            settings.tendQualityForFullSuppression = listing.Slider(settings.tendQualityForFullSuppression, 0.01f, 2.00f);

            listing.Label((string)$"Base suppression on any tend: {settings.tendSuppressionOffset * 100f:F0}%", -1f, (TipSignal)"The guaranteed percentage to reduce venom buildup rate simply by having the wound tended at all, even at 0% quality.\n\nWill smoothly interpolate from this value to 100% suppression at your full negation quality target.\n\nDefault: 0% (matches CE default - 0% tends having no effect on venom)");
            settings.tendSuppressionOffset = listing.Slider(settings.tendSuppressionOffset, 0.0f, 0.90f);

            listing.Gap(32);
            listing.GapLine(16);

            // Blood Operation Tweaks Toggle
            listing.CheckboxLabeled("Enable Blood Operation Tweaks", ref settings.enableBloodTweaks, "If enabled, blood transfusions will reduce a random amount of venom buildup, and hemogen extractions will remove blood without producing a pack for envenomed pawns.\n\nThis allows a strategy to counteract venom buildup by actively flushing with hemogen extractions and transfusions, assuming you have the hemogen packs and first aid doctors available.\n\nTime to add hemogen packs to your medic loadouts!");

            // Only draw the sliders if the master toggle is checked
            if (settings.enableBloodTweaks)
            {
                listing.Gap(16);

                listing.Label((string)$"Min reduction for full Transfusion: {settings.transfusionReductionMin * 100f:F0}%", -1f, (TipSignal)"The MIN range to randomly reduce venom buildup (100% blood transfusion).\n\nIn practice, transfusions will have proportionally reduced effect according to how much blood was actually replaced.\n\nDefault: 20%");
                settings.transfusionReductionMin = listing.Slider(settings.transfusionReductionMin, 0.0f, 1.0f);

                listing.Label((string)$"Max reduction for full Transfusion: {settings.transfusionReductionMax * 100f:F0}%", -1f, (TipSignal)"The MAX range to randomly reduce venom buildup (100% blood transfusion).\n\nIn practice, transfusions will have proportionally reduced effect according to how much blood was actually replaced.\n\nDefault: 60%");
                settings.transfusionReductionMax = listing.Slider(settings.transfusionReductionMax, 0.0f, 1.0f);

                // Prevent min from passing max
                if (settings.transfusionReductionMin > settings.transfusionReductionMax)
                {
                    settings.transfusionReductionMin = settings.transfusionReductionMax;
                }
            }

            // -----------------------------------------------------------
            // RESTORE DEFAULTS BUTTON
            // -----------------------------------------------------------
            listing.GapLine();
            if (listing.ButtonText("Restore Defaults", "Resets all settings to their original values."))
            {
                settings.buildupMultiplier = 1.0f;
                settings.recoveryMultiplier = 1.0f;
                settings.enableBloodTweaks = true;
                settings.transfusionReductionMin = 0.20f;
                settings.transfusionReductionMax = 0.60f;
                settings.tendQualityForFullSuppression = 1.0f;
                settings.tendSuppressionOffset = 0.0f;

                // Immediately apply settings
                DefPatcher.ApplyDynamicSettings();
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "CE Venom Tweaks";

        public override void WriteSettings()
        {
            base.WriteSettings();
            DefPatcher.ApplyDynamicSettings();
        }
    }

    // -----------------------------------------------------------
    // 3. XML Def Modification (Dynamic Settings & Strings)
    // -----------------------------------------------------------
    [StaticConstructorOnStartup]
    public static class DefPatcher
    {
        public static float originalCERecoveryRate = 0f;
        public static bool isRateCached = false;

        // Add a string to cache the original CE description
        public static string originalDescription = null;

        static DefPatcher()
        {
            ApplyDynamicSettings();
        }

        public static void ApplyDynamicSettings()
        {
            HediffDef venomDef = DefDatabase<HediffDef>.GetNamedSilentFail("VenomBuildup");

            if (venomDef != null)
            {
                // --- 1. Modify Description ---
                if (originalDescription == null)
                {
                    // Cache the original XML text safely
                    originalDescription = venomDef.description ?? "";
                }

                if (VenomMod.settings.enableBloodTweaks)
                {
                    // Append the notice if toggled on
                    venomDef.description = originalDescription + "\n\nNote: Receiving a blood transfusions will actively flush some venom from the bloodstream, reducing total buildup.";
                }
                else
                {
                    // Revert to pure CE vanilla if toggled off
                    venomDef.description = originalDescription;
                }

                // --- 2. Modify Recovery Rate ---
                if (venomDef.comps != null)
                {
                    var comp = venomDef.comps.Find(c => c is HediffCompProperties_Immunizable) as HediffCompProperties_Immunizable;
                    if (comp != null)
                    {
                        if (!isRateCached)
                        {
                            originalCERecoveryRate = comp.severityPerDayNotImmune;
                            isRateCached = true;
                        }
                        comp.severityPerDayNotImmune = originalCERecoveryRate * VenomMod.settings.recoveryMultiplier;
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------
    // 4. Harmony Patch: Venom Buildup Multiplier
    // -----------------------------------------------------------
    [HarmonyPatch(typeof(Hediff), nameof(Hediff.Severity), MethodType.Setter)]
    public static class Patch_Hediff_Severity
    {
        public static void Prefix(Hediff __instance, ref float value)
        {
            if (__instance.def != null && __instance.def.defName == "VenomBuildup" && value > __instance.Severity)
            {
                float difference = value - __instance.Severity;
                value = __instance.Severity + (difference * VenomMod.settings.buildupMultiplier);
            }
        }
    }

    // -----------------------------------------------------------
    // 5. Harmony Patch: Blood Transfusion (Dynamic Scaling)
    // -----------------------------------------------------------
    [HarmonyPatch(typeof(Recipe_BloodTransfusion), nameof(Recipe_BloodTransfusion.ApplyOnPawn))]
    public static class Patch_Recipe_BloodTransfusion
    {
        public static void Prefix(Pawn pawn, out float __state)
        {
            // Silently abort the patch logic if the toggle is disabled
            if (!VenomMod.settings.enableBloodTweaks)
            {
                __state = 0f;
                return;
            }

            Hediff bloodLoss = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            __state = bloodLoss != null ? bloodLoss.Severity : 0f;
        }

        public static void Postfix(Pawn pawn, float __state)
        {
            // Abort postfix entirely if toggle is disabled
            if (!VenomMod.settings.enableBloodTweaks) return;

            Hediff bloodLoss = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BloodLoss);
            float newSeverity = bloodLoss != null ? bloodLoss.Severity : 0f;

            float recoveredFraction = Mathf.Max(0f, __state - newSeverity);

            if (recoveredFraction > 0f)
            {
                HediffDef venomDef = DefDatabase<HediffDef>.GetNamedSilentFail("VenomBuildup");
                if (venomDef == null) return;

                Hediff venom = pawn.health.hediffSet.GetFirstHediffOfDef(venomDef);
                if (venom != null && venom.Severity > 0f)
                {
                    float baseReduction = Rand.Range(VenomMod.settings.transfusionReductionMin, VenomMod.settings.transfusionReductionMax);
                    float finalReduction = baseReduction * recoveredFraction;

                    venom.Severity -= finalReduction;

                    if (PawnUtility.ShouldSendNotificationAbout(pawn))
                    {
                        if (venom.Severity <= 0f || !pawn.health.hediffSet.HasHediff(venomDef))
                        {
                            Messages.Message($"Blood transfusion on {pawn.NameShortColored} has cleared their current venom buildup.", pawn, MessageTypeDefOf.PositiveEvent);
                        }
                        else
                        {
                            int reductionPercent = Mathf.RoundToInt(finalReduction * 100f);

                            Messages.Message($"Blood transfusion on {pawn.NameShortColored} reduced venom buildup by {reductionPercent}%.", pawn, MessageTypeDefOf.PositiveEvent);
                        }
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------
    // 6. Harmony Patch: Extract Hemogen Pack Fail State
    // -----------------------------------------------------------
    [HarmonyPatch(typeof(Recipe_ExtractHemogen), nameof(Recipe_ExtractHemogen.ApplyOnPawn))]
    public static class Patch_Recipe_ExtractHemogen
    {
        public static bool Prefix(Pawn pawn, Pawn billDoer)
        {
            // Silently permit standard vanilla extraction if the feature is disabled
            if (!VenomMod.settings.enableBloodTweaks) return true;

            HediffDef venomDef = DefDatabase<HediffDef>.GetNamedSilentFail("VenomBuildup");
            if (venomDef == null) return true;

            Hediff venom = pawn.health.hediffSet.GetFirstHediffOfDef(venomDef);
            if (venom != null && venom.Severity > 0f)
            {
                if (billDoer != null)
                {
                    TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
                }

                Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.BloodLoss, pawn);
                hediff.Severity = 0.449f;
                pawn.health.AddHediff(hediff);

                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message($"Hemogen extracted from {pawn.NameShortColored} was ruined due to venom contamination.", pawn, MessageTypeDefOf.NeutralEvent);
                }

                return false;
            }

            return true;
        }
    }

    // -----------------------------------------------------------
    // 7. Harmony Patch: Venom Tend Quality Suppression
    // -----------------------------------------------------------
    [HarmonyPatch("CombatExtended.HediffComp_Venom", "CompTended")]
    public static class Patch_HediffComp_Venom_CompTended
    {
        public static void Prefix(object __instance, out float __state)
        {
            __state = Traverse.Create(__instance).Field("_venomPerTick").GetValue<float>();
        }

        public static void Postfix(object __instance, float quality, float __state)
        {
            float targetQ = VenomMod.settings.tendQualityForFullSuppression;
            float offset = VenomMod.settings.tendSuppressionOffset;
            float customMultiplier;

            if (targetQ <= 0f)
            {
                customMultiplier = 0f;
            }
            else
            {
                // Calculate how close the quality is to the target (0.0 to 1.0)
                float progress = Mathf.Clamp01(quality / targetQ);

                // Establish our new worst-case scenario multiplier (e.g. 0.9 offset = 0.1 max multiplier)
                float maxMultiplier = 1f - offset;

                // Interpolate from our max multiplier down to 0 based on progress
                customMultiplier = maxMultiplier * (1f - progress);
            }

            // Apply our custom multiplier to the pristine pre-tended value
            float newVenomPerTick = __state * customMultiplier;

            // Overwrite CE's private field with our calculated value
            Traverse.Create(__instance).Field("_venomPerTick").SetValue(newVenomPerTick);
        }
    }

}
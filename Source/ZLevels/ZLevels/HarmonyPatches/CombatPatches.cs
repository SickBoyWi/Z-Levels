﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ZLevels
{
    [HarmonyPatch(typeof(AttackTargetFinder), "BestAttackTarget")]
    public static class CombatPatches_BestAttackTarget_Patch
    {

        public static bool recursiveTrap = false;
        public static bool Prefix(ref IAttackTarget __result, List<IAttackTarget> ___tmpTargets, List<Pair<IAttackTarget, float>> ___availableShootingTargets,
                List<float> ___tmpTargetScores, List<bool> ___tmpCanShootAtTarget, List<IntVec3> ___tempDestList, List<IntVec3> ___tempSourceList,
                IAttackTargetSearcher searcher, TargetScanFlags flags, Predicate<Thing> validator = null, float minDist = 0f,
                float maxDist = 9999f, IntVec3 locus = default(IntVec3), float maxTravelRadiusFromLocus = 3.40282347E+38f, bool canBash = false,
                bool canTakeTargetsCloserThanEffectiveMinRange = true)
        {
            bool result = true;
            if (!recursiveTrap)
            {
                recursiveTrap = true;
                Map oldMap = searcher.Thing.Map;
                IntVec3 oldPosition = searcher.Thing.Position;
                foreach (var map in ZUtils.GetAllMapsInClosestOrder(searcher.Thing, oldMap, oldPosition))
                {
                    if (ZUtils.ZTracker.GetZIndexFor(map) < ZUtils.ZTracker.GetZIndexFor(oldMap))
                    {
                        CanBeSeenOverFast_Patch.returnTrue = true;
                    }
                    var target = AttackTargetFinder.BestAttackTarget(searcher, flags, validator, minDist,
                            maxDist, locus, maxTravelRadiusFromLocus, canBash, canTakeTargetsCloserThanEffectiveMinRange);
                    //Log.Message(searcher.Thing + " - 1: " + ZUtils.ZTracker.GetMapInfo(searcher.Thing.Map) + " - result: " + target);
                    if (target != null)
                    {
                        __result = target;
                        result = false;
                        break;
                    }
                }
                ZUtils.TeleportThing(searcher.Thing, oldMap, oldPosition);
                recursiveTrap = false;
                CanBeSeenOverFast_Patch.returnTrue = false;
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(GenGrid), "CanBeSeenOverFast")]
    public static class CanBeSeenOverFast_Patch
    {
        public static bool returnTrue = false;
        public static bool Prefix(IntVec3 c, Map map, ref bool __result)
        {
            if (returnTrue)
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
    public static class TryCastShot_Patch
    {
        public static Map oldMap1;
        public static Map oldMap2;

        public static bool teleportBack;
        public static void Prefix(Verb_LaunchProjectile __instance, List<IntVec3> ___tempLeanShootSources, List<IntVec3> ___tempDestList, LocalTargetInfo ___currentTarget, ref bool __result)
        {
            if (__instance.caster.Map != ___currentTarget.Thing?.Map && __instance.caster.Map.Tile == ___currentTarget.Thing?.Map.Tile)
            {
                var ind1 = ZUtils.ZTracker.GetZIndexFor(__instance.caster.Map);
                var ind2 = ZUtils.ZTracker.GetZIndexFor(___currentTarget.Thing.Map);
                if (ind1 > ind2)
                {
                    teleportBack = true;
                    oldMap1 = ___currentTarget.Thing.Map;
                    ZUtils.TeleportThing(___currentTarget.Thing, __instance.caster.Map, ___currentTarget.Thing.Position);
                }
                else if (ind1 < ind2)
                {
                    teleportBack = true;
                    oldMap2 = __instance.caster.Map;
                    ZUtils.TeleportThing(__instance.caster, ___currentTarget.Thing.Map, __instance.caster.Position);
                }
            }
        }
        public static void Postfix(Verb_LaunchProjectile __instance, List<IntVec3> ___tempLeanShootSources, List<IntVec3> ___tempDestList, LocalTargetInfo ___currentTarget, ref bool __result)
        {
            if (teleportBack)
            {
                if (oldMap1 != null)
                {
                    ZUtils.TeleportThing(___currentTarget.Thing, oldMap1, ___currentTarget.Thing.Position);
                    oldMap1 = null;
                }
                else if (oldMap2 != null)
                {
                    ZUtils.TeleportThing(__instance.caster, oldMap2, __instance.caster.Position);
                    oldMap2 = null;
                }
            }
        }
    }


    [HarmonyPatch(typeof(Verb), "TryFindShootLineFromTo")]
    public static class TryFindShootLineFromTo_Patch
    {
        public static Map oldMap1;
        public static Map oldMap2;

        public static bool teleportBack;
        public static void Prefix(Verb __instance, List<IntVec3> ___tempLeanShootSources, List<IntVec3> ___tempDestList, IntVec3 root, LocalTargetInfo targ, ref ShootLine resultingLine, ref bool __result)
        {
            if (__instance.caster?.Map != targ.Thing?.Map && __instance.caster?.Map?.Tile == targ.Thing?.Map?.Tile)
            {
                var ind1 = ZUtils.ZTracker.GetZIndexFor(__instance.caster.Map);
                var ind2 = ZUtils.ZTracker.GetZIndexFor(targ.Thing.Map);
                if (ind1 > ind2)
                {
                    teleportBack = true;
                    oldMap1 = targ.Thing.Map;
                    ZUtils.TeleportThing(targ.Thing, __instance.caster.Map, targ.Thing.Position);
                }
                else if (ind1 < ind2)
                {
                    teleportBack = true;
                    oldMap2 = __instance.caster.Map;
                    ZUtils.TeleportThing(__instance.caster, targ.Thing.Map, __instance.caster.Position);
                }
            }
        }
        public static void Postfix(Verb __instance, List<IntVec3> ___tempLeanShootSources, List<IntVec3> ___tempDestList, IntVec3 root, LocalTargetInfo targ, ref ShootLine resultingLine, ref bool __result)
        {
            if (teleportBack)
            {
                if (oldMap1 != null)
                {
                    ZUtils.TeleportThing(targ.Thing, oldMap1, targ.Thing.Position);
                    oldMap1 = null;
                }
                else if (oldMap2 != null)
                {
                    ZUtils.TeleportThing(__instance.caster, oldMap2, __instance.caster.Position);
                    oldMap2 = null;
                }
                teleportBack = false;
                var ind1 = ZUtils.ZTracker.GetZIndexFor(__instance.caster.Map);
                var ind2 = ZUtils.ZTracker.GetZIndexFor(targ.Thing.Map);
                if (ind1 > ind2 && !IsVoidsEverywhereInShootingLine(resultingLine, __instance.caster.Map))
                {
                    //Log.Message(__instance.caster + " shouldnt shoot 1", true);
                    __result = false;
                }
                else if (ind1 < ind2 && !IsVoidsEverywhereInShootingLineInBackWard(resultingLine, targ.Thing.Map))
                {
                    //Log.Message(__instance.caster + " shouldnt shoot 2", true);
                    __result = false;
                }
            }
        }

        public static bool IsVoidsEverywhereInShootingLine(ShootLine resultingLine, Map map)
        {
            var points = resultingLine.Points().ToList();
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i] != resultingLine.Source)
                {
                    if (i > 1 && points[i].GetTerrain(map) != ZLevelsDefOf.ZL_OutsideTerrain)
                    {
                        //Log.Message(i + " - " + points[i] + " - " + points[i].GetTerrain(map) + " - " + map, true);
                        return false;
                    }
                    else if (i == 1 && points[i].GetCover(map)?.def?.Fillage == FillCategory.Full)
                    {
                        //Log.Message(i + " - " + points[i] + " - " + points[i].GetCover(map) + " - " + map, true);
                        return false;
                    }
                }
            }
            return true;
        }

        public static bool IsVoidsEverywhereInShootingLineInBackWard(ShootLine resultingLine, Map map)
        {
            var points = resultingLine.Points().ToList();
            for (int i = points.Count - 1; i >= 0; i--)
            {
                if (points[i] != resultingLine.Dest)
                {
                    //Log.Message("points[i].GetTerrain(map): " + points[i] + " - " + points[i].GetTerrain(map), true);
                    if (i < points.Count - 1 && points[i].GetTerrain(map) != ZLevelsDefOf.ZL_OutsideTerrain)
                    {
                        //Log.Message(i + " - " + points.Count + " - " + points[i] + " - " + points[i].GetTerrain(map) + " - " + map, true);
                        return false;
                    }
                    else if (i == points.Count - 1 && points[i].GetCover(map)?.def?.Fillage == FillCategory.Full)
                    {
                        //Log.Message(i + " - " + points.Count + " - " + points[i] + " - " + points[i].GetCover(map) + " - " + map, true);
                        return false;
                    }
                }
            }
            return true;
        }
    }

    //[HarmonyPatch(typeof(JobGiver_ConfigurableHostilityResponse), "TryGiveJob")]
    //public static class TryGiveJob_Patch
    //{
    //    public static void Postfix(Pawn pawn, ref Job __result)
    //    {
    //        Log.Message(pawn + " got response: " + __result);
    //    }
    //}


    //[HarmonyPatch(typeof(JobGiver_AIFightEnemies), "TryGiveJob")]
    //public class JobGiver_AIFightEnemies_Patch
    //{
    //      private static void Postfix(ref JobGiver_AIFightEnemies __instance, ref Job __result, ref Pawn pawn)
    //      {
    //
    //              Log.Message(pawn + " - 3 TEST: " + __result);
    //      }
    //}

    //[HarmonyPatch(typeof(JobGiver_AIDefendPoint), "TryGiveJob")]
    //public class JobGiver_AIDefendPoint_Patch
    //{
    //      private static void Postfix(ref JobGiver_AIDefendPoint __instance, ref Job __result, ref Pawn pawn)
    //      {
    //
    //              Log.Message(pawn + " - 3 TEST: " + __result);
    //      }
    //}

    //[HarmonyPatch(typeof(JobGiver_AIDefendPawn), "TryGiveJob")]
    //public class JobGiver_AIDefendPawn_Patch
    //{
    //    private static void Postfix(ref JobGiver_AIDefendPawn __instance, ref Job __result, ref Pawn pawn)
    //    {
    //
    //        Log.Message(pawn + " - 3 TEST: " + __result);
    //    }
    //}

    //[HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    //public class JobGiver_AIFightEnemy_Patch
    //{
    //    public static bool recursiveTrap = false;
    //    private static bool Prefix(ref JobGiver_AIFightEnemy __instance, ref Job __result, ref Pawn pawn)
    //    {
    //        bool result = true;
    //        if (!recursiveTrap)
    //        {
    //            recursiveTrap = true;
    //            Map oldMap = pawn.Map;
    //            IntVec3 oldPosition = pawn.Position;
    //            foreach (var map in ZUtils.GetAllMapsInClosestOrder(pawn, oldMap, oldPosition))
    //            {
    //                var job = Traverse.Create(__instance).Method("TryGiveJob", new object[]
    //                                {
    //                                                                    pawn
    //                                }).GetValue<Job>();
    //
    //                Log.Message("4: " + ZUtils.ZTracker.GetMapInfo(pawn.Map) + " - result: " + job);
    //                if (job != null)
    //                {
    //                    __result = job;
    //                    result = false;
    //                    break;
    //                }
    //            }
    //            ZUtils.TeleportThing(pawn, oldMap, oldPosition);
    //            recursiveTrap = false;
    //        }
    //        Log.Message(pawn + " - 4 TEST: " + __result);
    //        return result;
    //    }
    //}
}
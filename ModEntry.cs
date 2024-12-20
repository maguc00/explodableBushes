using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

using StardewValley.TerrainFeatures; // Namespace for accessing LargeTerrainFeature and Bush class
using HarmonyLib;

namespace explodableBushes
{

    internal class ObjectPatches
    {
        private static IMonitor Monitor;

        // call this method from your Entry class
        internal static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        // patches need to be static!
        internal static bool Explode_Prefix(GameLocation __instance, Vector2 tileLocation, int radius, Farmer who, bool damageFarmers = true, int damage_amount = -1, bool destroyObjects = true)
        {
            try
            {
                /* 
                   if explode gets called with "destroyObjects = false" then immediately return back to the original logic
                   since theres no point in doing stuff here if stuff shouldn't get destroyed.
                */
                if (!destroyObjects)
                    return true;

                int destroyRadius = radius - 1;

                /* go through every tile defined by destroyRadius to check if a bush is present */
                for (int offsetX = -destroyRadius; offsetX <= destroyRadius; offsetX++)
                {
                    for (int offsetY = -destroyRadius; offsetY <= destroyRadius; offsetY++)
                    {

                        Vector2 offset = new Vector2((int)tileLocation.X + offsetX, (int)tileLocation.Y + offsetY);

                        // try to get our TerrainFeature directly as a Bush class, in order to call the Bush method "shake" directly later on
                        // if there is no LargeTerrainFeature on that tile, then it will return "null" and it will be skipped with an if check.
                        Bush? bush = __instance.getLargeTerrainFeatureAt((int)offset.X, (int)offset.Y) as Bush;

                        if (bush != null)
                        {

                            bush.shake(offset, true); // should harvest berries and trigger Krobus (if Krobus's bush wasn't already destroyed..)
                            __instance.largeTerrainFeatures.Remove(bush); // deletes bush from game world

                            Item item = ItemRegistry.Create("(O)771", 50); // Fiber
                            Game1.createItemDebris(item, offset * 64, 0, __instance);
                            Item item2 = ItemRegistry.Create("(O)388", 10); // Wood
                            Game1.createItemDebris(item2, offset * 64, 0, __instance);

                            // ignore that
                            //Item item3 = ItemRegistry.Create("(O)684", 10);
                            //Game1.createItemDebris(item3, __instance.getCharacterFromName("Linus").Position, 0, __instance);
                            //__instance.characters.Remove(__instance.getCharacterFromName("Linus"));

                            __instance.localSound("cut", offset);
                        }
                    }
                }

                return true; // back to original explode logic to do the other explosion related stuff
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(Explode_Prefix)}:\n{ex}", LogLevel.Error);
                return true; // run original logic
            }
        }
    }

    // https://stardewvalleywiki.com/Modding:Modder_Guide/Get_Started
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {

            // https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Harmony
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(StardewValley.GameLocation), nameof(StardewValley.GameLocation.explode)),
                prefix: new HarmonyMethod(typeof(ObjectPatches), nameof(ObjectPatches.Explode_Prefix))
            );
        }
    }
}
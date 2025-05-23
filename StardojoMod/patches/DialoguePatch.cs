using System;
using StardewValley.Menus;
using HarmonyLib;
using StardewValley.Locations;
using StardewValley;

namespace ActionSpace.patches
{
    [HarmonyPatch(typeof(DialogueBox))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(string), typeof(Response[]), typeof(int) })]
    public static class DialoguePatch
    {

        public static void Prefix(string dialogue, Response[] responses, int width)
        {
            System.Console.Out.Write($"{Environment.StackTrace.ToString()}");

        }
    }

    [HarmonyPatch(typeof(DialogueBox))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(Dialogue) })]
    public static class DialoguePatch2
    {

        public static void Postfix()
        {
            System.Console.Out.Write($"{Environment.StackTrace.ToString()}");
        }
    }

    [HarmonyPatch(typeof(DialogueBox))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(List<string>) })]
    public static class DialoguePatch3
    {

        public static void Postfix()
        {
            System.Console.Out.Write($"{Environment.StackTrace.ToString()}");
        }
    }
}


using System;
using StardewValley.Menus;
using HarmonyLib;
namespace ActionSpace.patches
{
	[HarmonyPatch(typeof(ShopMenu))]
	[HarmonyPatch("Initialize")]
	public static class ShopMenuPatch
	{

		public static void Postfix()
		{
			System.Console.Out.Write($"{Environment.StackTrace.ToString()}");
		}
	}
}


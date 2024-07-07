using System;
using System.Reflection;
using System.Linq;
using EFT;
using JsonType;
using SPT.Reflection.Utils;
using UnityEngine;

namespace AmandsSense
{
    public class AmandsSenseHelper
    {
        private static Type LocalizedType;
        private static MethodInfo LocalizedMethod;

        private static Type RoleType;
        private static MethodInfo GetScavRoleKeyMethod;
        private static MethodInfo IsFollowerMethod;
        private static MethodInfo CountAsBossForStatisticsMethod;
        private static MethodInfo IsBossMethod;

        private static Type TransliterateType;
        private static MethodInfo TransliterateMethod;

        public static bool UsesFSR2UpscalerMethodFound;
        private static MethodInfo UsesFSR2UpscalerMethod;

        private static Type ToColorType;
        private static MethodInfo ToColorMethod;

        public static void Init()
        {
            LocalizedType = PatchConstants.EftTypes.Single((Type x) => x.GetMethod("ParseLocalization", BindingFlags.Static | BindingFlags.Public) != null);
            LocalizedMethod = LocalizedType.GetMethods().First((MethodInfo x) => x.Name == "Localized" && x.GetParameters().Length == 2 && x.GetParameters()[0].ParameterType == typeof(string) && x.GetParameters()[1].ParameterType == typeof(EStringCase));

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            RoleType = PatchConstants.EftTypes.Single((Type x) => x.GetMethod("IsBoss", flags) != null && x.GetMethod("Init", flags) != null);
            IsBossMethod = RoleType.GetMethod("IsBoss", flags);
            IsFollowerMethod = RoleType.GetMethod("IsFollower", flags);
            CountAsBossForStatisticsMethod = RoleType.GetMethod("CountAsBossForStatistics", flags);
            GetScavRoleKeyMethod = RoleType.GetMethod("GetScavRoleKey", flags);

            TransliterateType = PatchConstants.EftTypes.Single(x => x.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Any(t => t.Name == "Transliterate"));
            TransliterateMethod = TransliterateType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Single(x => x.Name == "Transliterate" && x.GetParameters().Length == 1);
            UsesFSR2UpscalerMethod = typeof(SSAA).GetMethod("UsesFSR2Upscaler", BindingFlags.Public | BindingFlags.Instance);
            UsesFSR2UpscalerMethodFound = UsesFSR2UpscalerMethod != null;

            ToColorType = PatchConstants.EftTypes.Single((Type x) => x.GetMethod("ToColor", BindingFlags.Static | BindingFlags.Public) != null);
            ToColorMethod = ToColorType.GetMethods().First((MethodInfo x) => x.Name == "ToColor");

        }
        public static string Localized(string id, EStringCase @case)
        {
            return (string)LocalizedMethod.Invoke(null, new object[]
            {
                id,
                @case
            });
        }
        public static bool IsBoss(WildSpawnType role)
        {
            return (bool)IsBossMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static bool IsFollower(WildSpawnType role)
        {
            return (bool)IsFollowerMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static bool CountAsBossForStatistics(WildSpawnType role)
        {
            return (bool)CountAsBossForStatisticsMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static string GetScavRoleKey(WildSpawnType role)
        {
            return (string)GetScavRoleKeyMethod.Invoke(null, new object[]
            {
                role
            });
        }
        public static string Transliterate(string text)
        {
            return (string)TransliterateMethod.Invoke(null, new object[]
            {
                text
            });
        }
        public static bool UsesFSR2Upscaler()
        {
            return UsesFSR2UpscalerMethodFound ? (bool)UsesFSR2UpscalerMethod.Invoke(null, new object[]
            {
            }) : false;
        }
        public static Color ToColor(TaxonomyColor taxonomyColor)
        {
            return (Color)ToColorMethod.Invoke(null, new object[]
            {
                taxonomyColor
            });
        }
    }
}

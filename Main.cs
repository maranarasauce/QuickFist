using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace QuickFist
{
    [UKPlugin("maranara_quick_fist", "QuickFist", "0.2.0", "Binds Feedbacker to the punch keybind and KnuckleBlaster to the change fist keybind", true, true)]
    public class QuickFist : UKMod
    {
        private static Harmony harmony;
        private void OnEnable()
        {
            harmony = new Harmony("maranara_quick_fist");
            harmony.PatchAll(typeof(QuickFist));
        }

        static Punch redPunch;
        static Punch bluePunch;
        [HarmonyPatch(typeof(FistControl), "ResetFists")]
        [HarmonyPostfix]
        static void ArmStart(FistControl __instance)
        {
            redPunch = __instance.redArm.GetComponent<Punch>();
            bluePunch = __instance.blueArm.GetComponent<Punch>();
            __instance.redArm.SetActive(true);
            __instance.blueArm.SetActive(true);
            __instance.ForceArm(0);
            __instance.RefreshArm();
        }

        [HarmonyPatch(typeof(FistControl), "ArmChange")]
        [HarmonyPrefix]
        static bool ArmChange(FistControl __instance, int orderNum)
        {
            Debug.Log(orderNum);
            if (orderNum == 1)
            {
                __instance.redArm.SetActive(true);
                __instance.blueArm.SetActive(true);
                bluePunch.ready = true;
                redPunch.ready = true;
            }
            return false;
        }


        [HarmonyPatch(typeof(Punch), "Start")]
        [HarmonyPostfix]
        static void ArmPunch(Punch __instance)
        {
            if (__instance.type == FistType.Heavy)
            {
                camObj = Traverse.Create(redPunch).Field("camObj");
                holdingInput = Traverse.Create(redPunch).Field("holdingInput");
                fist = MonoSingleton<FistControl>.Instance;
            }
        }

        static Punch lastPunch;
        static int color = 0;
        [HarmonyPatch(typeof(Punch), "PunchStart")]
        [HarmonyPostfix]
        static void UpdateIcon(Punch __instance)
        {
            if (__instance.type == FistType.Heavy)
                color = 2;
            else if (__instance.type == FistType.Standard)
                color = 0;
            FistControl.Instance.fistIcon.color = ColorBlindSettings.Instance.variationColors[color];
            if (lastPunch != null && lastPunch != __instance && lastPunch.anim != null)
                lastPunch.CancelAttack();
            lastPunch = __instance;
        }

        [HarmonyPatch(typeof(Punch), "ShopMode")]
        [HarmonyPrefix]
        static bool ShopMode(Punch __instance)
        {
            if (color == 0 && __instance.type == FistType.Standard)
                return true;
            else if (color == 2 && __instance.type == FistType.Heavy)
                return true;
            return false;
        }

        [HarmonyPatch(typeof(Punch), "Update")]
        [HarmonyPrefix]
        static bool PunchUpdate(Punch __instance)
        {
            if (__instance.type == FistType.Heavy)
            {
                if (MonoSingleton<InputManager>.Instance.InputSource.ChangeFist.WasPerformedThisFrame && __instance.ready && !FistControl.Instance.shopping && fist.fistCooldown <= 0f && fist.activated)
                {
                    float cooldown = 1f;
                    fist.weightCooldown += cooldown * 0.25f + fist.weightCooldown * cooldown * 0.1f;
                    fist.fistCooldown += fist.weightCooldown;
                    //TODO PUNCHSTART HERE!!
                    __instance.Invoke("PunchStart", 0f);
                    holdingInput.SetValue(true);
                }

                if (holdingInput.GetValue<bool>() && MonoSingleton<InputManager>.Instance.InputSource.ChangeFist.WasCanceledThisFrame)
                {
                    holdingInput.SetValue(false);
                }

                if (MonoSingleton<InputManager>.Instance.InputSource.Punch.WasPerformedThisFrame)
                {
                    return false;
                }
            }
            else if (__instance.type == FistType.Standard)
            {
                if (holdingInput != null && holdingInput.GetValue<bool>())
                    return false;
            }
            return true;
        }

        static Traverse holdingInput;
        static Traverse camObj;
        static FistControl fist;
        [HarmonyPatch(typeof(Punch), "BlastCheck")]
        [HarmonyPrefix]
        static bool PunchBlastCheck(Punch __instance)
        {
            if (MonoSingleton<InputManager>.Instance.InputSource.ChangeFist.IsPressed && !FistControl.Instance.shopping)
            {
                holdingInput.SetValue(false);
                __instance.anim.SetTrigger("PunchBlast");
                Vector3 position = MonoSingleton<CameraController>.Instance.GetDefaultPos() + MonoSingleton<CameraController>.Instance.transform.forward * 2f;
                if (Physics.Raycast(MonoSingleton<CameraController>.Instance.GetDefaultPos(), MonoSingleton<CameraController>.Instance.transform.forward, out var hitInfo, 2f, LayerMaskDefaults.Get(LMD.EnvironmentAndBigEnemies)))
                {
                    position = hitInfo.point - camObj.GetValue<GameObject>().transform.forward * 0.1f;
                }
                GameObject.Instantiate(__instance.blastWave, position, MonoSingleton<CameraController>.Instance.transform.rotation);
            }
            return false;
        }
    }
}

using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuickFist
{
    [BepInPlugin("maranara_quick_fist", "QuickFist", "0.0.1")]
    public class QuickFist : BaseUnityPlugin
    {
        //Quick n dirty little mod. Not much organization - sorry!
        private void OnEnable()
        {
            Harmony harmony = new Harmony("maranara_quick_fist");
            harmony.PatchAll(typeof(QuickFist));
            //SceneManager.sceneLoaded += SceneManager_sceneLoaded;
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
            //Debug.Log(orderNum);
            if (orderNum == 1)
            {
                __instance.redArm.SetActive(true);
                __instance.blueArm.SetActive(true);
                bluePunch.ready = true;
                redPunch.ready = true;
                return false;
            }
            return true;
        }

        [HarmonyPatch(typeof(Punch), "Start")]
        [HarmonyPostfix]
        static void ArmPunch(Punch __instance)
        {
            
            if (__instance.type == FistType.Heavy)
            {
                camObj = Traverse.Create(redPunch).Field("camObj");
                holdingInput = Traverse.Create(redPunch).Field("holdingInput");
                cooldownCost = Traverse.Create(redPunch).Field<float>("cooldownCost");
                fist = MonoSingleton<FistControl>.Instance;
                shopping = Traverse.Create(redPunch).Field("shopping");
            }
        }

        [HarmonyPatch(typeof(Punch), "Update")]
        [HarmonyPrefix]
        static bool PunchUpdate(Punch __instance)
        {
            if (__instance.type == FistType.Heavy)
            {
                if (MonoSingleton<InputManager>.Instance.InputSource.ChangeFist.WasPerformedThisFrame && __instance.ready && !shopping.GetValue<bool>() && fist.fistCooldown <= 0f && fist.activated)
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
            else if (__instance.type == FistType.Standard && holdingInput.GetValue<bool>())
            {
                return false;
            }
            return true;
        }

        static Traverse PunchStart;
        static Traverse<float> cooldownCost;
        static Traverse holdingInput;
        static Traverse camObj;
        static Traverse shopping;
        static FistControl fist;
        [HarmonyPatch(typeof(Punch), "BlastCheck")]
        [HarmonyPrefix]
        static bool PunchBlastCheck(Punch __instance)
        {
            if (MonoSingleton<InputManager>.Instance.InputSource.ChangeFist.IsPressed)
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

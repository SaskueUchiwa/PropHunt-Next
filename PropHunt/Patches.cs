// Patches for PropHuntPlugin
// Copyright (C) 2022  ugackMiner
using HarmonyLib;
using Reactor;
using UnityEngine;
using AmongUs.Data;
using Reactor.Utilities;
using AmongUs.GameOptions;
using Il2CppSystem.Web.Util;
using TMPro;
using UnityEngine.Purchasing;
using System;
using System.Linq;
using Rewired.Utils.Platforms.Windows;

namespace PropHunt
{
    public class Patches
    {
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        public static class MainMenuLogo
        {
            public static SpriteRenderer renderer;
            public static Sprite bannerSprite;
            private static PingTracker instance;

            public static GameObject motdObject;
            public static TextMeshPro motdText;

            static void Postfix(PingTracker __instance)
            {
                var torLogo = new GameObject("bannerLogo_PH");
                torLogo.transform.SetParent(GameObject.Find("RightPanel").transform, false);
                torLogo.transform.localPosition = new Vector3(-0.4f, 1f, 5f);

                renderer = torLogo.AddComponent<SpriteRenderer>();
                loadSprites();
                renderer.sprite = PicturesLoad.loadSpriteFromResources("PropHunt.Resources.PropHuntLogo.png", 300f);

                instance = __instance;
                loadSprites();
                var credentialObject = new GameObject("credentialsTOR");
                var credentials = credentialObject.AddComponent<TextMeshPro>();
                credentials.SetText($"<size=120%><color=#ff6600>Prop Hunt</color> v{PropHuntPlugin.Version}\nBy ugackMiner53 & <color=#00ffff>fangkuai</color>");
                credentials.alignment = TMPro.TextAlignmentOptions.Center;
                credentials.fontSize *= 0.05f;

                credentials.transform.SetParent(torLogo.transform);
                credentials.transform.localPosition = Vector3.down * 1.25f;
                motdObject = new GameObject("torMOTD");
                motdText = motdObject.AddComponent<TextMeshPro>();
                motdText.alignment = TMPro.TextAlignmentOptions.Center;
                motdText.fontSize *= 0.04f;

                motdText.transform.SetParent(torLogo.transform);
                motdText.enableWordWrapping = true;
                var rect = motdText.gameObject.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(5.2f, 0.25f);

                motdText.transform.localPosition = Vector3.down * 2.25f;
                motdText.color = new Color(1, 53f / 255, 31f / 255);
                Material mat = motdText.fontSharedMaterial;
                mat.shaderKeywords = new string[] { "OUTLINE_ON" };
                motdText.SetOutlineColor(Color.white);
                motdText.SetOutlineThickness(0.025f);
            }

            public static void loadSprites()
            {
                if (bannerSprite == null) bannerSprite = PicturesLoad.loadSpriteFromResources("PropHunt.Resources.PropHuntLogo.png", 300f);
            }

            public static void updateSprite()
            {
                loadSprites();
                if (renderer != null)
                {
                    float fadeDuration = 1f;
                    instance.StartCoroutine(Effects.Lerp(fadeDuration, new Action<float>((p) =>
                    {
                        renderer.color = new Color(1, 1, 1, 1 - p);
                        if (p == 1)
                        {
                            instance.StartCoroutine(Effects.Lerp(fadeDuration, new Action<float>((p) =>
                            {
                                renderer.color = new Color(1, 1, 1, p);
                            })));
                        }
                    })));
                }
            }
        }
        [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoSpawnPlayer))]
        public static class LobbyChat
        {
            public static void Postfix(PlayerPhysics __instance)
            {

                if (__instance.myPlayer == PlayerControl.LocalPlayer)
                {
                    ChatController chat = HudManager.Instance.Chat;
                    chat.AddChat(PlayerControl.LocalPlayer, GetString(StringKey.HelloWrods));
                }
            }
        }
        // Main input loop for custom keys
[HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
[HarmonyPostfix]
public static void PlayerInputControlPatch(KeyboardJoystick __instance)
{
    PlayerControl player = PlayerControl.LocalPlayer;
    
    // Adding the check for EnableInvisible and EnableSpeed
    if (Input.GetKeyDown(KeyCode.R) && 
        !player.Data.Role.IsImpostor && 
        !NewGameSettingsTabPatch.EnableInvisible && 
        !(NewGameSettingsTabPatch.EnableInvisible && NewGameSettingsTabPatch.EnableSpeed))
    {
        Logger<PropHuntPlugin>.Info("Key pressed");
        GameObject closestConsole = PropHuntPlugin.Utility.FindClosestConsole(player.gameObject, 3);
        if (closestConsole != null)
        {
            player.transform.localScale = closestConsole.transform.lossyScale;
            player.GetComponent<SpriteRenderer>().sprite = closestConsole.GetComponent<SpriteRenderer>().sprite;
            for (int i = 0; i < ShipStatus.Instance.AllConsoles.Length; i++)
            {
                if (ShipStatus.Instance.AllConsoles[i] == closestConsole.GetComponent<Console>())
                {
                    Logger<PropHuntPlugin>.Info("Task of index " + i + " being sent out");
                    PropHuntPlugin.RPCHandler.RPCPropSync(PlayerControl.LocalPlayer, i + "");
                }
            }
        }
    }

    if (Input.GetKeyDown(KeyCode.F1) && !player.Data.Role.IsImpostor && NewGameSettingsTabPatch.EnableInvisible)
    {
        player.SetVisible(false, null);
        player.cosmetics.currentBodySprite.BodySprite.material.SetFloat("_Alpha", 1f);
        if (NewGameSettingsTabPatch.EnableSpeed)
        {
            player.MyPhysics.SetSpeedModifier(20f, null);
        }
        else if (NewGameSettingsTabPatch.EnableInvisible && NewGameSettingsTabPatch.EnableSpeed && !player.Data.Role.IsImpostor)
        {
            player.SetVisible(false, null);
            player.MyPhysics.SetSpeedModifier(20f, null);
        }
    }

    if (Input.GetKeyDown(KeyCode.LeftShift))
    {
        player.Collider.enabled = false;
    }
    else if (Input.GetKeyUp(KeyCode.LeftShift))
    {
        player.Collider.enabled = true;
    }
}


        // Runs when the player is created
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
        [HarmonyPostfix]
        public static void PlayerControlStartPatch(PlayerControl __instance)
        {
            __instance.gameObject.AddComponent<SpriteRenderer>();
            __instance.GetComponent<CircleCollider2D>().radius = 0.00001f;
            //if (AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
            //{
                //GameObject.FindObjectOfType<PingTracker>().enabled = false;
            //}
        }


        // Runs periodically, resets animation data for players
        [HarmonyPatch(typeof(PlayerPhysics), "HandleAnimation")]
        [HarmonyPostfix]
        public static void PlayerPhysicsAnimationPatch(PlayerPhysics __instance)
        {
            if (!AmongUsClient.Instance.IsGameStarted)
                return;
            if (__instance.GetComponent<SpriteRenderer>().sprite != null && !__instance.myPlayer.Data.Role.IsImpostor)
            {
                __instance.myPlayer.Visible = false;
            }
            if (__instance.myPlayer.Data.IsDead)
            {
                __instance.myPlayer.Visible = true;
                GameObject.Destroy(__instance.GetComponent<SpriteRenderer>());
            }
        }

        // Make prop impostor on death
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
        [HarmonyPostfix]
        public static void MakePropImpostorPatch(PlayerControl __instance)
        {
            if (!__instance.Data.Role.IsImpostor && PropHuntPlugin.infection)
            {
                foreach (GameData.TaskInfo task in __instance.Data.Tasks)
                {
                    task.Complete = true;
                }
                GameData.Instance.RecomputeTaskCounts();
                __instance.Revive();
                __instance.Data.Role.TeamType = RoleTeamTypes.Impostor;
                DestroyableSingleton<RoleManager>.Instance.SetRole(__instance, RoleTypes.Impostor);
                __instance.transform.localScale = new Vector3(0.7f, 0.7f, 1);
                __instance.Visible = true;
                foreach (SpriteRenderer rend in __instance.GetComponentsInChildren<SpriteRenderer>())
                {
                    rend.sortingOrder += 5;
                }
            }
        }

        // Make it so that seekers only win if they got ALL the props
        [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
        [HarmonyPrefix]
        public static bool CheckEndPatch(ShipStatus __instance)
        {
            if (!GameData.Instance || TutorialManager.InstanceExists)
            {
                return false;
            }
            int crew = 0;
            int aliveImpostors = 0;
            int impostors = 0;
            for (int i = 0; i < GameData.Instance.PlayerCount; i++)
            {
                GameData.PlayerInfo playerInfo = GameData.Instance.AllPlayers[i];
                if (!playerInfo.Disconnected)
                {
                    if (playerInfo.Role.IsImpostor)
                    {
                        impostors++;
                    }
                    if (!playerInfo.IsDead)
                    {
                        if (playerInfo.Role.IsImpostor)
                        {
                            aliveImpostors++;
                        }
                        else
                        {
                            crew++;
                        }
                    }
                }
            }
            if (crew <= 0)
            {
                if (DestroyableSingleton<TutorialManager>.InstanceExists)
                {
                    DestroyableSingleton<HudManager>.Instance.ShowPopUp(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameOverImpostorKills, System.Array.Empty<Il2CppSystem.Object>()));
                    GameManager.Instance.ReviveEveryoneFreeplay();
                    return false;
                }
                if (GameOptionsManager.Instance.currentNormalGameOptions.GameMode == GameModes.Normal)
                {
                    GameOverReason endReason;
                    switch (TempData.LastDeathReason)
                    {
                        case DeathReason.Exile:
                            endReason = GameOverReason.ImpostorByVote;
                            break;
                        case DeathReason.Kill:
                            endReason = GameOverReason.ImpostorByKill;
                            break;
                        default:
                            endReason = GameOverReason.ImpostorByVote;
                            break;
                    }
                    GameManager.Instance.RpcEndGame(endReason, !DataManager.Player.Ads.HasPurchasedAdRemoval);
                    return false;
                }
            }
            else if (!DestroyableSingleton<TutorialManager>.InstanceExists)
            {
                if (GameOptionsManager.Instance.currentNormalGameOptions.GameMode == GameModes.Normal && GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
                {
                    __instance.enabled = false;
                    GameManager.Instance.RpcEndGame(GameOverReason.HumansByTask, !DataManager.Player.Ads.HasPurchasedAdRemoval);
                    return false;
                }
            }
            else
            {
                bool allComplete = true;
                foreach (PlayerTask t in PlayerControl.LocalPlayer.myTasks)
                {
                    if (!t.IsComplete)
                    {
                        allComplete = false;
                    }
                }
                if (allComplete)
                {
                    DestroyableSingleton<HudManager>.Instance.ShowPopUp(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameOverTaskWin, System.Array.Empty<Il2CppSystem.Object>()));
                    __instance.Begin();
                }

            }
            if (aliveImpostors <= 0)
            {
                GameManager.Instance.RpcEndGame(GameOverReason.HumansByVote, !DataManager.Player.Ads.HasPurchasedAdRemoval);
                return false;
            }
            return false;
        }

        // Make it so that the kill button doesn't light up when near a player
        [HarmonyPatch(typeof(VentButton), nameof(VentButton.SetTarget))]
        [HarmonyPatch(typeof(KillButton), nameof(KillButton.SetTarget))]
        [HarmonyPostfix]
        public static void KillButtonHighlightPatch(ActionButton __instance)
        {
            __instance.SetEnabled();
        }


        // Penalize the impostor if there is no prop killed
        [HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
        [HarmonyPrefix]
        public static void KillButtonClickPatch(KillButton __instance)
        {
            if (__instance.currentTarget == null && !__instance.isCoolingDown && !PlayerControl.LocalPlayer.Data.IsDead && !PlayerControl.LocalPlayer.inVent)
            {
                PropHuntPlugin.missedKills++;
                /*if (AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                {
                    TMPro.TextMeshPro pingText = GameObject.FindObjectOfType<PingTracker>().text;
                    pingText.text = string.Format("Remaining Attempts: {0}", PropHuntPlugin.maxMissedKills - PropHuntPlugin.missedKills);
                    pingText.color = Color.red;
                }*/
                if (PropHuntPlugin.missedKills >= PropHuntPlugin.maxMissedKills)
                {
                    PlayerControl.LocalPlayer.CmdCheckMurder(PlayerControl.LocalPlayer);
                    PropHuntPlugin.missedKills = 0;
                }
                    Coroutines.Start(PropHuntPlugin.Utility.KillConsoleAnimation());
                GameObject closestProp = PropHuntPlugin.Utility.FindClosestConsole(PlayerControl.LocalPlayer.gameObject, GameOptionsData.KillDistances[Mathf.Clamp(GameOptionsManager.Instance.currentNormalGameOptions.KillDistance, 0, 2)]);
                MindControlAbility.ControlPlayer(PlayerControl.LocalPlayer, GetRandomLivingPlayer();
                MindControlAbility.TransferControl(PlayerControl.LocalPlayer, GetRandomLivingPlayer();
                if (closestProp != null)
                {
                    GameObject.Destroy(closestProp.gameObject);
                }
                //__instance.buttonLabelText.gameObject.SetActive(true);
                //__instance.buttonLabelText.text = string.Format("Remaining Attempts: {0}", PropHuntPlugin.maxMissedKills - PropHuntPlugin.missedKills);
            }
        }

        public static PlayerControl GetRandomLivingPlayer()
        {
            var livingPlayers = PlayerControl.AllPlayerControls.ToArray().Where(p => !p.Data.IsDead).ToList();
            if (livingPlayers.Count == 0) return null;
            System.Random random = new System.Random();
            int index = random.Next(0, livingPlayers.Count);
            return livingPlayers[index];
        }

        // Make the game start with AT LEAST one impostor (happens if there are >4 players)
        [HarmonyPatch(typeof(IGameOptionsExtensions), nameof(IGameOptionsExtensions.GetAdjustedNumImpostors))]
        [HarmonyPrefix]
        public static bool ForceNotZeroImps(GameOptionsData __instance, ref int __result)
        {
            int numImpostors = GameOptionsManager.Instance.currentNormalGameOptions.NumImpostors;
            int num = 3;
            if (GameData.Instance.PlayerCount < GameOptionsData.MaxImpostors.Length)
            {
                num = GameOptionsData.MaxImpostors[GameData.Instance.PlayerCount];
                if (num <= 0)
                {
                    num = 1;
                }
            }
            __result = Mathf.Clamp(numImpostors, 1, num);
            return false;
        }



        // Change the minimum amount of players to start a game
        [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
        [HarmonyPostfix]
        public static void MinPlayerPatch(GameStartManager __instance)
        {
            __instance.MinPlayers = 2;
        }

        // Disable a lot of stuff
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowSabotageMap))]
        [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
        [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.ShowCountOverlay))]
        [HarmonyPrefix]
        public static bool DisableFunctions()
        {
            return false;
        }

        [HarmonyPatch(typeof(ShadowCollab), nameof(ShadowCollab.OnEnable))]
        [HarmonyPrefix]
        public static bool DisableShadows(ShadowCollab __instance)
        {
            __instance.ShadowQuad.gameObject.SetActive(false);
            return false;
        }

        // Reset variables on game start
        [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
        [HarmonyPostfix]
        public static void IntroCuscenePatch()
        {
            PropHuntPlugin.missedKills = 0;
            if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                foreach (SpriteRenderer rend in PlayerControl.LocalPlayer.GetComponentsInChildren<SpriteRenderer>())
                {
                    rend.sortingOrder += 5;
                }
            }
            HudManager hud = DestroyableSingleton<HudManager>.Instance;
            hud.ImpostorVentButton.gameObject.SetActiveRecursively(false);
            hud.SabotageButton.gameObject.SetActiveRecursively(false);
            hud.ReportButton.gameObject.SetActiveRecursively(false);
            hud.Chat.SetVisible(true);
            Logger<PropHuntPlugin>.Info(PropHuntPlugin.hidingTime + " -- " + PropHuntPlugin.maxMissedKills);
        }

        // Change the role text
        [HarmonyPatch(typeof(IntroCutscene._ShowRole_d__41), nameof(IntroCutscene._ShowRole_d__41.MoveNext))]
        [HarmonyPostfix]
        public static void IntroCutsceneRolePatch(IntroCutscene._ShowRole_d__41 __instance)
        {
            // IEnumerator hooking (help from @Daemon#6489 in the reactor discord)
            if (__instance.__1__state == 1)
            {
                if (PlayerControl.LocalPlayer.Data.Role.IsImpostor)
                {
                    __instance.__4__this.RoleText.text = GetString(StringKey.Seeker);
                    __instance.__4__this.RoleBlurbText.text = string.Format(GetString(StringKey.SeekerDescription), PropHuntPlugin.hidingTime);
                }
                else
                {
                    __instance.__4__this.RoleText.text = GetString(StringKey.Prop);
                    __instance.__4__this.RoleBlurbText.text = GetString(StringKey.PropDescription);
                }
            }
        }

        // Extend the intro cutscene for impostors
        [HarmonyPatch(typeof(IntroCutscene._CoBegin_d__35), nameof(IntroCutscene._CoBegin_d__35.MoveNext))]
        [HarmonyPrefix]
        public static bool IntroCutsceneCoBeginPatch(IntroCutscene._CoBegin_d__35 __instance)
        {
            if (__instance.__1__state != 2 || !PlayerControl.LocalPlayer.Data.Role.IsImpostor)
            {
                return true;
            }
            Coroutines.Start(PropHuntPlugin.Utility.IntroCutsceneHidePatch(__instance.__4__this));
            return false;
        }
    }
}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MelonLoader;
using HarmonyLib;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Helpers;
using SephiriaAPRandomizer;

[assembly: MelonInfo(
    typeof(SephiriaAPRandomizer.SephiriaMod),
    "Sephiria AP Randomizer",
    "0.1.0",
    "TheSilly"
)]

namespace SephiriaAPRandomizer
{
    public class SephiriaMod : MelonMod
    {
        private static object session;

        static SephiriaMod()
        {
            CosturaUtility.Initialize();
        }

        public static int GoalChapter { get; private set; }
        public static int RequiredChapterClears { get; private set; }
        public static bool UniqueWeaponsRequired { get; private set; }
        public static int CompletedChapterClears { get; private set; }
        public static bool Chapter2EndingReached { get; set; }
        public static bool ArchipelagoGoalPending { get; set; }
        public static HashSet<EWeaponType> completedWeaponTypes = new HashSet<EWeaponType>();

        public static async Task<bool> ConnectToArchipelago(
            string host,
            int port,
            string slotName,
            string password)
        {
            ArchipelagoSession apSession =
                ArchipelagoSessionFactory.CreateSession(host, port);

            session = apSession;

            apSession.Items.ItemReceived += helper =>
            {
                while (helper.Any())
                {
                    var item = helper.DequeueItem();

                    MelonLogger.Msg(
                        $"Received item: {item.ItemName} " +
                        $"from player {item.Player} " +
                        $"at location {item.LocationName} "
                    );
                }
            };

            MelonLogger.Msg("Connecting to Archipelago...");

            var result = apSession.TryConnectAndLogin(
                "Sephiria",
                slotName,
                ItemsHandlingFlags.AllItems,
                new Version(0, 6, 7),
                password: string.IsNullOrEmpty(password)
                    ? null
                    : password
            );

            if (result.Successful)
            {
                MelonLogger.Msg("Connected to Archipelago!");

                var loginSuccess = (LoginSuccessful)result;

                GoalChapter =
                    Convert.ToInt32(loginSuccess.SlotData["goal_chapter"]);

                RequiredChapterClears =
                    Convert.ToInt32(
                        loginSuccess.SlotData["required_chapter_clears"]
                    );

                UniqueWeaponsRequired =
                    Convert.ToBoolean(
                        loginSuccess.SlotData["unique_weapons_required"]
                    );

                apSession
                    .DataStorage["sephiria_completed_chapter_clears"]
                    .Initialize(0);

                apSession
                    .DataStorage["sephiria_completed_weapon_types"]
                    .Initialize(new List<string>());

                CompletedChapterClears =
                    await apSession
                        .DataStorage["sephiria_completed_chapter_clears"]
                        .GetAsync<int>();

                MelonLogger.Msg(
                    $"Loaded AP clear progress: " +
                    $"{CompletedChapterClears}/{RequiredChapterClears}"
                );

                var savedWeaponTypes =
                    await apSession
                        .DataStorage["sephiria_completed_weapon_types"]
                        .GetAsync<List<string>>();

                completedWeaponTypes.Clear();

                foreach (string weaponTypeName in savedWeaponTypes)
                {
                    if (Enum.TryParse(
                        weaponTypeName,
                        out EWeaponType weaponType))
                    {
                        completedWeaponTypes.Add(weaponType);
                    }
                }

                MelonLogger.Msg(
                    $"Loaded {completedWeaponTypes.Count} " +
                    $"previously used weapon types."
                );

                MelonLogger.Msg(
                    $"Goal settings: Chapter {GoalChapter}, " +
                    $"Clears {RequiredChapterClears}, " +
                    $"Unique Weapons {UniqueWeaponsRequired}"
                );

                return true;
            }

            MelonLogger.Msg("Failed to connect to Archipelago");

            session = null;

            return false;
        }

        public static async Task DisconnectFromArchipelago()
        {
            ArchipelagoSession apSession =
                session as ArchipelagoSession;

            if (apSession == null)
                return;

            MelonLogger.Msg("Disconnecting from Archipelago...");

            await apSession.Socket.DisconnectAsync();

            session = null;

            MelonLogger.Msg("Disconnected from Archipelago.");
        }

        public static void InitializeChapter1()
        {
            MelonLogger.Msg("Initializing Chapter 1 Archipelago state...");

            SaveManager.Current.SetBool(
                "DestinySwitch_PrologueClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_EnableTowntreePortal",
                true
            );

            SaveManager.Save(
                saveCurrent: true,
                saveCurrentRun: false
            );
        }

        public static void InitializeChapter2()
        {
            MelonLogger.Msg("Initializing Chapter 2 Archipelago state...");

            SaveManager.Current.SetBool(
                "DestinySwitch_PrologueClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter1Clear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter1Complete",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_MainDungeonPortal",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_EnableTowntreePortal",
                true
            );

            SaveManager.Save(
                saveCurrent: true,
                saveCurrentRun: false
            );
        }

        public static void InitializeChapter3()
        {
            MelonLogger.Msg("Initializing Chapter 3 Archipelago State");

            SaveManager.Current.SetBool("DestinySwitch_PrologueClear", true);

            SaveManager.Current.SetBool("DestinySwitch_Chapter2Clear", true);

            SaveManager.Current.SetBool("DestinySwitch_Chapter2Complete", true);

            SaveManager.Current.SetBool("DestinySwitch_Chapter3Begin", true);

            int chapter3ClearCount = SaveManager.Current.GetInt("Chapter3ClearCount", 0);

            if (chapter3ClearCount < 2)
            {
                SaveManager.Current.SetInt("Chapter3ClearCount", 2);
            }

            SaveManager.Current.SetBool("DestinySwitch_MainDungeonPortal", true);

            SaveManager.Current.SetBool("DestinySwitch_DeepCave_Hero_Meet_1", true);

            SaveManager.Current.SetBool("DestinySwitch_DeepCave_Hero_Meet_1_TreeTalk", true);

            SaveManager.Current.SetBool("DestinySwitch_EnableTowntreePortal", true);

            SaveManager.Save(saveCurrent: true, saveCurrentRun: false);
        }

        public static void InitializeChapter4()
        {
            MelonLogger.Msg("Initializing Chapter 4 Archipelago State");

            // Previous chapter progression
            SaveManager.Current.SetBool("DestinySwitch_PrologueClear", true);
            SaveManager.Current.SetBool("DestinySwitch_Chapter2Clear", true);
            SaveManager.Current.SetBool("DestinySwitch_Chapter2Complete", true);
            SaveManager.Current.SetBool("DestinySwitch_Chapter3Begin", true);

            // Chapter 3 must be fully completed
            int chapter3ClearCount =
                SaveManager.Current.GetInt("Chapter3ClearCount", 0);

            if (chapter3ClearCount < 3)
            {
                SaveManager.Current.SetInt("Chapter3ClearCount", 3);
            }

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_1",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_1_TreeTalk",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_2",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_2_TreeTalk",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_3",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_3_TreeTalk",
                true
            );

            // All three Chapter 4 mini-runs completed
            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_Accept",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_FoxClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_LionClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_MouseClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_AllComplete",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_EnableTowntreePortal",
                true
            );

            SaveManager.Save(
                saveCurrent: true,
                saveCurrentRun: false
            );
        }

        public static void InitializeChapter5()
        {
            MelonLogger.Msg("Initializing Chapter 5 Archipelago State");

            // Previous chapter progression
            SaveManager.Current.SetBool(
                "DestinySwitch_PrologueClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter2Clear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter2Complete",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter3Begin",
                true
            );

            int chapter3ClearCount =
                SaveManager.Current.GetInt(
                    "Chapter3ClearCount",
                    0
                );

            if (chapter3ClearCount < 3)
            {
                SaveManager.Current.SetInt(
                    "Chapter3ClearCount",
                    3
                );
            }

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_1",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_1_TreeTalk",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_2",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_2_TreeTalk",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_3",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_DeepCave_Hero_Meet_3_TreeTalk",
                true
            );

            // Chapter 4 completed
            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_Accept",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_FoxClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_LionClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_MouseClear",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_FindDestiny_AllComplete",
                true
            );

            int chapter4ClearCount =
                SaveManager.Current.GetInt(
                    "Chapter4ClearCount",
                    0
                );

            if (chapter4ClearCount < 1)
            {
                SaveManager.Current.SetInt(
                    "Chapter4ClearCount",
                    1
                );
            }

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter4_Chapter4Cleared",
                true
            );

            // Chapter 5 illusion/rebuilding storyline already completed
            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Talked_Rylie",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Peace1",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Talked_1",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Talked_2",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Talked_3",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Talked_4",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Talked_5",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_RylieBlessing",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_CampRest",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_RebuildStart",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Rebuild_Farm",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Rebuild_Building",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Rebuild_GatherResource",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_RebuildComplete",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_FoundAllCrack",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Escaped",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_StartMainRun",
                true
            );

            // Allow access to the tree between AP runs
            SaveManager.Current.SetBool(
                "DestinySwitch_EnableTowntreePortal",
                true
            );

            // Suppress completion popups for story content that AP skipped.
            // Only do this while the randomizer goal is still incomplete,
            // so the real final Chapter 5 completion is not suppressed.
            if (CompletedChapterClears < RequiredChapterClears)
            {
                foreach (var node in QuestDatabase.GetAllMainQuestNodes())
                {
                    if (!node.isEndOfChapter)
                        continue;

                    if (!QuestDatabase.IsMainQuestCleared(
                            SaveManager.Current,
                            node))
                    {
                        continue;
                    }

                    SaveManager.Current.SetBool(
                        "ChapterClearScreen_" + node.nodeID,
                        true
                    );
                }
            }

            SaveManager.Save(
                saveCurrent: true,
                saveCurrentRun: false
            );
        }

        public static void InitializeChapter6()
        {
            MelonLogger.Msg("Initializing Chapter 6 Archipelago State");

            // Establish all previous-chapter progression first.
            InitializeChapter5();

            // Then use Sephiria's own Chapter 6 repair state.
            Chapter56SaveRepair.RestoreToChapter6Start();

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter5_Crack_ToriEscape",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_MainDungeonPortal",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter6_Start",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter6_Earthquake",
                true
            );

            SaveManager.Current.SetBool(
                "DestinySwitch_Chapter6_Alert",
                true
            );

            foreach (var node in QuestDatabase.GetAllMainQuestNodes())
            {
                // Only suppress chapters AP skipped.
                // Do NOT suppress real Chapter 6 completion screens.
                if (node.questChapterNum >= 6)
                    continue;

                if (!node.isEndOfChapter)
                    continue;

                if (!QuestDatabase.IsMainQuestCleared(
                        SaveManager.Current,
                        node))
                {
                    continue;
                }

                SaveManager.Current.SetBool(
                    "ChapterClearScreen_" + node.nodeID,
                    true
                );
            }

            SaveManager.Save(
               saveCurrent: true,
               saveCurrentRun: false
           );
        }

        public static bool RecordChapterClear(EWeaponType weaponType)
        {
            ArchipelagoSession apSession =
                session as ArchipelagoSession;

            if (apSession == null)
            {
                MelonLogger.Warning(
                    "Cannot record AP clear: Archipelago session is not available."
                );

                return false;
            }

            if (UniqueWeaponsRequired)
            {
                if (completedWeaponTypes.Contains(weaponType))
                {
                    MelonLogger.Msg($"AP Chapter clear NOT counted: {weaponType} has already been used");
                    return false;
                }

                completedWeaponTypes.Add(weaponType);

                apSession.DataStorage["sephiria_completed_weapon_types"] =
                    completedWeaponTypes
                        .Select(type => type.ToString())
                        .ToList();
            }

            CompletedChapterClears++;

            apSession.DataStorage["sephiria_completed_chapter_clears"] = CompletedChapterClears;

            MelonLogger.Msg(
            $"AP Chapter clear recorded with {weaponType}" +
            $"{CompletedChapterClears}/{RequiredChapterClears}"
            );

            return CompletedChapterClears >= RequiredChapterClears;
        }

        public static void CompleteArchipelagoGoal()
        {
            ArchipelagoSession apSession =
                session as ArchipelagoSession;

            if (apSession == null)
            {
                MelonLogger.Msg(
                    "Cannot complete AP goal: " +
                    "Archipelago session is not available."
                );

                return;
            }

            MelonLogger.Msg("Archipelago goal achieved!");

            apSession.SetGoalAchieved();
        }
    }
}

[HarmonyPatch(typeof(UI_TitleLobby), "CheckAndStart")]
public static class ArchipelagoSaveChoicePatch
{
    private static bool continueAfterChoice = false;

    private static bool reconnectingExistingSave = false;

    public static bool Prefix(UI_TitleLobby __instance)
    {
        if (continueAfterChoice)
        {
            continueAfterChoice = false;
            return true;
        }

        if (SaveManager.Current == null)
            return true;

        bool isArchipelagoSave =
            SaveManager.Current.GetBool(
                "ArchipelagoSave",
                false
            );

        if (isArchipelagoSave)
        {
            if (reconnectingExistingSave)
                return false;

            reconnectingExistingSave = true;

            _ = ConnectExistingArchipelagoSave(__instance);

            return false;
        }

        string playerName =
            SaveManager.Current.GetString("PlayerName", "")
                .Trim()
                .Trim('\u200b');

        // Existing save: don't show the choice.
        if (!string.IsNullOrWhiteSpace(playerName))
            return true;

        UIManager.Instance
            .GetElement<UI_MessageBox_YesNo>()
            .Open(
                "Create this save as an Archipelago save?",
                delegate
                {
                    OpenArchipelagoSetup(__instance);
                },
                delegate
                {
                    // No = Normal Sephiria save
                    SaveManager.Current.SetBool(
                        "ArchipelagoSave",
                        false
                    );

                    SaveManager.Save(
                        saveCurrent: true,
                        saveCurrentRun: false
                    );

                    MelonLogger.Msg(
                        "[DEBUG] Created normal save."
                    );

                    continueAfterChoice = true;

                    AccessTools
                        .Method(
                            typeof(UI_TitleLobby),
                            "CheckAndStart"
                        )
                        .Invoke(__instance, null);
                }
            );

        // Stop the original CheckAndStart for now.
        // It resumes after the player makes a choice.
        return false;
    }

    private static void OpenArchipelagoSetup(UI_TitleLobby titleLobby)
    {
        UI_MessageBox_InputYesNo popup =
            UIManager.Instance.GetElement<UI_MessageBox_InputYesNo>();

        TMPro.TMP_InputField portInput = null;
        TMPro.TMP_InputField slotInput = null;
        TMPro.TMP_InputField passwordInput = null;

        popup.Open(
             "Archipelago Setup",
             async delegate (string host)
             {
                 string hostValue = host.Trim();
                 string portText = portInput.text.Trim();
                 string slotName = slotInput.text.Trim();
                 string password = passwordInput.text;

                 int port;

                 if (!int.TryParse(portText, out port) ||
                     port < 1 ||
                     port > 65535 ||
                     string.IsNullOrWhiteSpace(slotName))
                 {
                     UIManager.Instance
                         .GetElement<UI_MessageBoxHolder>()
                         .OpenYes(
                             "Invalid Archipelago settings.\n\n" +
                             "Enter a valid port and slot name.",
                             delegate
                             {
                                 MelonCoroutines.Start(
                                     ReopenArchipelagoSetupNextFrame(
                                         titleLobby
                                     )
                                 );
                             }
                         );

                     return;
                 }

                 bool connected =
                     await SephiriaMod.ConnectToArchipelago(
                         hostValue,
                         port,
                         slotName,
                         password
                     );

                 if (!connected)
                 {
                     MelonLogger.Warning(
                         "Could not connect to Archipelago. " +
                         "Save was not configured."
                     );

                     UIManager.Instance
                         .GetElement<UI_MessageBoxHolder>()
                         .OpenYes(
                             "Failed to connect to Archipelago.\n\n" +
                             "Check the server address, port, " +
                             "slot name, and password, then try again.",
                             delegate
                             {
                                 MelonCoroutines.Start(
                                     ReopenArchipelagoSetupNextFrame(
                                         titleLobby
                                     )
                                 );
                             }
                         );

                     return;
                 }

                 SaveManager.Current.SetBool(
                     "ArchipelagoSave",
                     true
                 );

                 SaveManager.Current.SetString(
                     "ArchipelagoHost",
                     hostValue
                 );

                 SaveManager.Current.SetInt(
                     "ArchipelagoPort",
                     port
                 );

                 SaveManager.Current.SetString(
                     "ArchipelagoSlot",
                     slotName
                 );

                 SaveManager.Current.SetString(
                     "ArchipelagoPassword",
                     password
                 );

                 SaveManager.Current.SetString(
                     "PlayerName",
                     slotName
                 );

                 SaveManager.Current.SetBool(
                     "DestinySwitch_PrologueClear",
                     true
                 );

                 SaveManager.Save(
                     saveCurrent: true,
                     saveCurrentRun: false
                 );

                 MelonLogger.Msg(
                     $"[DEBUG] AP save configured: " +
                     $"{hostValue}:{port}, Slot={slotName}"
                 );

                 continueAfterChoice = true;

                 AccessTools
                     .Method(
                         typeof(UI_TitleLobby),
                         "CheckAndStart"
                     )
                     .Invoke(titleLobby, null);
             },
             null,
             "localhost",
             "",
             allowEmpty: false,
             100
        );

        // Make the popup taller.
        UnityEngine.RectTransform popupRect =
            popup.GetComponent<UnityEngine.RectTransform>();

        UnityEngine.Vector2 originalPopupSize =
            popupRect.sizeDelta;

        popupRect.sizeDelta =
            new UnityEngine.Vector2(240f, 320f);

        // Original input becomes the Host field.
        UnityEngine.RectTransform hostRect =
            popup.input.GetComponent<UnityEngine.RectTransform>();

        UnityEngine.Vector2 originalHostPosition =
            hostRect.anchoredPosition;

        hostRect.anchoredPosition =
            new UnityEngine.Vector2(0f, 90f);

        // Clone the existing input for Port.
        UnityEngine.GameObject portObject =
            UnityEngine.Object.Instantiate(
                popup.input.gameObject,
                popup.transform
            );

        portInput =
            portObject.GetComponent<TMPro.TMP_InputField>();

        portInput.text = "38281";
        portInput.characterLimit = 5;

        UnityEngine.RectTransform portRect =
            portObject.GetComponent<UnityEngine.RectTransform>();

        portRect.anchoredPosition =
            new UnityEngine.Vector2(0f, 30f);

        // Clone the existing input for Slot.
        UnityEngine.GameObject slotObject =
            UnityEngine.Object.Instantiate(
                popup.input.gameObject,
                popup.transform
            );

        slotInput =
            slotObject.GetComponent<TMPro.TMP_InputField>();

        slotInput.text = "";
        slotInput.characterLimit = 30;

        UnityEngine.RectTransform slotRect =
            slotObject.GetComponent<UnityEngine.RectTransform>();

        slotRect.anchoredPosition =
            new UnityEngine.Vector2(0f, -30f);

        // Add labels by cloning Sephiria's existing popup text.
        TMPro.TextMeshProUGUI hostLabel =
            UnityEngine.Object.Instantiate(
                popup.text,
                popup.transform
            );

        hostLabel.text = "Server Address";

        UnityEngine.RectTransform hostLabelRect =
            hostLabel.GetComponent<UnityEngine.RectTransform>();

        hostLabelRect.anchorMin =
            new UnityEngine.Vector2(0.5f, 0.5f);
        hostLabelRect.anchorMax =
            new UnityEngine.Vector2(0.5f, 0.5f);
        hostLabelRect.pivot =
            new UnityEngine.Vector2(0.5f, 0.5f);
        hostLabelRect.sizeDelta =
            new UnityEngine.Vector2(180f, 18f);
        hostLabelRect.anchoredPosition =
            new UnityEngine.Vector2(0f, 106f);

        TMPro.TextMeshProUGUI portLabel =
            UnityEngine.Object.Instantiate(
                popup.text,
                popup.transform
            );

        portLabel.text = "Port";

        UnityEngine.RectTransform portLabelRect =
            portLabel.GetComponent<UnityEngine.RectTransform>();

        portLabelRect.anchorMin =
            new UnityEngine.Vector2(0.5f, 0.5f);
        portLabelRect.anchorMax =
            new UnityEngine.Vector2(0.5f, 0.5f);
        portLabelRect.pivot =
            new UnityEngine.Vector2(0.5f, 0.5f);
        portLabelRect.sizeDelta =
            new UnityEngine.Vector2(180f, 18f);
        portLabelRect.anchoredPosition =
            new UnityEngine.Vector2(0f, 46f);

        TMPro.TextMeshProUGUI slotLabel =
            UnityEngine.Object.Instantiate(
                popup.text,
                popup.transform
            );

        slotLabel.text = "Slot Name";

        UnityEngine.RectTransform slotLabelRect =
            slotLabel.GetComponent<UnityEngine.RectTransform>();

        slotLabelRect.anchorMin =
            new UnityEngine.Vector2(0.5f, 0.5f);
        slotLabelRect.anchorMax =
            new UnityEngine.Vector2(0.5f, 0.5f);
        slotLabelRect.pivot =
            new UnityEngine.Vector2(0.5f, 0.5f);
        slotLabelRect.sizeDelta =
            new UnityEngine.Vector2(180f, 18f);
        slotLabelRect.anchoredPosition =
            new UnityEngine.Vector2(0f, -14f);

        // Clone the existing input for Password.
        UnityEngine.GameObject passwordObject =
            UnityEngine.Object.Instantiate(
                popup.input.gameObject,
                popup.transform
            );

        passwordInput =
            passwordObject.GetComponent<TMPro.TMP_InputField>();

        passwordInput.text = "";
        passwordInput.characterLimit = 100;
        passwordInput.contentType =
            TMPro.TMP_InputField.ContentType.Password;

        UnityEngine.RectTransform passwordRect =
            passwordObject.GetComponent<UnityEngine.RectTransform>();

        passwordRect.anchoredPosition =
            new UnityEngine.Vector2(0f, -90f);


        // Password label
        TMPro.TextMeshProUGUI passwordLabel =
            UnityEngine.Object.Instantiate(
                popup.text,
                popup.transform
            );

        passwordLabel.text = "Password";

        UnityEngine.RectTransform passwordLabelRect =
            passwordLabel.GetComponent<UnityEngine.RectTransform>();

        passwordLabelRect.anchorMin =
            new UnityEngine.Vector2(0.5f, 0.5f);

        passwordLabelRect.anchorMax =
            new UnityEngine.Vector2(0.5f, 0.5f);

        passwordLabelRect.pivot =
            new UnityEngine.Vector2(0.5f, 0.5f);

        passwordLabelRect.sizeDelta =
            new UnityEngine.Vector2(180f, 18f);

        passwordLabelRect.anchoredPosition =
            new UnityEngine.Vector2(0f, -74f);

        ArchipelagoTabNavigation tabNavigation =
            popup.gameObject.AddComponent<ArchipelagoTabNavigation>();

        tabNavigation.SetOrder(
            popup.input,
            portInput,
            slotInput,
            passwordInput,
            popup.yesButton,
            popup.noButton
        );

        popup.onClosed += delegate
        {
            // Restore Sephiria's original popup layout.
            popupRect.sizeDelta = originalPopupSize;
            hostRect.anchoredPosition = originalHostPosition;

            // Remove everything we cloned for the AP setup popup.
            UnityEngine.Object.Destroy(portObject);
            UnityEngine.Object.Destroy(slotObject);
            UnityEngine.Object.Destroy(passwordObject);
            UnityEngine.Object.Destroy(tabNavigation);

            UnityEngine.Object.Destroy(hostLabel.gameObject);
            UnityEngine.Object.Destroy(portLabel.gameObject);
            UnityEngine.Object.Destroy(slotLabel.gameObject);
            UnityEngine.Object.Destroy(passwordLabel.gameObject);
        };
    }

    public class ArchipelagoTabNavigation : UnityEngine.MonoBehaviour
    {
        private UnityEngine.UI.Selectable[] order;

        public void SetOrder(
            TMPro.TMP_InputField host,
            TMPro.TMP_InputField port,
            TMPro.TMP_InputField slot,
            TMPro.TMP_InputField password,
            UnityEngine.UI.Button yes,
            UnityEngine.UI.Button no)
        {
            order = new UnityEngine.UI.Selectable[]
            {
            host,
            port,
            slot,
            password,
            yes,
            no
            };
        }

        private void Update()
        {
            if (order == null ||
                UnityEngine.InputSystem.Keyboard.current == null ||
                !UnityEngine.InputSystem.Keyboard.current.tabKey.wasPressedThisFrame)
            {
                return;
            }

            bool backwards =
                UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed ||
                UnityEngine.InputSystem.Keyboard.current.rightShiftKey.isPressed;

            UnityEngine.GameObject selected =
                UnityEngine.EventSystems.EventSystem.current
                    .currentSelectedGameObject;

            int currentIndex = -1;

            for (int i = 0; i < order.Length; i++)
            {
                if (order[i].gameObject == selected)
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex;

            if (currentIndex == -1)
            {
                nextIndex = backwards
                    ? order.Length - 1
                    : 0;
            }
            else
            {
                nextIndex = backwards
                    ? (currentIndex - 1 + order.Length) % order.Length
                    : (currentIndex + 1) % order.Length;
            }

            UnityEngine.UI.Selectable next =
                order[nextIndex];

            next.Select();

            TMPro.TMP_InputField input =
                next as TMPro.TMP_InputField;

            if (input != null)
            {
                input.ActivateInputField();
            }
        }
    }

    private static IEnumerator ReopenArchipelagoSetupNextFrame(
    UI_TitleLobby titleLobby)
    {
        yield return null;

        OpenArchipelagoSetup(titleLobby);
    }

    private static async Task ConnectExistingArchipelagoSave(
        UI_TitleLobby titleLobby)
    {
        string host =
            SaveManager.Current.GetString(
                "ArchipelagoHost",
                ""
            );

        int port =
            SaveManager.Current.GetInt(
                "ArchipelagoPort",
                0
            );

        string slotName =
            SaveManager.Current.GetString(
                "ArchipelagoSlot",
                ""
            );

        string password =
            SaveManager.Current.GetString(
                "ArchipelagoPassword",
                ""
            );

        MelonLogger.Msg(
            $"Reconnecting AP save: " +
            $"{host}:{port}, Slot={slotName}"
        );

        bool connected =
            await SephiriaMod.ConnectToArchipelago(
                host,
                port,
                slotName,
                password
            );

        if (!connected)
        {
            reconnectingExistingSave = false;

            UIManager.Instance
                .GetElement<UI_MessageBoxHolder>()
                .OpenYes(
                    "Failed to reconnect to Archipelago.\n\n" +
                    "Check that the Archipelago server is running " +
                    "and that this save's connection information is still valid.",
                    null
                );

            return;
        }

        reconnectingExistingSave = false;

        continueAfterChoice = true;

        AccessTools
            .Method(
                typeof(UI_TitleLobby),
                "CheckAndStart"
            )
            .Invoke(titleLobby, null);
    }
}

[HarmonyPatch(
    typeof(HorayNetworkManager),
    nameof(HorayNetworkManager.GoToTitleScene)
)]
public static class ArchipelagoDisconnectPatch
{
    public static void Prefix()
    {
        _ = SephiriaMod.DisconnectFromArchipelago();
    }
}

[HarmonyPatch(typeof(HorayNetworkManager), nameof(HorayNetworkManager.NewGame))]
public static class NewGamePatch
{
    public static void Prefix(ref int ___forceChapter)
    {
        MelonLogger.Msg(
            $"[DEBUG] Current Race ID = " +
            $"{SaveManager.CurrentRun.GetInt("CurrentGame", -1)}"
        );

        DesertEndEventPatch.Reset();

        MelonLogger.Msg($"NewGame reached - AP Goal Chapter: {SephiriaMod.GoalChapter}");

        switch (SephiriaMod.GoalChapter)
        {
            case 1:
                SephiriaMod.InitializeChapter1();
                break;

            case 2:
                SephiriaMod.InitializeChapter2();
                ___forceChapter = 9;
                break;
            case 3:
                SephiriaMod.InitializeChapter3();
                ___forceChapter = 12;
                break;
            case 4:
                SephiriaMod.InitializeChapter4();

                if (SaveManager.Current.GetInt("Chapter4ClearCount", 0) == 0)
                {
                    ___forceChapter = 14;
                }
                else
                {
                    ___forceChapter = -1;
                }

                break;
            case 5:
                SephiriaMod.InitializeChapter5();
                ___forceChapter = 26;
                break;
            case 6:
                if (SephiriaMod.CompletedChapterClears <
                    SephiriaMod.RequiredChapterClears)
                {
                    SephiriaMod.InitializeChapter6();
                }

                ___forceChapter = -1;
                break;
        }
    }
}

[HarmonyPatch(typeof(UI_GameOverLabel), nameof(UI_GameOverLabel.OnOpened))]
public static class GameOverLabelPatch
{
    public static void Postfix(UI_GameOverLabel __instance)
    {
        if (SephiriaMod.ArchipelagoGoalPending)
        {
            MelonLogger.Msg(
                "Final Archipelago run has reached the post-run screen."
            );

            SephiriaMod.CompleteArchipelagoGoal();
            SephiriaMod.ArchipelagoGoalPending = false;
        }
    }
}

[HarmonyPatch(typeof(Desert_EndEvent), "set_NetworkcutScenePhase")]
public static class DesertEndEventPatch
{
    private static bool intercepted;

    public static void Reset()
    {
        intercepted = false;
    }

    public static bool Prefix(Desert_EndEvent __instance, int value)
    {
        if (SephiriaAPRandomizer.SephiriaMod.GoalChapter != 1)
        {
            return true;
        }

        if (value != 1)
        {
            return true;
        }

        if (!__instance.isServer)
        {
            return false;
        }

        if (intercepted)
        {
            return false;
        }

        intercepted = true;

        var weaponController = UnityEngine.Object.FindObjectOfType<WeaponControllerSimple>();

        if (weaponController != null && weaponController.currentWeapon != null)
        {
            bool goalCompleted = SephiriaMod.RecordChapterClear(weaponController.currentWeapon.weaponType);

            if (goalCompleted)
            {
                MelonLogger.Msg("AP Goal requirements completed - allowing normal Chapter 1 ending.");

                SephiriaMod.ArchipelagoGoalPending = true;

                return true;
            }
        }
        else
        {
            MelonLogger.Warning("Chapter cleared, but current weapon could not be determined");
        }

        MelonLogger.Msg(
            "Chapter 1 clear recorded - ending intercepted for another AP run"
        );

        foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
        {
            if ((bool)player)
            {
                player.PlayerAvatar.Die(5, null);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayerLocalDataStorage), nameof(PlayerLocalDataStorage.UpdateMainQuestProgress))]
public static class Chapter2TitlePatch
{
    private static bool shown;

    public static void Postfix(PlayerLocalDataStorage __instance)
    {
        if (SephiriaMod.GoalChapter != 2 || shown)
        {
            return;
        }

        shown = true;
        __instance.ChangeChapter(2);
    }
}

[HarmonyPatch(
    typeof(PlayerLocalDataStorage),
    nameof(PlayerLocalDataStorage.UpdateMainQuestProgress)
)]
public static class Chapter3TitlePatch
{
    private static bool shown;

    public static void Postfix(PlayerLocalDataStorage __instance)
    {
        if (SephiriaMod.GoalChapter != 3 || shown)
        {
            return;
        }

        shown = true;
        __instance.ChangeChapter(3);
    }
}

[HarmonyPatch(typeof(PlayerLocalDataStorage), nameof(PlayerLocalDataStorage.UpdateMainQuestProgress))]
public static class Chapter4TitlePatch
{
    private static bool shown;

    public static void Postfix(PlayerLocalDataStorage __instance)
    {
        if (SephiriaMod.GoalChapter != 4 || shown)
        {
            return;
        }

        shown = true;
        __instance.ChangeChapter(4);
    }
}

[HarmonyPatch(typeof(RaceEntity), nameof(RaceEntity.GetStartingFloorName))]
public static class Chapter3StartingFloorPatch
{
    public static void Postfix(RaceEntity __instance, ref string __result)
    {
        if (SephiriaMod.GoalChapter != 3)
            return;

        if (__instance.id != 12)
            return;

        bool meet2 =
            SwitchManager.GetDestinySwitch(
                "DeepCave_Hero_Meet_2",
                false
            );

        bool meet2Tree =
            SwitchManager.GetDestinySwitch(
                "DeepCave_Hero_Meet_2_TreeTalk",
                false
            );

        int clearCount =
            SaveManager.Current.GetInt(
                "Chapter3ClearCount",
                0
            );

        if (meet2 && meet2Tree && clearCount == 2)
        {
            __result = "TheRabbittown";
        }
    }
}

[HarmonyPatch(
    typeof(UnitAI_QBossAdv),
    "RpcChapter5ClearCutSceneStart"
)]
public static class Chapter5ClearPatch
{
    public static bool Prefix(UnitAI_QBossAdv __instance)
    {
        if (SephiriaMod.GoalChapter != 5)
            return true;

        if (DungeonManager.Instance == null ||
            DungeonManager.Instance.raceId != 26)
        {
            return true;
        }

        if (!__instance.isServer)
            return true;

        MelonLogger.Msg(
            "[DEBUG] Chapter 5 Qliphoth defeated."
        );

        var weaponController =
            UnityEngine.Object.FindObjectOfType<WeaponControllerSimple>();

        if (weaponController == null ||
            weaponController.currentWeapon == null)
        {
            MelonLogger.Warning(
                "Chapter 5 cleared, but current weapon could not be determined."
            );

            return true;
        }

        bool goalCompleted =
            SephiriaMod.RecordChapterClear(
                weaponController.currentWeapon.weaponType
            );

        if (goalCompleted)
        {
            MelonLogger.Msg(
                "AP Goal requirements completed - allowing normal Chapter 5 ending."
            );

            SephiriaMod.ArchipelagoGoalPending = true;

            return true;
        }

        MelonLogger.Msg(
            "Chapter 5 AP clear recorded - delaying player death."
        );

        MelonCoroutines.Start(DelayedPlayerDeath());

        // Stop Sephiria from starting the normal Chapter 5 ending.
        return false;
    }

    private static IEnumerator DelayedPlayerDeath()
    {
        // Let Qliphoth's defeated state sit on screen briefly,
        // so it is obvious the player won the fight.
        yield return new UnityEngine.WaitForSeconds(1.5f);

        foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
        {
            if ((bool)player &&
                (bool)player.PlayerAvatar)
            {
                player.PlayerAvatar.Die(5, null);
            }
        }
    }
}

[HarmonyPatch(
    typeof(BossEnvironment_QQBoss),
    nameof(BossEnvironment_QQBoss.DefeatBossUI)
)]
public static class Chapter6ClearPatch
{
    public static bool Prefix()
    {
        if (SephiriaMod.GoalChapter != 6)
            return true;

        if (DungeonManager.Instance == null ||
            DungeonManager.Instance.raceId != 28)
        {
            return true;
        }

        // Only the host should record the AP clear.
        if (!Mirror.NetworkServer.active)
            return true;

        var weaponController =
            UnityEngine.Object.FindObjectOfType<WeaponControllerSimple>();

        if (weaponController == null ||
            weaponController.currentWeapon == null)
        {
            MelonLogger.Warning(
                "Chapter 6 cleared, but current weapon could not be determined."
            );

            return true;
        }

        bool goalCompleted =
            SephiriaMod.RecordChapterClear(
                weaponController.currentWeapon.weaponType
            );

        if (goalCompleted)
        {
            MelonLogger.Msg(
                "AP Goal requirements completed - allowing normal Chapter 6 ending."
            );

            SephiriaMod.ArchipelagoGoalPending = true;

            // Allow Sephiria to set Chapter6_DefeatBoss
            // and continue the normal ending.
            return true;
        }

        MelonLogger.Msg(
            "Chapter 6 AP clear recorded - ending intercepted for another AP run."
        );

        foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
        {
            if ((bool)player &&
                (bool)player.PlayerAvatar)
            {
                player.PlayerAvatar.Die(5, null);
            }
        }

        // Do NOT let Sephiria set Chapter6_DefeatBoss.
        return false;
    }
}

[HarmonyPatch(typeof(Desert_Chapter2Event), nameof(Desert_Chapter2Event.OnStartServer))]
public static class Chapter2EndRoomPatch
{
    public static void Postfix()
    {
        if (SephiriaMod.GoalChapter != 2)
        {
            return;
        }

        SephiriaMod.Chapter2EndingReached = true;
    }
}

[HarmonyPatch(typeof(PlayerSpawner), nameof(PlayerSpawner.RpcGameOver))]
public static class Chapter2GameOverPatch
{
    public static bool Prefix(PlayerSpawner __instance)
    {
        if (SephiriaMod.GoalChapter != 2 ||
            !SephiriaMod.Chapter2EndingReached)
        {
            return true;
        }

        SephiriaMod.Chapter2EndingReached = false;

        var weaponController =
            __instance.GetComponent<WeaponControllerSimple>();

        if (weaponController == null ||
            weaponController.currentWeapon == null)
        {
            MelonLogger.Warning(
                "Chapter 2 cleared, but current weapon could not be determined."
            );
            return true;
        }

        bool goalCompleted = SephiriaMod.RecordChapterClear(
            weaponController.currentWeapon.weaponType
        );

        if (goalCompleted)
        {
            SephiriaMod.ArchipelagoGoalPending = true;

            var notification =
                UIManager.Instance.GetElement<UI_MainQuestClearNoti>();

            AccessTools.Method(
                typeof(UI_MainQuestClearNoti),
                "SetMainQuestProgress"
            ).Invoke(notification, new object[] { 4 });
        }
        else
        {
            SwitchManager.SetDestinySwitch(
               "Chapter2Clear",
               false
           );

            SaveManager.Save(
                saveCurrent: true,
                saveCurrentRun: false
            );

            __instance.PlayerAvatar.Die(5, null);
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(DungeonStair_Chapter3End), "UserCode_RpcEnd")]
public static class Chapter3EndPatch
{
    public static bool Prefix()
    {
        if (SephiriaMod.GoalChapter != 3)
            return true;

        var weaponController =
            UnityEngine.Object.FindObjectOfType<WeaponControllerSimple>();

        if (weaponController == null ||
            weaponController.currentWeapon == null)
        {
            MelonLogger.Warning(
                "Chapter 3 cleared, but current weapon could not be determined."
            );

            return true;
        }

        bool goalCompleted = SephiriaMod.RecordChapterClear(
            weaponController.currentWeapon.weaponType
        );

        if (goalCompleted)
        {
            MelonLogger.Msg(
                "AP Goal requirements completed - allowing normal Chapter 3 ending."
            );

            SephiriaMod.ArchipelagoGoalPending = true;
            return true;
        }

        MelonLogger.Msg(
            "AP Goal requirements not yet completed - treating Chapter 3 clear as a normal death."
        );

        foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
        {
            if ((bool)player)
                player.PlayerAvatar.Die(5, null);
        }

        return false;
    }
}

[HarmonyPatch(typeof(DungeonStair_Chapter3End), "UserCode_RpcEnd")]
public static class Chapter4EndPatch
{
    public static bool Prefix(DungeonStair_Chapter3End __instance)
    {
        if (SephiriaMod.GoalChapter != 4)
            return true;

        if (__instance.clearCountBoolKey != "Chapter4ClearCount")
            return true;

        var weaponController =
            UnityEngine.Object.FindObjectOfType<WeaponControllerSimple>();

        if (weaponController == null ||
            weaponController.currentWeapon == null)
        {
            MelonLogger.Warning(
                "Chapter 4 cleared, but current weapon could not be determined."
            );

            return false;
        }

        bool goalCompleted =
            SephiriaMod.RecordChapterClear(
                weaponController.currentWeapon.weaponType
            );

        if (goalCompleted)
        {
            MelonLogger.Msg(
                "AP Goal requirements completed - allowing normal Chapter 4 ending."
            );

            SephiriaMod.ArchipelagoGoalPending = true;

            return true;
        }

        MelonLogger.Msg(
            "AP Goal requirements not yet completed - treating Chapter 4 clear as a normal death."
        );

        foreach (PlayerSpawner player in PlayerSpawner.MultiplayerList)
        {
            if ((bool)player)
            {
                player.PlayerAvatar.Die(5, null);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(UI_MainQuestClearNoti), "SetMainQuestProgress")]
public static class Chapter2CompleteNotificationPatch
{
    public static bool Prefix(int progress)
    {
        if (SephiriaMod.GoalChapter == 2 &&
            progress == 4 &&
            SephiriaMod.CompletedChapterClears <
            SephiriaMod.RequiredChapterClears)
        {
            return false;
        }

        return true;
    }
}

[HarmonyPatch(
    typeof(PlayerLocalDataStorage),
    nameof(PlayerLocalDataStorage.ChangeChapter)
)]
public static class ChangeChapterBlockPatch
{
    public static bool Prefix(int chapter)
    {
        if ((SephiriaMod.GoalChapter == 4 ||
             SephiriaMod.GoalChapter == 5 ||
             SephiriaMod.GoalChapter == 6) &&
            SephiriaMod.CompletedChapterClears <
            SephiriaMod.RequiredChapterClears &&
            chapter != SephiriaMod.GoalChapter)
        {
            return false;
        }

        return true;
    }
}
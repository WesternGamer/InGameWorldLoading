using HarmonyLib;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.Utils;
using VRageMath;
using static Sandbox.Graphics.GUI.MyGuiScreenMessageBox;

namespace InGameWorldLoading.Patches
{
    [HarmonyPatch(typeof(MyGuiScreenMainMenu), "CreateInGameMenu")]
    internal class PauseMenu_Patch
    {
        private static MyGuiScreenMainMenu Instance;

        private static readonly MethodInfo onNewGameMethod = AccessTools.Method(typeof(MyGuiScreenMainMenu), "OnNewGame");

        private static readonly MethodInfo onJoinWorldMethod = AccessTools.Method(typeof(MyGuiScreenMainMenu), "OnJoinWorld");

        private static void Postfix(MyGuiScreenMainMenu __instance)
        {
            Instance = __instance;
            Vector2 minSizeGui = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;
            Vector2 leftButtonPositionOrigin = MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM) + new Vector2(minSizeGui.X / 2f, 0f) + new Vector2(15f, 0f) / MyGuiConstants.GUI_OPTIMAL_SIZE;
            float buttonsYAxis = 0.873f;

            if (IsPluginLoaded() == true)
            {
                buttonsYAxis -= 0.06f;
            }

            MyGuiControlButton newGameButton = new MyGuiControlButton(new Vector2(leftButtonPositionOrigin.X + 0.2f, buttonsYAxis), MyGuiControlButtonStyleEnum.Default, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, text: MyTexts.Get(MyCommonTexts.ScreenMenuButtonCampaign), onButtonClick: OpenNewGameMenu)
            {
                BorderEnabled = false,
                BorderSize = 0,
                BorderHighlightEnabled = false,
                BorderColor = Vector4.Zero
            };
            __instance.Controls.Add(newGameButton);
            //+0.06
            MyGuiControlButton loadGameButton = new MyGuiControlButton(new Vector2(leftButtonPositionOrigin.X + 0.2f, buttonsYAxis + 0.06f), MyGuiControlButtonStyleEnum.Default, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, text: MyTexts.Get(MyCommonTexts.ScreenMenuButtonLoadGame), onButtonClick: OpenLoadGameMenu)
            {
                BorderEnabled = false,
                BorderSize = 0,
                BorderHighlightEnabled = false,
                BorderColor = Vector4.Zero
            };
            __instance.Controls.Add(loadGameButton);
            //+0.06
            MyGuiControlButton joinGameButton = new MyGuiControlButton(new Vector2(leftButtonPositionOrigin.X + 0.2f, buttonsYAxis + 0.12f), MyGuiControlButtonStyleEnum.Default, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, text: MyTexts.Get(MyCommonTexts.ScreenMenuButtonJoinGame), onButtonClick: OpenJoinGameMenu)
            {
                BorderEnabled = false,
                BorderSize = 0,
                BorderHighlightEnabled = false,
                BorderColor = Vector4.Zero
            };
            __instance.Controls.Add(joinGameButton);

        }

        private static void OpenNewGameMenu(MyGuiControlButton button)
        {
            //Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                onNewGameMethod.Invoke(Instance, null);
                return;
            }

            ShowSaveMenu(delegate 
            { 
                onNewGameMethod.Invoke(Instance, null); 
            }, SaveMenuModes.NewGame);
        }

        private static void OpenLoadGameMenu(MyGuiControlButton button)
        {
            //Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                MyGuiSandbox.AddScreen(new MyGuiScreenLoadSandbox());
                return;
            }

            ShowSaveMenu(delegate 
            { 
                MyGuiSandbox.AddScreen(new MyGuiScreenLoadSandbox()); 
            }, SaveMenuModes.LoadGame);
        }

        private static void OpenJoinGameMenu(MyGuiControlButton button)
        {
            //Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                onJoinWorldMethod.Invoke(Instance, null);
                return;
            }
            ShowSaveMenu(delegate 
            { 
                onJoinWorldMethod.Invoke(Instance, null); 
            }, SaveMenuModes.JoinGame);

        }

        private static void ShowSaveMenu(Action afterMenu, SaveMenuModes modes)
        {
            string message = "";
            bool isCampaign = false;
            MyMessageBoxButtonsType buttonsType = MyMessageBoxButtonsType.YES_NO_CANCEL;

            //Sync.IsServer is backwards
            if (Sync.IsServer && !MySession.Static.Settings.EnableSaving)
            {
                message += "Exit from Campaign? All progress from the last checkpoint will be lost.";
                isCampaign = true;
                buttonsType = MyMessageBoxButtonsType.YES_NO;
            }
            else
            {
                if (modes == SaveMenuModes.NewGame)
                {
                    message += "Save changes before loading new world?";
                }
                else if (modes == SaveMenuModes.LoadGame)
                {
                    message += "Save changes before loading world?";
                }
                else if (modes == SaveMenuModes.JoinGame)
                {
                    message += "Save changes before joining world?";
                }
                else
                {
                    message += "Save changes?";
                }
            }

            MyGuiScreenMessageBox saveMenu = MyGuiSandbox.CreateMessageBox(buttonType: buttonsType, messageText: new StringBuilder(message), messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm), callback: ShowSaveMenuCallback);
            saveMenu.SkipTransition = true;
            saveMenu.InstantClose = false;
            MyGuiSandbox.AddScreen(saveMenu);

            void ShowSaveMenuCallback(ResultEnum callbackReturn)
            {
                if (isCampaign)
                {
                    if (callbackReturn == ResultEnum.YES)
                    {
                        afterMenu();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    if (callbackReturn == ResultEnum.YES)
                    {
                        MyAsyncSaving.Start();
                        afterMenu();
                    }
                    else if (callbackReturn == ResultEnum.NO)
                    {
                        afterMenu();
                    }
                    else
                    {
                        return;
                    }
                }
            }
        }

        //Checks if my In Game Exit Plugin is enabled so it's ui won't interfere with this plugin.
        public static bool IsPluginLoaded()
        {
            return AccessTools.TypeByName("InGameExit.Main") != null;
        }

        private enum SaveMenuModes
        {
            NewGame = 0,
            LoadGame = 1,
            JoinGame = 2
        }
    }
}

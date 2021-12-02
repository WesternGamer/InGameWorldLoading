using HarmonyLib;
using Sandbox;
using Sandbox.Engine.Networking;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
using System;
using System.IO;
using System.Text;
using System.Xml;
using VRage;
using VRage.FileSystem;
using VRage.Game;
using VRage.GameServices;
using VRage.Utils;
using VRageMath;
using static Sandbox.Graphics.GUI.MyGuiScreenMessageBox;

namespace InGameWorldLoading.Patches
{
    [HarmonyPatch(typeof(MyGuiScreenMainMenu), "CreateInGameMenu")]
    internal class PauseMenu_Patch
    {
        private static MyGuiScreenMainMenu Instance;
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
                OpenNewGameMenuInternal();
                return;
            }
            ShowSaveMenu(OpenNewGameMenuInternal, SaveMenuModes.NewGame);
            void OpenNewGameMenuInternal()
            {
                if (MySandboxGame.Config.EnableNewNewGameScreen)
                {
                    RunWithTutorialCheck(delegate
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenSimpleNewGame>(Array.Empty<object>()));
                    });
                    return;
                }
                RunWithTutorialCheck(delegate
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateScreen<MyGuiScreenNewGame>(new object[3] { true, true, true }));
                });
            }
        }

        private static void OpenLoadGameMenu(MyGuiControlButton button)
        {
            //Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                OpenLoadGameMenuInternal();
                return;
            }
            ShowSaveMenu(OpenLoadGameMenuInternal, SaveMenuModes.LoadGame);
            void OpenLoadGameMenuInternal()
            {
                RunWithTutorialCheck(delegate
                {
                    MyGuiSandbox.AddScreen(new MyGuiScreenLoadSandbox());
                });
            }
        }

        private static void OpenJoinGameMenu(MyGuiControlButton button)
        {
            //Sync.IsServer is backwards
            if (!Sync.IsServer)
            {
                OpenJoinGameMenuInternal();
                return;
            }
            ShowSaveMenu(OpenJoinGameMenuInternal, SaveMenuModes.JoinGame);

            void OpenJoinGameMenuInternal()
            {
                RunWithTutorialCheck(delegate
                {
                    if (MyGameService.IsOnline)
                    {
                        MyGameService.Service.RequestPermissions(Permissions.Multiplayer, attemptResolution: true, delegate (PermissionResult granted)
                        {
                            switch (granted)
                            {
                                case PermissionResult.Granted:
                                    MyGameService.Service.RequestPermissions(Permissions.UGC, attemptResolution: true, delegate (PermissionResult ugcGranted)
                                    {
                                        switch (ugcGranted)
                                        {
                                            case PermissionResult.Granted:
                                                MyGameService.Service.RequestPermissions(Permissions.CrossMultiplayer, attemptResolution: true, delegate (PermissionResult crossGranted)
                                                {
                                                    MyGuiScreenJoinGame myGuiScreenJoinGame = new MyGuiScreenJoinGame(crossGranted == PermissionResult.Granted);
                                                    myGuiScreenJoinGame.Closed += JoinGameScreenClosed;
                                                    MyGuiSandbox.AddScreen(myGuiScreenJoinGame);
                                                });
                                                break;
                                            case PermissionResult.Error:
                                                MySandboxGame.Static.Invoke(delegate
                                                {
                                                    MyGuiSandbox.Show(MyCommonTexts.XBoxPermission_MultiplayerError, default, MyMessageBoxStyleEnum.Info);
                                                }, "New Game screen");
                                                break;
                                        }
                                    });
                                    break;
                                case PermissionResult.Error:
                                    MyGuiSandbox.Show(MyCommonTexts.XBoxPermission_MultiplayerError, default, MyMessageBoxStyleEnum.Info);
                                    break;
                            }
                        });
                    }
                    else
                    {
                        MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.OK, messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionError), messageText: new StringBuilder().AppendFormat(MyTexts.GetString(MyGameService.IsActive ? MyCommonTexts.SteamIsOfflinePleaseRestart : MyCommonTexts.ErrorJoinSessionNoUser), MySession.GameServiceName)));
                    }
                });
            }
        }

        private static void JoinGameScreenClosed(MyGuiScreenBase source, bool isUnloading)
        {
            if (source.Cancelled)
            {
                Instance.State = MyGuiScreenState.OPENING;
                source.Closed -= JoinGameScreenClosed;
            }
        }

        private static void RunWithTutorialCheck(Action afterTutorial)
        {
            if (MySandboxGame.Config.FirstTimeTutorials)
            {
                MyGuiSandbox.AddScreen(new MyGuiScreenTutorialsScreen(afterTutorial));
            }
            else
            {
                afterTutorial();
            }
        }

        private static void ShowSaveMenu(Action afterMenu, SaveMenuModes modes)
        {
            string message = "";
            if (modes == SaveMenuModes.NewGame)
            {
                message = "Save changes before loading new world?";
            }
            else if (modes == SaveMenuModes.LoadGame)
            {
                message = "Save changes before loading world?";
            }
            else if (modes == SaveMenuModes.JoinGame)
            {
                message = "Save changes before joining world?";
            }
            else
            {
                message = "Save changes?";
            }
            bool isCampaign = false;
            MyMessageBoxButtonsType buttonsType = MyMessageBoxButtonsType.YES_NO_CANCEL;
            if (!Sync.IsServer && !MySession.Static.Settings.EnableSaving)
            {
                message += " All progress from the last checkpoint will be lost.";
                isCampaign = true;
                buttonsType = MyMessageBoxButtonsType.YES_NO;
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
            XmlDocument xml = new XmlDocument();
            xml.Load(Path.Combine(MyFileSystem.ExePath, "Plugins\\config.xml"));

            XmlNodeList xnList = xml.SelectNodes("PluginConfig/Plugins/Id");
            foreach (XmlNode xn in xnList)
            {
                string pluginName = xn.InnerText;

                if (pluginName == "WesternGamer/InGameExit")
                {
                    return true;
                }
            }
            return false;
        }

        enum SaveMenuModes
        {
            NewGame = 0,
            LoadGame = 1,
            JoinGame = 2
        }
    }
}

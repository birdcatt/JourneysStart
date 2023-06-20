using Menu;
using static Menu.SlideShow;
using static Menu.MenuScene;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Debug = UnityEngine.Debug;
using Vector2 = UnityEngine.Vector2;
using System.IO;
using System;

namespace JourneysStart.Shared.ArtOnScreen
{
    public class CutscenesSlideshows
    {
        public static SlideShowID LightpupIntroSlideShow = new("LightpupIntroSlideShow", true);

        public static SceneID LightpupIntroSceneA = new("LightpupIntroSceneA", true);
        //public static SceneID LightpupIntroSceneB;
        //public static SceneID LightpupIntroSceneC;

        public static void Hook()
        {
            IL.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
            IL.Menu.SlideShow.ctor += SlideShow_ctor;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
        }

        public static void SlugcatSelectMenu_StartGame(ILContext il)
        {
            ILCursor c = new(il);
            ILLabel label = il.DefineLabel();

            c.GotoNext(MoveType.Before, i => i.MatchLdarg(1), i => i.MatchLdsfld<SlugcatStats.Name>("White"));

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<SlugcatStats.Name, bool>>(storyGameCharacter =>
            {
                return Plugin.lghtbrpup == storyGameCharacter;
            });
            c.Emit(OpCodes.Brtrue_S, label);

            c.GotoNext(MoveType.After, i => i.MatchLdsfld<SlideShowID>("WhiteIntro"), i => i.MatchStfld<ProcessManager>("nextSlideshow"));

            c.MarkLabel(label);

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate((SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter) =>
            {
                if (Plugin.lghtbrpup == storyGameCharacter)
                {
                    self.manager.nextSlideshow = LightpupIntroSlideShow;
                }
            });
        }
        public static void SlideShow_ctor(ILContext il)
        {
            ILCursor c = new(il);

            c.GotoNext(MoveType.Before, i => i.MatchLdarg(2), i => i.MatchLdsfld<SlideShowID>("WhiteIntro"));

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate((SlideShow self, ProcessManager manager, SlideShowID slideShowID) =>
            {
                if (LightpupIntroSlideShow == slideShowID)
                {
                    Debug.Log($"{Plugin.MOD_NAME}: (Cutscenes) Playing slideshow {LightpupIntroSlideShow.value}");

                    if (manager.musicPlayer != null)
                    {
                        self.waitForMusic = "RW_Intro_Theme";
                        self.stall = true;
                        manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, 40f);
                    }

                    Scene[] scenes =
                    {
                        new Scene(SceneID.Empty, 0f, 0f, 0f),
                        new Scene(LightpupIntroSceneA, self.ConvertTime(0, 0, 20), self.ConvertTime(0, 3, 26), self.ConvertTime(0, 8, 6))
                    };
                    foreach (Scene scene in scenes)
                    {
                        scene.startAt += 0.6f;
                        scene.fadeInDoneAt += 0.6f;
                        scene.fadeOutStartAt += 0.6f;
                        self.playList.Add(scene);
                    }
                    self.processAfterSlideShow = ProcessManager.ProcessID.Game;
                }
            });
        }
        public static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);

            if (LightpupIntroSceneA == self.sceneID)
            {
                self.sceneFolder = GetLightpupScenesFilePath("intro_a");

                //if (self.flatMode)
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "flat", new Vector2(683f, 384f), false, true));
            }
        }

        public static string GetLightpupScenesFilePath(string fileName)
        {
            return GetScenesFilePath("lightpup" + Path.DirectorySeparatorChar.ToString() + fileName);
        }
        public static string GetScenesFilePath(string fileName)
        {
            string filePath = AssetManager.ResolveFilePath("scenes" + Path.DirectorySeparatorChar.ToString() + fileName + Path.DirectorySeparatorChar.ToString() + "flat.png");
            return filePath.Remove(filePath.Length - 1 - "flat.png".Length);
        }
    }
}
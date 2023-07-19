using SlugBase.DataTypes;
using SlugBase.Features;
using SlugBase;
using Colour = UnityEngine.Color;
using Custom = RWCustom.Custom;
using Debug = UnityEngine.Debug;
using JollyCoop.JollyMenu;
using JollyCustom = JollyCoop.JollyCustom;
using JollyColorMode = Options.JollyColorMode;
using System.Runtime.CompilerServices;
using MenuObject = Menu.MenuObject;
using PositionedMenuObject = Menu.PositionedMenuObject;
using MenuIllustration = Menu.MenuIllustration;
using Vector2 = UnityEngine.Vector2;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.IO;

namespace JourneysStart.Shared.ArtOnScreen
{
    public static class JollySelectMenu
    {
        public static ConditionalWeakTable<JollyPlayerSelector, JollyMenuPortraits> ExtraPortraits = new();

        public static void Hook()
        {
            On.PlayerGraphics.PopulateJollyColorArray += PlayerGraphics_PopulateJollyColorArray;
            On.PlayerGraphics.JollyFaceColorMenu += PlayerGraphics_JollyFaceColorMenu;
            On.PlayerGraphics.JollyUniqueColorMenu += PlayerGraphics_JollyUniqueColorMenu;

            On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.HasUniqueSprite += SymbolButtonTogglePupButton_HasUniqueSprite;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += JollyPlayerSelector_GetPupButtonOffName;
            On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.LoadIcon += SymbolButtonTogglePupButton_LoadIcon;
            IL.JollyCoop.JollyMenu.SymbolButtonToggle.LoadIcon += SymbolButtonToggle_LoadIcon;
            On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.Update += SymbolButtonTogglePupButton_Update;

            On.JollyCoop.JollyMenu.JollyPlayerSelector.ctor += JollyPlayerSelector_ctor;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_Update;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.GrafUpdate += JollyPlayerSelector_GrafUpdate;
        }

        #region colours
        public static void PlayerGraphics_PopulateJollyColorArray(On.PlayerGraphics.orig_PopulateJollyColorArray orig, SlugcatStats.Name reference)
        {
            orig(reference);
            //sometimes the auto colours are just ugly
            if (JollyColorMode.AUTO == Custom.rainWorld.options.jollyColorMode)
            {
                if (Plugin.lghtbrpup == reference)
                {
                    PlayerGraphics.jollyColors[1][0] = Custom.hexToColor("e682f3"); //body
                    PlayerGraphics.jollyColors[1][1] = Custom.hexToColor("443047"); //eyes
                }
            }
        }
        public static Colour PlayerGraphics_JollyFaceColorMenu(On.PlayerGraphics.orig_JollyFaceColorMenu orig, SlugcatStats.Name slugName, SlugcatStats.Name reference, int playerNumber)
        {
            Colour val = orig(slugName, reference, playerNumber);
            if (Utility.IsModcat(slugName))
            {
                if (SlugBaseCharacter.TryGet(slugName, out SlugBaseCharacter charac)
                && charac.Features.TryGet(PlayerFeatures.CustomColors, out ColorSlot[] customColours))
                {
                    if (JollyColorMode.DEFAULT == Custom.rainWorld.options.jollyColorMode || JollyColorMode.AUTO == Custom.rainWorld.options.jollyColorMode && 0 == playerNumber)
                        return customColours[1].GetColor(-1);
                }
            }
            return val;
        }
        public static Colour PlayerGraphics_JollyUniqueColorMenu(On.PlayerGraphics.orig_JollyUniqueColorMenu orig, SlugcatStats.Name slugName, SlugcatStats.Name reference, int playerNumber)
        {
            Colour val = orig(slugName, reference, playerNumber);
            if (Utility.IsModcat(slugName))
            {
                if (SlugBaseCharacter.TryGet(slugName, out SlugBaseCharacter charac)
                && charac.Features.TryGet(PlayerFeatures.CustomColors, out ColorSlot[] customColours)
                && customColours.Length > 2)
                {
                    if (JollyColorMode.DEFAULT == Custom.rainWorld.options.jollyColorMode || JollyColorMode.AUTO == Custom.rainWorld.options.jollyColorMode && 0 == playerNumber)
                        return customColours[2].GetColor(-1);
                }
            }
            return val;
        }
        #endregion

        #region pup graphics
        public static bool SymbolButtonTogglePupButton_HasUniqueSprite(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_HasUniqueSprite orig, SymbolButtonTogglePupButton self)
        {
            return orig(self) || self.symbolNameOff.Contains(Plugin.lghtbrpup.value) || self.symbolNameOff.Contains(Plugin.sproutcat.value);
        }
        public static string JollyPlayerSelector_GetPupButtonOffName(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyPlayerSelector self)
        {
            string val = orig(self);
            //if (Utility.IsLightpup(self.JollyOptions(self.index).playerClass))
            //    return Plugin.lghtbrpup + "_pup_off";
            //if (Utility.IsSproutcat(self.JollyOptions(self.index).playerClass))
            //    return Plugin.sproutcat + "_pup_off";
            if (Utility.IsModcat(self.JollyOptions(self.index).playerClass))
                return self.JollyOptions(self.index).playerClass + "_pup_off";
            return val;
        }
        public static void SymbolButtonTogglePupButton_LoadIcon(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_LoadIcon orig, SymbolButtonTogglePupButton self)
        {
            orig(self);
            if (self.uniqueSymbol != null && (self.uniqueSymbol.fileName.Contains(Plugin.lghtbrpup.value) || self.uniqueSymbol.fileName.Contains(Plugin.sproutcat.value)))
            {
                self.uniqueSymbol.pos.y = self.size.y / 2f;
            }
        }
        public static void SymbolButtonToggle_LoadIcon(ILContext il)
        {
            ILCursor c = new(il);
            ILLabel label = il.DefineLabel();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((SymbolButtonToggle self) =>
            {
                return self.symbolNameOff.Contains(Plugin.sproutcat.value) && self.isToggled;
            });
            c.Emit(OpCodes.Brfalse, label);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((SymbolButtonToggle self) =>
            {
                self.symbol.fileName = "sproutcat_pup_on";
                self.symbol.LoadFile();
                self.symbol.sprite.SetElementByName(self.symbol.fileName);
                self.symbol.fileName = self.symbolNameOn; //otherwise it forces you to be a pup in menu
            });
            c.Emit(OpCodes.Ret);

            c.MarkLabel(label);
        }
        public static void SymbolButtonTogglePupButton_Update(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_Update orig, SymbolButtonTogglePupButton self)
        {
            if (self.symbolNameOff.Contains(Plugin.sproutcat.value))
            {
                if (self.isToggled)
                {
                    if ((null == self.uniqueSymbol || self.uniqueSymbol.fileName.Contains("off")) //catches if its using the wrong unique sprite
                    && !(null != self.uniqueSymbol && "unique_sproutcat_pup_on" == self.uniqueSymbol.fileName) /*not already using the unique pup sprite*/)
                    {
                        //self.symbolNameOn is "pup_on", not "sproutcat_pup_on" or smth

                        if (null != self.uniqueSymbol)
                            self.RemoveUniqueSymbol();

                        self.uniqueSymbol = new MenuIllustration(self.menu, self, "", "unique_sproutcat_pup_on", self.size / 2f, true, true);
                        self.subObjects.Add(self.uniqueSymbol);
                        self.uniqueSymbol.LoadFile();
                        self.uniqueSymbol.sprite.SetElementByName(self.uniqueSymbol.fileName);
                    }
                }
                else if (null != self.uniqueSymbol)
                {
                    //just clean up, it should fix itself later in orig
                    self.RemoveUniqueSymbol();
                }
            }
            orig(self);
        }
        public static void RemoveUniqueSymbol(this SymbolButtonTogglePupButton self)
        {
            self.uniqueSymbol.RemoveSprites();
            self.subObjects.Remove(self.uniqueSymbol);
            self.uniqueSymbol = null;
        }
        #endregion

        #region custom portraits
        public static void JollyPlayerSelector_ctor(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_ctor orig, JollyPlayerSelector self, JollySetupDialog menu, MenuObject owner, Vector2 pos, int index)
        {
            orig(self, menu, owner, pos, index);

            if (!ExtraPortraits.TryGetValue(self, out _))
                ExtraPortraits.Add(self, new JollyMenuPortraits(self, menu, self.portrait.pos, owner));
        }
        public static void JollyPlayerSelector_Update(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyPlayerSelector self)
        {
            orig(self);

            if (ExtraPortraits.TryGetValue(self, out JollyMenuPortraits p))
            {
                SlugcatStats.Name currentSlug = JollyCustom.SlugClassMenu(self.index, self.dialog.currentSlugcatPageName);
                if (Utility.IsModcat(currentSlug) && (JollyColorMode.CUSTOM == self.dialog.Options.jollyColorMode || JollyColorMode.AUTO == self.dialog.Options.jollyColorMode && self.index != 0))
                {
                    self.portrait.sprite.isVisible = false;
                    p.EnableAllPortraits(currentSlug, new SlugcatStats.Name("JollyPlayer" + (self.index + 1).ToString(), false), self.JollyOptions(0).playerClass, self.index);
                }
                else
                {
                    self.portrait.sprite.isVisible = true;
                    p.DisableAllPortraits();
                }
            }
        }
        public static void JollyPlayerSelector_GrafUpdate(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GrafUpdate orig, JollyPlayerSelector self, float timeStacker)
        {
            orig(self, timeStacker);

            if (ExtraPortraits.TryGetValue(self, out JollyMenuPortraits p) && p.PortraitsEnabled)
            {
                p.Body.sprite.color = self.FadePortraitSprite(self.bodyTintColor, timeStacker);
                p.Eyes.sprite.color = self.FadePortraitSprite(self.faceTintColor, timeStacker);
                p.Unique.sprite.color = self.FadePortraitSprite(self.uniqueTintColor, timeStacker);
                p.Extra.sprite.color = self.FadePortraitSprite(Colour.white, timeStacker);
            }
        }
        #endregion
    }

    public class JollyMenuPortraits : PositionedMenuObject
    {
        public MenuIllustration Body;
        public MenuIllustration Eyes;
        public MenuIllustration Unique;
        public MenuIllustration Extra;
        public MenuIllustration Background;
        public bool PortraitsEnabled;

        public JollyMenuPortraits(JollyPlayerSelector self, JollySetupDialog menu, Vector2 pos, MenuObject owner) : base(menu, owner, pos)
        {
            Background = new(menu, self, "", "jollyicon_background", pos, true, true);
            Body = new(menu, self, "", "jollyicon_body-lightbringer", pos, true, true);
            Unique = new(menu, self, "", "jollyicon_unique-lightbringer", pos, true, true);
            Eyes = new(menu, self, "", "jollyicon_eyes-lightbringer", pos, true, true);
            Extra = new(menu, self, "", "jollyicon_extra-lightbringer", pos, true, true);

            AddAllPortraits();
            PortraitsEnabled = true; //so DisableAllPortraits can run correctly
            DisableAllPortraits();
        }
        ~JollyMenuPortraits()
        {
            RemoveAllPortraits();
        }

        public bool IsCorrectSlug(SlugcatStats.Name slug)
        {
            return Body.fileName.Contains(slug.value);
        }

        public void EnableAllPortraits(SlugcatStats.Name actualSlug, SlugcatStats.Name jollySlug, SlugcatStats.Name reference, int index)
        {
            //jollySlug is like jollyplayer4 or something
            //colours called with Default/Auto, so reference is 1st jolly player
            LoadAllPortraits(actualSlug);
            EnableAllPortraits(jollySlug, reference, index);
        }
        public void EnableAllPortraits(SlugcatStats.Name jollySlug, SlugcatStats.Name reference, int index)
        {
            PortraitsEnabled = true;

            SetPortrait(ref Body, PlayerGraphics.JollyBodyColorMenu(jollySlug, reference));
            SetPortrait(ref Unique, PlayerGraphics.JollyUniqueColorMenu(jollySlug, reference, index));
            SetPortrait(ref Eyes, PlayerGraphics.JollyFaceColorMenu(jollySlug, reference, index));
            SetPortrait(ref Extra);
            SetPortrait(ref Background);
        }
        public void DisableAllPortraits()
        {
            if (!PortraitsEnabled)
                return;

            PortraitsEnabled = false;

            Body.sprite.isVisible = false;
            Eyes.sprite.isVisible = false;
            Unique.sprite.isVisible = false;
            Extra.sprite.isVisible = false;
            Background.sprite.isVisible = false;
        }
        public void AddAllPortraits()
        {
            owner.subObjects.Add(Background);
            owner.subObjects.Add(Body);
            owner.subObjects.Add(Unique);
            owner.subObjects.Add(Eyes); //display Eyes above Unique
            owner.subObjects.Add(Extra);
        }
        public void RemoveAllPortraits()
        {
            owner.subObjects.Remove(Background);
            owner.subObjects.Remove(Body);
            owner.subObjects.Remove(Eyes);
            owner.subObjects.Remove(Unique);
            owner.subObjects.Remove(Extra);
        }
        public void LoadAllPortraits(SlugcatStats.Name slug)
        {
            if (IsCorrectSlug(slug))
                return;

            LoadPortrait(ref Body, slug);
            LoadPortrait(ref Eyes, slug);
            LoadPortrait(ref Unique, slug);
            LoadPortrait(ref Extra, slug);
        }

        public void LoadPortrait(ref MenuIllustration portrait, SlugcatStats.Name slug)
        {
            string fileName = portrait.fileName.Remove(portrait.fileName.IndexOf("-") + 1) + slug.value;

            if (!File.Exists(AssetManager.ResolveFilePath("illustrations" + Path.DirectorySeparatorChar.ToString() + fileName + ".png")))
            {
                portrait.sprite.isVisible = false;
                return;
            }

            portrait.fileName = fileName;
            portrait.LoadFile();
            portrait.sprite.SetElementByName(portrait.fileName);
        }
        public void SetPortrait(ref MenuIllustration portrait, Colour colour)
        {
            SetPortrait(ref portrait);
            portrait.sprite.color = colour;
        }
        public void SetPortrait(ref MenuIllustration portrait)
        {
            portrait.sprite.isVisible = true;
        }
    }
}

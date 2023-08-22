using static SSOracleBehavior;
using static SLOracleBehaviorHasMark;
using CreatureTemplateType = MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType;
using Colour = UnityEngine.Color;
using Mathf = UnityEngine.Mathf;
using Vector2 = UnityEngine.Vector2;
using Debug = UnityEngine.Debug;
using static JourneysStart.Slugcats.Lightbringer.MiscData.FRDData;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using Random = UnityEngine.Random;
using Exception = System.Exception;
using static JourneysStart.Plugin;
using JourneysStart.FisobsItems.Taser;
using SlugBase.DataTypes;
using SlugBase.Features;
using SlugBase;
using RWCustom;
using JourneysStart.Slugcats.Outgrowth.PlayerStuff;
using JourneysStart.Slugcats.Lightbringer.PlayerStuff;
using JourneysStart.Slugcats.Strawberry;

namespace JourneysStart
{
    public static class Utility
    {
        public static bool TryGetLightpup(this Player player, out LightpupData lightpup)
        {
            bool isLightpup = PlayerDataCWT.TryGetValue(player, out var pData) && pData.IsLightpup;
            lightpup = isLightpup ? pData.Lightpup : null;
            return isLightpup;
        }
        public static bool TryGetSproutcat(this Player player, out OutgrowthData sprout)
        {
            bool isSprout = PlayerDataCWT.TryGetValue(player, out var pData) && pData.IsSproutcat;
            sprout = isSprout ? pData.Sproutcat : null;
            return isSprout;
        }
        public static bool TryGetStrawberry(this Player player, out StrawberryData strawb)
        {
            bool isStrawb = PlayerDataCWT.TryGetValue(player, out var pData) && pData.IsStrawberry;
            strawb = isStrawb ? pData.Strawberry : null;
            return isStrawb;
        }

        public static bool IsStrawbBlue(this Player player)
        {
            if (player.room == null) return false;
            return IsStrawberry(player.room.game.StoryCharacter) && player.abstractCreature.ID == StrawbBlueID;
        }
        public static bool IsStrawbYellow(this Player player)
        {
            if (player.room == null) return false;
            return IsStrawberry(player.room.game.StoryCharacter) && player.abstractCreature.ID == StrawbYellowID;
        }

        #region slug checks
        public static bool IsModcat(SlugcatStats.Name slugName)
        {
            return null != slugName && (sproutcat == slugName || lghtbrpup == slugName || strawberry == slugName);
        }

        public static bool IsLightpup(SlugcatStats.Name slugName)
        {
            return null != slugName && lghtbrpup == slugName;
        }

        public static bool IsSproutcat(SlugcatStats.Name slugName)
        {
            return null != slugName && sproutcat == slugName;
        }
        public static bool IsSproutcat(Creature crit)
        {
            return crit is Player player && IsSproutcat(player);
        }
        public static bool IsSproutcat(Player player)
        {
            return null != player && IsSproutcat(player.slugcatStats.name);
        }
        public static bool IsStrawberry(SlugcatStats.Name name)
        {
            return name != null && strawberry == name;
        }
        #endregion

        #region debug
        public static bool ProgressionUnlocked(RainWorldGame game)
        {
            return ProgressionUnlocked(game.session);
        }
        public static bool ProgressionUnlocked(GameSession session)
        {
            return session is StoryGameSession story && ProgressionUnlocked(story);
        }
        public static bool ProgressionUnlocked(StoryGameSession story)
        {
            if (lghtbrpup == story.game.StoryCharacter)
            {
                SLOrcacleState SLOracleState = story.saveState.miscWorldSaveData.SLOracleState; //lmao orcacle
                return Lightpup_Debug_UnlockProgression.TryGet(story.game, out bool yeah) && yeah || SLOracleState.playerEncounters > 0 && SLOracleState.significantPearls.Contains(LightpupPearl);
            }

            if (sproutcat == story.game.StoryCharacter)
            {
                return Sprout_Debug_UnlockProgression.TryGet(story.game, out bool yeah) && yeah || 9 == story.saveState.deathPersistentSaveData.karmaCap;
            }

            return false;
        }
        #endregion

        #region crafting
        public static void SpawnItem(Room room, AbstractPhysicalObject item)
        {
            Debug.Log($"{MOD_NAME}: Realizing abstract object {item.type}");
            room.abstractRoom.AddEntity(item);
            item.RealizeInRoom();
        }
        public static void SpawnItemInHand(Player self, AbstractPhysicalObject item)
        {
            SpawnItem(self.room, item);
            int freeHand = self.FreeHand();
            if (-1 != freeHand)
            {
                Debug.Log($"{MOD_NAME}: Putting {item.type} in player grasp {freeHand}");
                self.SlugcatGrab(item.realizedObject, freeHand);
            }
        }
        public static void SpawnItemInHand(Player self, AbstractPhysicalObject item, int index)
        {
            SpawnItem(self.room, item);

            if (index < 0 || -1 == self.FreeHand() || index > self.grasps.Length || self.grasps[index] != null)
            {
                //attempt to spawn in next available free hand
                int freeHand = self.FreeHand();
                if (-1 != freeHand)
                {
                    Debug.Log($"{MOD_NAME}: Putting {item.type} in player grasp {freeHand}");
                    self.SlugcatGrab(item.realizedObject, freeHand);
                }
                return;
            }

            Debug.Log($"{MOD_NAME}: Putting {item.type} in player grasp {index}");
            self.SlugcatGrab(item.realizedObject, index);
        }
        public static void AddCraftingLight(Player self, float duration = 200f, int lifetime = 6)
        {
            if (!ConfigMenu.removeCraftingLight.Value)
                self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos, duration, 1f, lifetime, new Colour(0.7f, 1f, 1f)));
        }
        public static void AddCraftingSpark(Player self)
        {
            //from electric spear's Spark method
            for (int i = 0; i < 10; i++)
            {
                Vector2 a = RWCustom.Custom.RNV();
                self.room.AddObject(new Spark(self.firstChunk.pos + a * Random.value * 20f, a * Mathf.Lerp(4f, 10f, Random.value), Colour.white, null, 4, 18));
            }
        }
        #endregion

        #region items
        public static void SetChargeDependantElectricColour(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, int index, int electricCharge)
        {
            //using FSprite instead of SpriteLeaser and index doesn't work for electric spears
            if (3 < electricCharge || electricCharge < 0) //3 should be the max charge
                return;

            sLeaser.sprites[index].color = Colour.Lerp(sLeaser.sprites[index].color, rCam.currentPalette.blackColor, 0.33f * (3 - electricCharge));

            if (0 == electricCharge)
                return;

            Colour myDude = sLeaser.sprites[index].color;
            sLeaser.sprites[index].color = new Colour(myDude.r, myDude.g, myDude.b + 0.1f); //more blue
        }
        #endregion

        #region player graphics
        public static Colour GetColour(Player player, int index)
        {
            //only works for body, eye, and unique colour
            Colour colour = Colour.grey;
            bool jollyDefaultColourMode = ModManager.CoopAvailable && Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.DEFAULT;
            int playerNumber = player.room.game.session is not ArenaGameSession && (player.playerState.playerNumber == 0 || jollyDefaultColourMode) ? -1 : player.playerState.playerNumber;

            if (SlugBaseCharacter.TryGet(player.slugcatStats.name, out SlugBaseCharacter charac)
                && charac.Features.TryGet(PlayerFeatures.CustomColors, out ColorSlot[] customColours))
            {
                //loading default colours)
                colour = customColours[index].GetColor(playerNumber);
            }

            if (PlayerGraphics.customColors != null && !player.IsJollyPlayer && !ModManager.JollyCoop)
            {
                if (PlayerGraphics.customColors.Count > index)
                    colour = PlayerGraphics.CustomColorSafety(index);
            }
            else if (ModManager.CoopAvailable)
            {
                if (playerNumber > 0)
                {
                    //why is p1 null. entire reason i have to check for player and colour mode
                    colour = PlayerGraphics.JollyColor(playerNumber, index);
                }
                else if (Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.CUSTOM)
                {
                    //these are the jolly custom colours btw
                    JollyCoop.JollyMenu.JollyPlayerOptions jollyPlayer = Custom.rainWorld.options.jollyPlayerOptionsArray[0];
                    switch (index)
                    {
                        case 0:
                            colour = jollyPlayer.GetBodyColor();
                            break;
                        case 1:
                            colour = jollyPlayer.GetFaceColor();
                            break;
                        case 2:
                            colour = jollyPlayer.GetUniqueColor();
                            break;
                    }
                }
            }
            return colour;
        }

        public static Colour? GetColour(PlayerGraphics pGraf, PlayerColor playerColour) => playerColour.GetColor(pGraf);

        public static Colour GetMalnourishedColour(this PlayerGraphics self, Colour colour)
        {
            if (self.malnourished > 0f)
            {
                float num = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                colour = Colour.Lerp(colour, Colour.gray, 0.4f * num);
            }
            return self.HypothermiaColorBlend(colour);
        }
        public static Colour? GetMalnourishedColour(this PlayerGraphics self, SlugBase.DataTypes.PlayerColor playerColour)
        {
            var colour = playerColour.GetColor(self);

            if (colour == null)
                return null;

            if (self.malnourished > 0f)
            {
                float num = self.player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                colour = Colour.Lerp((Colour)colour, Colour.gray, 0.4f * num);
            }
            return self.HypothermiaColorBlend((Colour)colour);
        }
        #endregion

        #region outgrowth
        public static bool EdibleIsBug(IPlayerEdible eatenobject)
        {
            return eatenobject is Fly || eatenobject is SmallNeedleWorm || eatenobject is VultureGrub || eatenobject is Centipede;
        }
        #endregion

        #region lightpup
        public static bool CreatureIsRot(CreatureTemplate.Type critType)
        {
            return CreatureTemplate.Type.DaddyLongLegs == critType || CreatureTemplate.Type.BrotherLongLegs == critType || ModManager.MSC && CreatureTemplateType.TerrorLongLegs == critType;
        }

        #region item checks
        public static bool GraspIsTaserWithCharge(Creature.Grasp obj)
        {
            return obj.grabbed.abstractPhysicalObject is TaserAbstract taser && taser.electricCharge > 0;
        }
        public static bool GraspIsSpearWithCharge(Creature.Grasp obj)
        {
            return obj.grabbed.abstractPhysicalObject is AbstractSpear spear && spear.electric && spear.electricCharge > 0;
        }
        public static bool GraspIsFullElectric(Creature.Grasp obj)
        {
            return GraspIsElectricSpearFullCharge(obj) || GraspIsTaserFullCharge(obj);
        }
        public static bool GraspIsTaserFullCharge(Creature.Grasp obj)
        {
            return obj.grabbed.abstractPhysicalObject is TaserAbstract taser && taser.electricCharge >= 3;
        }
        public static bool GraspIsElectricSpearFullCharge(Creature.Grasp obj)
        {
            return obj.grabbed.abstractPhysicalObject is AbstractSpear spear && spear.electric && spear.electricCharge >= 3;
        }
        #endregion
        #endregion

        #region oracle
        #region oracle saying stuff
        public static void AddMessage(this Conversation self, string text, int initialWait = 0, int textLinger = 0)
        {
            self.events.Add(new Conversation.TextEvent(self, initialWait, text, textLinger));
        }
        public static void PlayConvo(this PebblesConversation self, string[][] convo)
        {
            for (int i = 0; i < convo.Length; i++)
            {
                for (int j = 0; j < convo[i].Length; j++)
                {
                    if (i == 0 && j == 0)
                        AddMessage(self, convo[i][j]);
                    else
                        AddMessage(self, convo[i][j], textLinger: 20);
                }
            }
        }
        public static void PlayConvo(this MoonConversation self, string[] convo, int textLinger)
        {
            foreach (string i in convo)
            {
                AddMessage(self, i, textLinger: textLinger);
            }
        }
        #endregion

        public static void LoadEventsFromFile(this Conversation self, string fileName, SlugcatStats.Name saveFile = null, bool oneRandomLine = false, int randomSeed = 0)
        {
            Debug.Log("~~~LOAD CONVO " + fileName);
            InGameTranslator.LanguageID languageID = self.interfaceOwner.rainWorld.inGameTranslator.currentLanguage;
            string text;
            while (true)
            {
                text = AssetManager.ResolveFilePath(self.interfaceOwner.rainWorld.inGameTranslator.SpecificTextFolderDirectory(languageID) + Path.DirectorySeparatorChar.ToString() + fileName + ".txt");
                if (saveFile != null)
                {
                    string text2 = text;
                    text = AssetManager.ResolveFilePath(string.Concat(new string[]
                    {
                        self.interfaceOwner.rainWorld.inGameTranslator.SpecificTextFolderDirectory(languageID),
                        Path.DirectorySeparatorChar.ToString(),
                        fileName,
                        "-",
                        saveFile.value,
                        ".txt"
                    }));
                    if (!File.Exists(text))
                    {
                        text = text2;
                    }
                }
                if (File.Exists(text))
                {
                    goto ReadFile;
                }
                Debug.Log("NOT FOUND " + text);
                if (languageID == InGameTranslator.LanguageID.English)
                {
                    break;
                }
                Debug.Log("RETRY WITH ENGLISH");
                languageID = InGameTranslator.LanguageID.English;
            }
            return;
        ReadFile:
            string text3 = File.ReadAllText(text, Encoding.UTF8);
            if (text3.Length == 0) //idk i hope it doesnt crash with empty text files now
                return;
            //still requires something as the first line in the file to properly read
            string[] array = Regex.Split(text3, "\r\n");
            try
            {
                if (oneRandomLine)
                {
                    List<Conversation.TextEvent> list = new();
                    for (int i = 1; i < array.Length; i++)
                    {
                        string[] array2 = LocalizationTranslator.ConsolidateLineInstructions(array[i]);
                        if (array2.Length == 3)
                        {
                            list.Add(new Conversation.TextEvent(self, int.Parse(array2[0], NumberStyles.Any, CultureInfo.InvariantCulture), array2[2], int.Parse(array2[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                        }
                        else if (array2.Length == 1 && array2[0].Length > 0)
                        {
                            list.Add(new Conversation.TextEvent(self, 0, array2[0], 0));
                        }
                    }
                    if (list.Count > 0)
                    {
                        Random.State state = Random.state;
                        Random.InitState(randomSeed);
                        Conversation.TextEvent item = list[Random.Range(0, list.Count)];
                        Random.state = state;
                        self.events.Add(item);
                    }
                }
                else
                {
                    for (int j = 1; j < array.Length; j++)
                    {
                        string[] array3 = LocalizationTranslator.ConsolidateLineInstructions(array[j]);
                        if (array3.Length == 3)
                        {
                            if (ModManager.MSC && !int.TryParse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture, out int num) && int.TryParse(array3[2], NumberStyles.Any, CultureInfo.InvariantCulture, out int num2))
                            {
                                self.events.Add(new Conversation.TextEvent(self, int.Parse(array3[0], NumberStyles.Any, CultureInfo.InvariantCulture), array3[1], int.Parse(array3[2], NumberStyles.Any, CultureInfo.InvariantCulture)));
                            }
                            else
                            {
                                self.events.Add(new Conversation.TextEvent(self, int.Parse(array3[0], NumberStyles.Any, CultureInfo.InvariantCulture), array3[2], int.Parse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                            }
                        }
                        else if (array3.Length == 2)
                        {
                            if (array3[0] == "SPECEVENT")
                            {
                                self.events.Add(new Conversation.SpecialEvent(self, 0, array3[1]));
                            }
                            else if (array3[0] == "PEBBLESWAIT")
                            {
                                self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, null, int.Parse(array3[1], NumberStyles.Any, CultureInfo.InvariantCulture)));
                            }
                        }
                        else if (array3.Length == 1 && array3[0].Length > 0)
                        {
                            self.events.Add(new Conversation.TextEvent(self, 0, array3[0], 0));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("TEXT ERROR");
                self.events.Add(new Conversation.TextEvent(self, 0, "TEXT ERROR", 100));
                Debug.LogException(e);
            }
        }
        #endregion
    }
}

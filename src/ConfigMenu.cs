using UnityEngine;
using Menu.Remix.MixedUI;

namespace JourneysStart
{
    public class ConfigMenu : OptionInterface
    {
        public static ConfigMenu instance = new();

        public const float topRow = 550f;
        public const float firstCol = 130f;
        public const float secondCol = firstCol + 225f;
        public const float separator = 40f;
        //private const float lilSeparator = separator - 5f; //35f

        public static Configurable<bool> enableJokeRecipes;
        public static Configurable<bool> removeCraftingLight;

        public static Configurable<int> flareCooldown; //this is in seconds, so x/40

        //public static Configurable<KeyCode> p1FlareKey;
        //public static Configurable<KeyCode> p2FlareKey;
        //public static Configurable<KeyCode> p3FlareKey;
        //public static Configurable<KeyCode> p4FlareKey;

        public ConfigMenu()
        {
            removeCraftingLight = config.Bind("removeCraftingLight",false,
                new ConfigurableInfo("Disable the light associated with crafting electric spears. Does not remove flashing lights from other parts of the game.",
                tags: new object[]
                {
                    "Disable Crafting Light"
                }));
            enableJokeRecipes = config.Bind("enableJokeRecipes", false,
                new ConfigurableInfo("Enable additional joke recipes for crafting.",
                tags: new object[]
                {
                    "Enable Joke Recipes"
                }));

            flareCooldown = config.Bind("flareCooldown", 3,
                new ConfigurableInfo("Change the cooldown for using the flare ability, in seconds.",
                tags: new object[]
                {
                    "Flare Cooldown"
                }));

            //p1FlareKey = config.Bind("p1FlareKey", /*KeyCode.LeftControl*/ KeyCode.Joystick1Button4,
            //    new ConfigurableInfo("Player 1's keybind for the flare ability.",
            //    tags: new object[]
            //    {
            //        "Player 1"
            //    }));
            //p2FlareKey = config.Bind("p2FlareKey", new KeyCode(),
            //    new ConfigurableInfo("Player 2's keybind for the flare ability.",
            //    tags: new object[]
            //    {
            //        "Player 2"
            //    }));
            //p3FlareKey = config.Bind("p3FlareKey", new KeyCode(),
            //    new ConfigurableInfo("Player 3's keybind for the flare ability.",
            //    tags: new object[]
            //    {
            //        "Player 3"
            //    }));
            //p4FlareKey = config.Bind("p4FlareKey", new KeyCode(),
            //    new ConfigurableInfo("Player 4's keybind for the flare ability.",
            //    tags: new object[]
            //    {
            //        "Player 4"
            //    }));
        }

        //called when config menu is opened by player
        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[3];
            Tabs[0] = new OpTab(instance, "Options");
            //Tabs[1] = new OpTab(instance, "Recipe List");
            //Tabs[2] = new OpTab(instance, "Joke Recipes");

            float nextRow = topRow;

            AddTextLabel("The Lightbringer", topRow, firstCol + 10f, 300f, 30f, true, FLabelAlignment.Center);

            nextRow -= separator;
            AddCheckBox(enableJokeRecipes, firstCol, nextRow);
            AddCheckBox(removeCraftingLight, secondCol, nextRow);
            //nextRow -= separator;
            //AddCheckBox(dimFlareAbility, firstCol, nextRow);

            //nextRow -= (separator * 2);

            //AddRecipeList();
            //AddJokeRecipes();
        }

        //public void AddRecipeList()
        //{
        //    //float nextRow = topRow;
        //    const int tab = 1;

        //    AddTextLabel("Crafting Recipes", topRow, firstCol + 10f, 300f, 30f, true, FLabelAlignment.Center, tab);

        //    //AddTextLabel("(Also requires 1 food pip and the spear to be non-explosive", nextRow, firstCol);
        //}

        //public void AddJokeRecipes()
        //{
        //    float nextRow = topRow;
        //    const int tab = 2;

        //    AddTextLabel("Joke Crafting Recipes", topRow, firstCol + 10f, 300f, 30f, true, FLabelAlignment.Center, tab);
        //    nextRow -= separator;
        //    AddRecipe("Karma Flower + Grenade", "Fire Egg", nextRow, tab);
        //}

        private void AddTextLabel(string text, float pos_y, float pos_x = firstCol + 150f,
            float size_x = 20f, float size_y = 20f, bool bigText = false, FLabelAlignment align = FLabelAlignment.Right, int tab = 0)
        {
            OpLabel textLabel = new(new Vector2(pos_x, pos_y), new Vector2(size_x, size_y), text, align, bigText);
            Tabs[tab].AddItems(new UIelement[]
            {
                textLabel
            });
        }
        private void AddCheckBox(Configurable<bool> optionText, float x, float y, int tab = 0)
        {
            OpCheckBox checkbox = new(optionText, new Vector2(x, y))
            {
                description = optionText.info.description
            };

            OpLabel checkboxLabel = new(x + 40f, y + 2f, optionText.info.Tags[0] as string)
            {
                description = optionText.info.description
            };

            Tabs[tab].AddItems(new UIelement[]
            {
                checkbox,
                checkboxLabel
            });
        }
        //private void AddRecipe(string components, string result, float pos_y, int tab = 0, float size_x = 20f, float size_y = 20f,
        //    bool bigText = false, FLabelAlignment align = FLabelAlignment.Left)
        //{
        //    //and then do this for components
        //    AddTextLabel(components, pos_y, firstCol, size_x, size_y, bigText, align, tab);
        //    AddTextLabel("=", pos_y, firstCol + secondCol / 2, size_x, size_y, bigText, align, tab);
        //    AddTextLabel(result, pos_y, secondCol + 40f, size_x, size_y, bigText, FLabelAlignment.Right, tab);
        //}
    }
}
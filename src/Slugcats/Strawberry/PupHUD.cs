#if false
using HUD;
using System.Collections.Generic;
using Mathf = UnityEngine.Mathf;
using Vector2 = UnityEngine.Vector2;
using Colour = UnityEngine.Color;
using static MoreSlugcats.MoreSlugcatsEnums;
//using MonoMod.RuntimeDetour;
//using System.Reflection;

namespace JourneysStart.Slugcats.Strawberry;

public class PupHUD
{
    public enum PupStatus
    {
        None,
        AddFood,
        Danger,
        Dead
    }

    public static void Hook()
    {
        
    }

    public class MockPupFoodMeter: HudPart
    {
        public int minFood;
        public int maxFood;
        
        public Vector2 pos;
        public Vector2 lastPos;

        public List<MockPupMeterCircle> circles;

        FSprite darkFade;
        FSprite lineSprite;

        public MockPupFoodMeter(HUD.HUD hud) : base(hud)
        {
            pos = new Vector2(Mathf.Max(50f, hud.rainWorld.options.SafeScreenOffset.x + 5.5f), Mathf.Max(25f, hud.rainWorld.options.SafeScreenOffset.y + 17.25f));
            lastPos = pos;

            circles = new();

            var food = SlugcatStats.SlugcatFoodMeter(SlugcatStatsName.Slugpup);
            minFood = food.y;
            maxFood = food.x;

            for (int i = 0; i < maxFood; i++)
            {
                circles.Add(new MockPupMeterCircle(this, i));
                circles[i].AddGradient();
            }

            darkFade = new FSprite("Futile_White", true)
            {
                shader = hud.rainWorld.Shaders["FlatLight"],
                color = new Colour(0f, 0f, 0f)
            };
            FContainer.AddChild(darkFade);

            lineSprite = new FSprite("pixel", true) // the | line separating min and max
            {
                scaleX = 1.5f,
                scaleY = 18.5f
            };
            FContainer.AddChild(lineSprite);

            for (int j = 0; j < circles.Count; j++)
            {
                circles[j].AddCircles();
            }

            //get current food count from save data
            //theres also more code in the ctor, i just dont have the save data rn
        }

        #region getters/setters
        public FContainer FContainer
        {
            get
            {
                return hud.fContainers[1];
            }
        }
        #endregion

        #region overrides
        public override void Update()
        {
            for (int i = circles.Count - 1; i >= 0; i--)
            {
                circles[i].Update();
            }
        }
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
        }
        public override void ClearSprites()
        {
            base.ClearSprites();
        }
        #endregion

        #region meter circle
        public class MockPupMeterCircle
        {
            public MockPupFoodMeter meter;
            public int number;

            FSprite gradient;

            HUDCircle[] circles;
            float[,] rads;

            public MockPupMeterCircle(MockPupFoodMeter meter, int num)
            {
                this.meter = meter;
                number = num;
                //more in ctor
            }

            public void Update()
            {

            }

            public void AddGradient()
            {
                gradient = new FSprite("Futile_White", true)
                {
                    shader = meter.hud.rainWorld.Shaders["FlatLight"],
                    color = new Colour(0f, 0f, 0f)
                };
                meter.FContainer.AddChild(gradient);
            }
            public void AddCircles()
            {
                circles = new HUDCircle[2];
                circles[0] = new(meter.hud, HUDCircle.SnapToGraphic.FoodCircleA, meter.FContainer, 0)
                {
                    rad = 0f,
                    lastRad = 0f
                };
                circles[1] = new(meter.hud, HUDCircle.SnapToGraphic.FoodCircleB, meter.FContainer, 0)
                {
                    rad = 0f,
                    lastRad = 0f
                };
                rads = new float[circles.Length, 2];
                for (int i = 0; i < rads.GetLength(0); i++)
                {
                    rads[i, 0] = circles[i].snapRad;
                }
            } 
        }
        #endregion
    }

    //public static void Hook()
    //{
    //    On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
    //    On.HUD.FoodMeter.CircleDistance += FoodMeter_CircleDistance;
    //}

    //private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
    //{
    //    orig(self, cam);
    //    if (self.owner is Player p && p.SlugCatClass == Plugin.strawberry)
    //    {
    //        var newHUD = new MockPupHUD(self);
    //        newHUD.lineSprite.scaleX = 1.5f;
    //        newHUD.lineSprite.scaleY = 18.5f;
    //        newHUD.pupBars.Clear();
    //        newHUD.lastCount = 0;

    //        self.AddPart(newHUD);
    //    }
    //}

    //private static float FoodMeter_CircleDistance(On.HUD.FoodMeter.orig_CircleDistance orig, FoodMeter self, float timeStacker)
    //{
    //    var val = orig(self, timeStacker);
    //    if (self is MockPupHUD)
    //        return Mathf.Lerp(20f, 15f, self.deathFade);
    //    return val;
    //}

    //public class MockPupHUD : FoodMeter
    //{
    //    public MockPupHUD(HUD.HUD hud) : base(hud, 0, 0) { }
    //}
    //public class PupSaveData
    //{
    //    public PupStatus status = PupStatus.None;
    //    public int currentFood;
    //}
}
#endif
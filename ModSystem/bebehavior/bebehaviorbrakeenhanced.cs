namespace Millwright.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.GameContent.Mechanics;
    using Millwright.ModConfig;

    public class BEBehaviorBrakeEnhanced : BEBehaviorMPAxle
    {
        BEBrakeEnhanced bebrakeenhanced;
        float resistance;
        ILoadedSound brakeSound;

        private static readonly Vec3f particleVelMin = new Vec3f(-0.1f, 0.1f, -0.1f);
        private static readonly Vec3f particleVelMax = new Vec3f(0.2f, 0.2f, 0.2f);
        private static readonly AssetLocation woodgrindSound = new AssetLocation("game:sounds/effect/woodgrind.ogg");

        private readonly float resistanceMultiplier = (float)ModConfig.Loaded.BrakeResistanceModifier;

        private string side;

        public override CompositeShape Shape
        {
            get
            {
                var shape = new CompositeShape() { Base = new AssetLocation("game:shapes/block/wood/mechanics/axle.json") };

                if (this.side == "east" || this.side == "west")
                { shape.rotateY = 90; }
                return shape;
            }
            set
            { }
        }


        public BEBehaviorBrakeEnhanced(BlockEntity blockentity) : base(blockentity)
        {
            this.bebrakeenhanced = blockentity as BEBrakeEnhanced;
        }


        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            this.side = this.Block.Variant["side"];
            this.bebrakeenhanced.RegisterGameTickListener(this.OnEvery50Ms, 100);
            switch (this.side)
            {
                case "north":
                case "south":
                    this.AxisSign = new int[] { -1, 0, 0 };
                    break;

                case "east":
                case "west":
                    this.AxisSign = new int[] { 0, 0, -1 };
                    break;
                default:
                    break;
            }
        }


        protected override bool AddStands => false;

        private void OnEvery50Ms(float dt)
        {
            this.resistance = GameMath.Clamp(this.resistance + dt / (this.bebrakeenhanced.Engaged ? 20 : -10), 0, 3);
            if (this.bebrakeenhanced.Engaged)
            { this.resistance *= this.resistanceMultiplier; }

            if (this.bebrakeenhanced.Engaged && this.network != null && this.network.Speed > 0.1)
            {
                this.Api.World.SpawnParticles(
                    this.network.Speed * 1.7f,
                    ColorUtil.ColorFromRgba(60, 60, 60, 100),
                    this.Position.ToVec3d().Add(0.1f, 0.5f, 0.1f),
                    this.Position.ToVec3d().Add(0.8f, 0.3f, 0.8f),
                    particleVelMin,
                    particleVelMax,
                    2, 0, 0.3f);
            }
            this.UpdateBreakSounds();
        }


        public void UpdateBreakSounds()
        {
            if (this.Api.Side != EnumAppSide.Client)
            { return; }

            if (this.resistance > 0 && this.bebrakeenhanced.Engaged && this.network != null && this.network.Speed > 0.1)
            {
                if (this.brakeSound == null || !this.brakeSound.IsPlaying)
                {
                    var clientWorld = this.Api.World as IClientWorldAccessor;
                    if (clientWorld == null) return;
                    this.brakeSound = clientWorld.LoadSound(new SoundParams()
                    {
                        Location = woodgrindSound,
                        ShouldLoop = true,
                        Position = this.Position.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                        DisposeOnFinish = false,
                        Volume = 1
                    });
                    this.brakeSound.Start();
                }
                this.brakeSound.SetPitch(GameMath.Clamp(this.network.Speed * 1.5f + 0.2f, 0.5f, 1));
            }
            else
            {
                this.brakeSound?.FadeOut(1, (s) => this.brakeSound.Stop());
            }
        }


        public override float GetResistance()
        {
            return this.resistance;
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            return base.OnTesselation(mesher, tesselator);
        }
    }
}

namespace Millwright.ModSystem
{
    //using System.Diagnostics;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;

    public class BEBrakeEnhanced : BlockEntity
    {
        public bool Engaged { get; protected set; }
        BlockEntityAnimationUtil AnimUtil => this.GetBehavior<BEBehaviorAnimatable>().animUtil;


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }


        MeshData ownMesh;

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            base.OnTesselation(mesher, tessThreadTesselator);
            if (!this.Engaged)
            {
                if (this.ownMesh == null)
                {
                    this.ownMesh = this.GenOpenedMesh(tessThreadTesselator, this.Block.Shape.rotateY);
                    if (this.ownMesh == null)
                    { return false; }
                }
                mesher.AddMeshData(this.ownMesh);
                return true;
            }
            return false;
        }


        private MeshData GenOpenedMesh(ITesselatorAPI tesselator, float rotY)
        {
            var key = "mwmechbrakeOpenedMesh" + rotY;
            var mesh = ObjectCacheUtil.GetOrCreate(this.Api, key, () =>
            {
                var shapeloc = AssetLocation.Create("millwright:shapes/block/wood/mechanics/brake-stand-opened.json");
                var shape = Shape.TryGet(this.Api, shapeloc);
                tesselator.TesselateShape(this.Block, shape, out var m, new Vec3f(0, rotY, 0));
                return m;
            });
            return mesh;
        }


        public bool OnInteract(IPlayer byPlayer)
        {
            this.Engaged = !this.Engaged;
            this.Api.World.PlaySoundAt(woodswitchSound, this.Pos.X + 0.5, this.Pos.Y + 0.5, this.Pos.Z + 0.5, byPlayer);
            this.MarkDirty(true);
            return true;
        }

        private static readonly AssetLocation woodswitchSound = new AssetLocation("game:sounds/effect/woodswitch.ogg");


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            this.Engaged = tree.GetBool("engaged");
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetBool("engaged", this.Engaged);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Axiom.RoguelikeTest.Components;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Core;

namespace Axiom.RoguelikeTest.Subsystems
{
    class SpriteRendererSubsystem : Subsystem
    {
        private readonly RTWorld _world;
        private readonly SpriteBatch _sb;

        public SpriteRendererSubsystem(RTWorld theWorld, SpriteBatch spriteBatch) : base(theWorld)
        {
            ComponentMask.SetBit(RTComponentType.Sprite);
            ComponentMask.SetBit(RTComponentType.Spatial);
            _world = theWorld;
            _sb = spriteBatch;
        }

        public override void Update(float dt) { }

        public void Draw()
        {
            uint entity;
            for (entity = 0; entity < World.MaxEntityId; entity++)
            {
                if (!ComponentMask.IsSubsetOf(World.EntityMasks[entity])) continue;

                var spatial = _world.SpatialComponents[entity];
                var sprite = _world.SpriteComponents[entity];

                _sb.Draw(sprite.Texture, spatial.Position, sprite.Tint);
            }
        }
    }
}

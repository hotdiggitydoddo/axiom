using System;
using System.Collections.Generic;
using System.Text;
using Axiom.RoguelikeTest.Components;
using Axiom.RoguelikeTest.Subsystems;
using Microsoft.Xna.Framework.Graphics;
using Zenith.Core;

namespace Axiom.RoguelikeTest
{
    class RTWorld : World
    {
        private readonly SpriteRendererSubsystem _spriteRendererSubsystem;

        public SpatialComponent[] SpatialComponents { get; }
        public PhysicsComponent[] PhysicsComponents { get; }
        public SpriteComponent[] SpriteComponents { get; }
        public RTWorld(uint maxEntities, SpriteBatch sb) : base(maxEntities)
        {
            SpatialComponents = new SpatialComponent[MaxEntities];
            PhysicsComponents = new PhysicsComponent[MaxEntities];
            SpriteComponents = new SpriteComponent[MaxEntities];

            _spriteRendererSubsystem = new SpriteRendererSubsystem(this, sb);

            int entity;
            for (entity = (int) MaxEntities - 1; entity >= 0; entity--)
            {
                Initialize((uint) entity);
                SpriteComponents[entity] = new SpriteComponent();
                SpatialComponents[entity] = new SpatialComponent();
            }
        }

        public override void Update(float dt)
        {
            throw new NotImplementedException();
        }

        public override void Render()
        {
            _spriteRendererSubsystem.Draw();
        }
    }
}

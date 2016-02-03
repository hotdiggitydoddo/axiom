using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Axiom.RoguelikeTest.Components
{
    class SpriteComponent
    {
        public Texture2D Texture { get; set; }
        public Color Tint { get; set; }
        public float Scale { get; set; }
        public Rectangle SourceRectangle { get; set; }

        public SpriteComponent(Texture2D tex, float scale = 1.0f, Color color = default(Color))
        {
            Texture = tex;
            Scale = scale;
            Tint = color;
        }

        public SpriteComponent()
        {
            
        }
    }
}

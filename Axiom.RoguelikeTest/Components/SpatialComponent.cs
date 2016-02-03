using Microsoft.Xna.Framework;

namespace Axiom.RoguelikeTest.Components
{
    class SpatialComponent
    {
        public Vector2 Position { get; set; }

        public SpatialComponent(){ }

        public SpatialComponent(float x, float y)
        {
            Position = new Vector2(x, y);
        }
        public SpatialComponent(Vector2 pos)
        {
            Position = pos;
        }
    }
}

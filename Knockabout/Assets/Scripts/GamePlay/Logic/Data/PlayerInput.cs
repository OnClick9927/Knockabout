using System;

namespace GamePlay
{
    public class PlayerInput : IEquatable<PlayerInput>
    {
        public string guid;
        public long frame;

        public enum InputType
        {
            None,
            UseCard,
        }
        public InputType type;
        public int Card_index;
        public int Card_id;

        public bool Equals(PlayerInput other)
        {
            return true;
        }

  
    }
}



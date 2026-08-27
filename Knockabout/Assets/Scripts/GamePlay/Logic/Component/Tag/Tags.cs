namespace GamePlay
{
    public class Tags
    {
        public const string Dead = "Dead";
        public const string Silence = "Silence";
        public const string Player = "Player";
        public const string Role = "Role";

        public const char sp = '_';

        /// <summary>
        /// Р§зг
        /// Dead Dead
        /// Dead Dead_1
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool ValueIsTag(string tag, string value)
        {
            if (tag == value)
                return true;
            var index = tag.IndexOf(value);
            if (index != 0) return false;
            if (tag[value.Length] != sp) return false;
            return true;
        }
    }
}



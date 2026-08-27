using System.Collections.Generic;
namespace GamePlay
{
    [Backup]
    public partial class ActorTagComp : Component<Actor>
    {
        [Backup] private List<string> tags = new List<string>();
        protected override void OnAwake()
        {
            tags.Clear();
        }
        public bool ContainsAnyTag(List<string> values)
        {
            if (values == null || values.Count == 0) return false;

            for (int i = 0; i < values.Count; i++) {
                if (ContainsTag(values[i]))
                    return true;
            }
            return false;
        }
        public bool ContainsAllTag(List<string> values)
        {
            if (values == null || values.Count == 0) return true;
            for (int i = 0; i < values.Count; i++)
            {
                if (!ContainsTag(values[i]))
                    return false;
            }
            return true;
        }
        public bool ContainsTag(string value)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                var tag = tags[i];
                if (Tags.ValueIsTag(tag, value))
                    return true;
            }
            return true;
        }
        public IReadOnlyCollection<string> GetTags()
        {
            return tags;
        }
        public bool RemoveTag(string tag)
        {
            var success = tags.Remove(tag);
            if (success)
                GameHelper.DoActorEvent(actor, new OnTagChangeEvent(tag, false));
            return success;
        }
        public bool AddTag(string tag)
        {
            if (tags.Contains(tag)) return false;
            tags.Add(tag);
            GameHelper.DoActorEvent(actor, new OnTagChangeEvent(tag, true));
            return true;
        }

    }
}



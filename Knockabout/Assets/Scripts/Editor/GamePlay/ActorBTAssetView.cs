using ActionEditor.Nodes.BT;

namespace GamePlay
{
    public class ActorBTAssetView : BTTreeView<ActorBTAsset>
    {
        public override bool IsFileFitAsset(string path)
        {
            return path.StartsWith("Assets/Project_GP/BT");

        }
    }

}



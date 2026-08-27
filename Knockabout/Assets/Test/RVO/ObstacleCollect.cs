using System.Collections.Generic;
using Lockstep;
using Lockstep.RVO;
using UnityEngine;

public class ObstacleCollect : MonoBehaviour
{
    void Start()
    {
        BoxCollider[] boxColliders = GetComponentsInChildren<BoxCollider>();
        for (int i = 0; i < boxColliders.Length; i++)
        {
            var collider = boxColliders[i];
            LVector2 center = new LVector2(
                LMath.ToLFloat(collider.transform.position.x),
                LMath.ToLFloat(collider.transform.position.z));
            LVector2 size = new LVector2(
                LMath.ToLFloat(collider.size.x) * LMath.ToLFloat(collider.transform.lossyScale.x),
                LMath.ToLFloat(collider.size.z) * LMath.ToLFloat(collider.transform.lossyScale.z));
            LVector2 half = size * LFloat.half;

            LFloat minX = center.x - half.x;
            LFloat minZ = center.y - half.y;
            LFloat maxX = center.x + half.x;
            LFloat maxZ = center.y + half.y;

            IList<LVector2> obstacle = new List<LVector2>();
            obstacle.Add(new LVector2(maxX, maxZ));
            obstacle.Add(new LVector2(minX, maxZ));
            obstacle.Add(new LVector2(minX, minZ));
            obstacle.Add(new LVector2(maxX, minZ));
            GameMainManager.Instance.simulator.addObstacle(obstacle);
        }
    }
}
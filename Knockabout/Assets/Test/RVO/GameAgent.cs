using Lockstep;
using Lockstep.RVO;
using UnityEngine;

public class GameAgent : MonoBehaviour
{
    [HideInInspector] public int sid = -1;

    private Lockstep.Random m_random = new Lockstep.Random(17);
    private int m_randomSid = -1;
    private Simulator simulator => GameMainManager.Instance.simulator;

    private void EnsureRandomSeed()
    {
        if (m_randomSid == sid)
            return;

        uint seed = (uint)(17 + sid * 1103515245);
        m_random = new Lockstep.Random(seed);
        m_randomSid = sid;
    }

    void Update()
    {
        if (sid < 0)
            return;

        EnsureRandomSeed();

        var agent = simulator.getAgent(sid);
        LVector2 pos = agent.position_;
        LVector2 vel = agent.prefVelocity_;
        transform.position = new Vector3(pos.x.ToFloat(), transform.position.y, pos.y.ToFloat());
        if (LMath.Abs(vel.x) > LFloat.EPSILON && LMath.Abs(vel.y) > LFloat.EPSILON)
            transform.forward = new Vector3(vel.x.ToFloat(), 0, vel.y.ToFloat()).normalized;

        if (!Input.GetMouseButton(1))
        {
            agent.prefVelocity_ = LVector2.zero;
            return;
        }

        LVector2 goalVector = GameMainManager.Instance.mousePosition - agent.position_;
        if (RVOMath.absSq(goalVector) > LFloat.one)
        {
            goalVector = RVOMath.normalize(goalVector);
        }

        agent.prefVelocity_ = goalVector;

        LFloat angle = m_random.value * LMath.PI2;
        LFloat dist = m_random.value * LFloat.FromRaw(100L);
        agent.prefVelocity_ = agent.prefVelocity_ + dist * new LVector2(LMath.Cos(angle), LMath.Sin(angle));
    }
}

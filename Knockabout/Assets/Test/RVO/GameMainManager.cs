using System.Collections.Generic;
using Lockstep;
using Lockstep.RVO;
using UnityEngine;
using UnityEngine.Assertions;

public class GameMainManager : MonoBehaviour
{
    private static readonly LFloat TimeStep = LFloat.FromRaw(250000L);
    private static readonly LFloat DeleteRadius = LFloat.FromRaw(1500000L);

    private static GameMainManager _instance;
    public static GameMainManager Instance => _instance;

    private void Awake()
    {
        _instance = this;
    }

    public GameObject agentPrefab;

    [HideInInspector] public LVector2 mousePosition;

    private Plane m_hPlane = new Plane(Vector3.up, Vector3.zero);
    private Dictionary<int, GameAgent> m_agentMap = new Dictionary<int, GameAgent>();
    public Simulator simulator = new Simulator();

    void Start()
    {
        simulator.timeStep_ = TimeStep;
        simulator.setAgentDefaults(
            LMath.ToLFloat(15).ToFloat(),
            10,
            LMath.ToLFloat(5).ToFloat(),
            LMath.ToLFloat(5).ToFloat(),
            LMath.ToLFloat(2).ToFloat(),
            LMath.ToLFloat(2).ToFloat(),
            LVector2.zero);
        simulator.processObstacles();
    }

    private void UpdateMousePosition()
    {
        Vector3 position = Vector3.zero;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        float rayDistance;
        if (m_hPlane.Raycast(mouseRay, out rayDistance))
            position = mouseRay.GetPoint(rayDistance);

        mousePosition = new LVector2(LMath.ToLFloat(position.x), LMath.ToLFloat(position.z));
    }

    void DeleteAgent()
    {
        int agentNo = this.simulator.queryNearAgent(mousePosition, DeleteRadius);
        if (agentNo == -1 || !m_agentMap.ContainsKey(agentNo))
            return;

        this.simulator.delAgent(agentNo);
        GameObject.Destroy(m_agentMap[agentNo].gameObject);
        m_agentMap.Remove(agentNo);
    }

    void CreatAgent()
    {
        int sid = this.simulator.addAgent(mousePosition).id_;
        if (sid >= 0)
        {
            GameObject go = GameObject.Instantiate(agentPrefab, new Vector3(mousePosition.x.ToFloat(), 0, mousePosition.y.ToFloat()), Quaternion.identity);
            GameAgent ga = go.GetComponent<GameAgent>();
            Assert.IsNotNull(ga);
            ga.sid = sid;
            m_agentMap.Add(sid, ga);
        }
    }

    private void Update()
    {
        UpdateMousePosition();
        if (Input.GetMouseButtonUp(0))
        {
            if (Input.GetKey(KeyCode.Delete))
            {
                DeleteAgent();
            }
            else
            {
                CreatAgent();
            }
        }

        this.simulator.doStep();
    }
}
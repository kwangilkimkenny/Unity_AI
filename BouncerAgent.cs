using UnityEngine;
using MLAgents;


 // 이 코드의 핵심은 초기화, 관찰, 보상과 패널티, 점프운동과 회전 그리고 점프 제한횟수다.


public class BouncerAgent : Agent
{
    [Header("Bouncer Specific")]
    public GameObject target;
    public GameObject bodyObject;
    Rigidbody m_Rb;
    Vector3 m_LookDir;
    public float strength = 10f;
    float m_JumpCooldown;
    int m_NumberJumps = 20;
    int m_JumpLeft = 20;

    ResetParameters m_ResetParams; //에이전트의 파라미터값을 초기화함

    public override void InitializeAgent() // 초기화한다.
    {
        m_Rb = gameObject.GetComponent<Rigidbody>(); // 리지드바디값을 가져옴
        m_LookDir = Vector3.zero; // (0,0,0)으로 초기화

        var academy = FindObjectOfType<Academy>();  // 아카데미에서 오브젝트 타입을 아카데미변수에 넣느다.
        m_ResetParams = academy.resetParameters;  //아카데미의 초기화된 파라미터값을 가져와서넣는다.

        SetResetParameters(); //리셋한다.
    }

    public override void CollectObservations() // 관찰한다. 에이전트와 타켓의 위치를 관찰한다.(이것을 텐서플로우에 보내나?)
    {
        AddVectorObs(gameObject.transform.localPosition);
        AddVectorObs(target.transform.localPosition);
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        for (var i = 0; i < vectorAction.Length; i++) //액션의 크기만큼 반복한다.
        {
            vectorAction[i] = Mathf.Clamp(vectorAction[i], -1f, 1f); // -1~+1 사이로 값을 고정하여 백터엑션에 넣는다.
        }
        var x = vectorAction[0]; //첫번째는 변수 x 값에 넣는다.
        var y = ScaleAction(vectorAction[1], 0, 1);  //두번째 값을 변수에 넣는다.
        var z = vectorAction[2]; //세번째 값을 Z값에 넣는다. 
        m_Rb.AddForce(new Vector3(x, y + 1, z) * strength); //에이전트의 리지드바디에 힘을 넣어 준다. 그러면 위로 에이전트가 올라가게 됨(튀기는 효과재현)

        AddReward(-0.05f * (
            vectorAction[0] * vectorAction[0] +
            vectorAction[1] * vectorAction[1] +
            vectorAction[2] * vectorAction[2]) / 3f); //  액션에 대한  x, y, z 행동의 값을 모두 더해서 평균을 낸 후 보상을 -로 준다. 가만히 있어도 벌점이 가해진다는 의미 

        m_LookDir = new Vector3(x, y, z); //x,y,z의 값을 m_LookDir에 넣어준다.
    }

    public override void AgentReset() //에이전트를 리셋한다.
    {
        gameObject.transform.localPosition = new Vector3(
            (1 - 2 * Random.value) * 5, 2, (1 - 2 * Random.value) * 5); //에이전트의 위치는 랜덤으로 결정된다.
        m_Rb.velocity = default(Vector3); // 가속도는 기본값으로 한다.
        var environment = gameObject.transform.parent.gameObject; //구현환경은 에이전트가 몸담고 있는 부모오브젝트다.
        var targets =
            environment.GetComponentsInChildren<BouncerTarget>(); // 타겟을 정한다.
        foreach (var t in targets) //타겟을 계속 리스폰한다. 리셋되면 자동으로 타겟이 나타나도록 함.
        {
            t.Respawn();
        }
        m_JumpLeft = m_NumberJumps; // 20번 점프하게 함. 이것이 남은 점프수고 20번안에 타켓을 건들이지 못하면 리셋.

        SetResetParameters();
    }

    public override void AgentOnDone() // 아무것도 없음 이건뭐냥. 지우든가 해야지.
    {
    }

    private void FixedUpdate() //무조건 실행되는 함수로 조건이 좀 있다.
    {
        if (Physics.Raycast(transform.position, new Vector3(0f, -1f, 0f), 0.51f) && m_JumpCooldown <= 0f) // 점프가 안되면 다시 점프를 하도록 하고 이동하지 못하도록 속도를 초기화한다. 카운트로 -1을 한다. 
        {
            RequestDecision();
            m_JumpLeft -= 1;
            m_JumpCooldown = 0.1f;
            m_Rb.velocity = default(Vector3);
        }

        m_JumpCooldown -= Time.fixedDeltaTime; // 공의 힘을 점점 빠지게 한다.

        if (gameObject.transform.position.y < -1) //밖으로 떯어지면 -1점 준다. 벌준다.
        {
            AddReward(-1);
            Done();
            return;
        }

        if (gameObject.transform.localPosition.x < -19 || gameObject.transform.localPosition.x > 19
            || gameObject.transform.localPosition.z < -19 || gameObject.transform.localPosition.z > 19) //밖으로 튕겨나가도 벌준다.
        {
            AddReward(-1);
            Done();
            return;
        }
        if (m_JumpLeft == 0) //남은 점프수가 0이면 끝낸다.
        {
            Done();
        }
    }

    public override float[] Heuristic() // 휴리스틱으로 할 때 인풋값이다. 
    {
        var action = new float[3];

        action[0] = Input.GetAxis("Horizontal");
        action[1] = Input.GetKey(KeyCode.Space) ? 1.0f : 0.0f;
        action[2] = Input.GetAxis("Vertical");
        return action;
    }

    private void Update() //반복한다. 회전한다는 의미다. 주변을 살펴보기 위해서임. 
    {
        if (m_LookDir.magnitude > float.Epsilon) // 길이가 아주 작은 수보다 크다면? 회전해!
        {
            bodyObject.transform.rotation = Quaternion.Lerp(bodyObject.transform.rotation,
                Quaternion.LookRotation(m_LookDir),
                Time.deltaTime * 10f);
        }
    }

    public void SetTargetScale() // 타겟의 크기를 설정한다. 초기화하면 타켓의 크기를 다시 설정하도록 되어 있음.
    {
        var targetScale = m_ResetParams["target_scale"];
        target.transform.localScale = new Vector3(targetScale, targetScale, targetScale);
    }

    public void SetResetParameters()
    {
        SetTargetScale();
    }
}

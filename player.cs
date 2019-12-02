using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class player : MonoBehaviour
{
    [SerializeField] private string targetTag = "TargetBasket";

    public GameObject ball;
    public GameObject playerCamera; // agnet에 붙어있는 카메라가 바라봄
    public float ballDistance = 0.1f; // 공과 agnet와의 거리를 정해줌
    public float ballThrowingForce = 600f; //공던지는 힘
    public float distance; // 거리

    private bool holdingBall = true; // 처음부터 공을 잡고 있음. 나중에 여기에 기능을 변경하자면, 공을 찾아가서(false) 공을 잡으면(ture)로 처리하자

    public float range = 300f; // 이건 레이케스트의 레인지를 300으로 정함
    public bool avoiding = false; // 상대방을 피하기 위함으로 기본값은 피함으로 설정(false)


    [Header("Sensors")]
    public float sensorLength = 50f;
    public float frontSensorPosition = 0.5f;
    public float frontsideSensorPosition = 5f;
    public float frontSensorAngle = 30f;


    // Start is called before the first frame update
    void Start()
    {
        ball.GetComponent<Rigidbody>().useGravity = false; // 공에게 중력를 없애서 공을 잡도록 하려는 의도임
    }

    private void FixedUpdate()
    {
        Sensors();
    }

    private void Sensors() // 첫번째 센서를 장착한다.
    {
        //main sensor
        RaycastHit hit;
        Vector3 sensorStartPos = playerCamera.transform.position; //센서의 시작점은 카메라의 위치와 같다.
        sensorStartPos.x += frontSensorPosition; // 센서의 시작점에서 x축 방향으로 0.5f 만큼 위치해 있다.
        float avoidingMultiplier = 0;
        avoiding = false;

        // 이것은 조건문을 추가하여 스텐드를 찾아내면 각도를 올리거나 내려서(조정) 혹은 그냥 공을 발사(단, 공의 발사파워를 자유롭게 조정-이것은 AI가 결정)
        if (Physics.Raycast(sensorStartPos, playerCamera.transform.forward, out hit, sensorLength))
        {
            if (hit.collider.CompareTag("stand"))
            //if (!hit.collider.CompareTag("stand"))     //!연산자를 왜 넣었지? 빼야하나 넣어야하나?
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                Debug.Log(hit.transform.name);
                Debug.Log("Laycast!! go a another way");
                avoiding = true;
            }
      
        }

        //front center sensor     //상대플레이어를 정면으로 마주치면(레이로 캐스팅이 정면으로 되면 최대한 각도를 계산해서 좌우 회전을 결정하자
        if (avoidingMultiplier == 0)
        {
            if (Physics.Raycast(sensorStartPos, playerCamera.transform.forward, out hit, sensorLength))
            {
                if (!hit.collider.CompareTag("Defence"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    Debug.Log(hit.transform.name);
                    Debug.Log("Laycast!! go a another way");
                    avoiding = true;
                    if(hit.normal.x < 0)
                    {
                        avoidingMultiplier = -1;
                    } else
                    {
                        avoidingMultiplier = 1;
                    }
                }

                
                else if(!hit.collider.CompareTag("wall")) // 벽 콜라이더를 만나면 방향을 3만큼 전환하라는 것인데, ** 레이의 거리가 너무 길면 벽을 만나기도 전에 회전을 할것임, 레이의 길이를 줄이거나, 새로운 레이를 추가하거나 할 것
                {                                         //벽을 감지하면 가까이 갈때까지 액션을 취하지 않고, 가까이가면 방향 전환액션을 실행하는 코드를 삽입
                    Debug.Log("wall! turn around!");  
                    avoiding = true;
                    if (hit.normal.x < 0)
                    {
                        avoidingMultiplier = -3;
                    }
                    else
                    {
                        avoidingMultiplier = 3;
                    }
                }

            }
        }


        //front right sensor
        if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontSensorAngle, transform.up) * transform.forward, out  hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Defence"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                Debug.Log(hit.transform.name);
                Debug.Log("Laycast!! trun left");

                avoiding = true;
                avoidingMultiplier -= 1f;
            }

        }

        //front left sensor
        if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Defence"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                Debug.Log(hit.transform.name);
                Debug.Log("Laycast!! trun right!!");

                avoiding = true;
                avoidingMultiplier += 1f;
            }
        }

        if (avoiding)
        {
            //플레이어 오브젝트가 방향을 트는 기능 추가로 left, right의 방향으로 회전각도 maxTurningAngle * avoidingMultiplier; 이런식으로 처리
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (holdingBall) // 공을 잡는다, 초기값이 true이기 때문. **나중에 공을 찾아서 다가가서 잡는 방식으로 구현할계획
        {

            // 카메라를 이용하여 레이캐스트를 적용 후 바스켓에 적중하면 공을 발사하는 코드 ** 위아래로 조절하는 카메라 레이케스트 코드도 넣어야 함(자율주행 부분 추가한 후 작성)

            ball.transform.position = playerCamera.transform.position + playerCamera.transform.up * ballDistance ; // play의 위에 공을 위치시킨다.

            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, range)) // 레인지 범위안에 카메라가 레이를 쏜다. 뭔가를 감지하기 위해서
            {
                //Debug.Log(hit.transform.name); // 콘솔에 인식한 오브젝트의 이름을 출력한다. 확인하는 차원에서 
                //Debug.DrawLine(playerCamera.transform.position, hit.point);

                var targeting = hit.transform;
                if (targeting.CompareTag(targetTag))//감지하려는 레이가 만약 target의 Tag에 맞으면,
                {

                    holdingBall = false; //공을 놓고
                    ball.GetComponent<Rigidbody>().useGravity = true; // 공에 중력을 부여한다.
                    Debug.Log("Fire");
                    ball.GetComponent<Rigidbody>().AddForce(playerCamera.transform.forward * ballThrowingForce); // 공을 발사한다. 공에게 앞으로 나아가는 힘을 부여해준다.
                }     

            }

            


        }

    }
}
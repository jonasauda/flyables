using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleFly : MonoBehaviour
{


    Vector3 homePos = new Vector3(1, 1, 4);

    bool idle = true;


    Rigidbody m_Rigidbody;
    float m_Speed;

    // Start is called before the first frame update
    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        this.transform.position = homePos;
        m_Speed = 1.0f;

    }

    // Update is called once per frame
    void Update()
    {
        if (idle)
        {
            fly();
        }
    }



    private void naturalHeightChange()
    {
        float y = transform.position.y + Mathf.Sin(Time.realtimeSinceStartup) / 360 * 0.5f;
        //Debug.Log("y=" + y);
        transform.position = new Vector3(transform.position.x, y, transform.position.z);


    }


    private void fly()
    {
        transform.Rotate(new Vector3(0, -1, 0) * Time.deltaTime * 100.0f, Space.World);
        m_Rigidbody.velocity = transform.forward * m_Speed;


        naturalHeightChange();





    }
}

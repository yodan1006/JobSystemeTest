using Eloi.WatchAndDate;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDeleteMeMono : MonoBehaviour
{

    public int m_numberOfTest=100;
    public WatchAndDateTimeActionResult m_time;
    public void DoSomething() {

        m_time.StartCounting();
        for (int i = 0; i < m_numberOfTest; i++) {
            Debug.Log("Hello");
        }
        m_time.StopCounting();

    }
}

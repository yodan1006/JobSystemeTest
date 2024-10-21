using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yodan
{
    public class MoveManagerMono : MonoBehaviour
    {
        /// <summary>
        /// this is de rotation on the killDozer in angle left to righ
        /// </summary>
        [Tooltip("this rotation in angle around Y axis")]
        public float m_rotationLeftRight = 90f;

        [Space(1)]
        [Tooltip("this speed in meter forward")]
        public float m_frontalSpeed = 10f;
        [Tooltip("this speed in meter to go backward")]
        public float m_backSpeed = 5f;

        [Header("pourcent joystick")]
        [Range(-1, 1)]
        [SerializeField]
        float m_rotateLeftRightPercent;

        [Range(-1, 1)]
        [SerializeField]
        float m_moveFrontBackPercent;

        public Transform m_whatToMove;

        [ContextMenu("set random input")]
        public void SetRandomValue()
        {
            m_moveFrontBackPercent = UnityEngine.Random.Range(-1f, 1f);
            m_rotateLeftRightPercent = UnityEngine.Random.Range(-1f, 1f);
        }

        public void SetRotateLeftRightPercent(float percent11)
        {
            if (percent11 > 1)
                percent11 = 1;
            if (percent11 < -1)
                percent11 = -1;
            m_rotateLeftRightPercent = percent11;
        }

        public void SetMoveBackForwardPercent(float percent11)
        {
            percent11 = MyMathToolBox.Clamp(percent11, -1, 1);
            m_moveFrontBackPercent = percent11;
        }



        void Update()
        {
            //rotation
            float rotation = m_rotateLeftRightPercent * m_rotationLeftRight;
            m_whatToMove.Rotate(0, rotation * Time.deltaTime, 0);


            float speed = GetSpeedFront();
            m_whatToMove.Translate(0, 0, m_moveFrontBackPercent * speed * Time.deltaTime);
        }

        private float GetSpeedFront()
        {
            return m_moveFrontBackPercent > 0 ? m_frontalSpeed : m_backSpeed;
        }
    }
    public class MyMathToolBox
    {
        public static float Clamp(float value, float minValue, float maxValue)
        {
            if (value > 1)
                value = 1;
            if (value < -1)
                value = -1;
            return value;
        }
    }
}
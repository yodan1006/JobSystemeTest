using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManagerMono : MonoBehaviour
{

    #region PUBLIC VARIABLES
    public InputActionReference m_whatToListenForYAxis;
    public InputActionReference m_whatToListenForXAxis;
    public UnityEvent<float> m_onAxisYChanged;
    public UnityEvent<float> m_onAxisXChanged;
    public float m_currentValueX;
    public float m_currentValueY;
    #endregion

    private void NotifyXChanged(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (value != m_currentValueX)
        {
            m_currentValueX = value;
            m_onAxisXChanged.Invoke(m_currentValueX);
        }
    }

    private void NotifyYChanged(InputAction.CallbackContext context)
    {
        float value = context.ReadValue<float>();
        if (value != m_currentValueY)
        {
            m_currentValueY = value;
            m_onAxisYChanged.Invoke(m_currentValueY);
        }
    }

    #region UNITY METHODE
    private void OnEnable()
    {
        m_whatToListenForXAxis.action.Enable();
        m_whatToListenForXAxis.action.performed += NotifyXChanged;
        m_whatToListenForXAxis.action.canceled += NotifyXChanged;

        m_whatToListenForYAxis.action.Enable();
        m_whatToListenForYAxis.action.performed += NotifyYChanged;
        m_whatToListenForYAxis.action.canceled += NotifyYChanged;
    }

    private void OnDisable()
    {
        m_whatToListenForXAxis.action.performed -= NotifyXChanged;
        m_whatToListenForXAxis.action.canceled -= NotifyXChanged;

        m_whatToListenForYAxis.action.performed -= NotifyYChanged;
        m_whatToListenForYAxis.action.canceled -= NotifyYChanged;
    }
    #endregion
}


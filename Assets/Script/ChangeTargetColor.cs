using UnityEngine;

public class ChangeTargetColor : MonoBehaviour
{

    public MeshRenderer m_toAffect;
    public Color m_colorToChangeTo;

    [ContextMenu("Change Random Color")]
    public void ChangeRandomColor()
    {
        m_colorToChangeTo =
            new Color(
                Random.value,
                Random.value,
                Random.value
                );
        m_toAffect.material.color = m_colorToChangeTo;
    }

}

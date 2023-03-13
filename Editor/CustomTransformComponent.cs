using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(Transform))]
[CanEditMultipleObjects]
public class CustomTransformComponent : Editor
{
    private Transform _transform;


    public override void OnInspectorGUI()
    {
        _transform = (Transform)target;

        StandardTransformInspector();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.Space();
        AlignToGround();
        StickToGround();
        EditorGUILayout.EndHorizontal();
    }


    #region StandardTransform

    private void StandardTransformInspector()
    {
        bool didPositionChange = false;
        bool didRotationChange = false;
        bool didScaleChange = false;

        Vector3 initialLocalPosition = _transform.localPosition;
        Vector3 initialLocalEuler = _transform.localEulerAngles;
        Vector3 initialLocalScale = _transform.localScale;

        EditorGUI.BeginChangeCheck();
        Vector3 localPosition = EditorGUILayout.Vector3Field("Position", _transform.localPosition);
        if (EditorGUI.EndChangeCheck())
            didPositionChange = true;

        EditorGUI.BeginChangeCheck();
        Vector3 localEulerAngles = EditorGUILayout.Vector3Field("Euler Rotation", _transform.localEulerAngles);
        if (EditorGUI.EndChangeCheck())
            didRotationChange = true;

        EditorGUI.BeginChangeCheck();
        Vector3 localScale = EditorGUILayout.Vector3Field("Scale", _transform.localScale);
        if (EditorGUI.EndChangeCheck())
            didScaleChange = true;

        if (didPositionChange || didRotationChange || didScaleChange)
        {
            Undo.RecordObject(_transform, _transform.name);

            if (didPositionChange)
                _transform.localPosition = localPosition;

            if (didRotationChange)
                _transform.localEulerAngles = localEulerAngles;

            if (didScaleChange)
                _transform.localScale = localScale;

        }

        Transform[] selectedTransforms = Selection.transforms;
        if (selectedTransforms.Length > 1)
        {
            foreach (var item in selectedTransforms)
            {
                if (didPositionChange || didRotationChange || didScaleChange)
                    Undo.RecordObject(item, item.name);

                if (didPositionChange)
                {
                    item.localPosition = ApplyChangesOnly(
                        item.localPosition, initialLocalPosition, _transform.localPosition);
                }

                if (didRotationChange)
                {
                    item.localEulerAngles = ApplyChangesOnly(
                        item.localEulerAngles, initialLocalEuler, _transform.localEulerAngles);
                }

                if (didScaleChange)
                {
                    item.localScale = ApplyChangesOnly(
                        item.localScale, initialLocalScale, _transform.localScale);
                }
            }
        }
    }

    private Vector3 ApplyChangesOnly(Vector3 toApply, Vector3 initial, Vector3 changed)
    {
        if (!Mathf.Approximately(initial.x, changed.x))
            toApply.x = _transform.localPosition.x;

        if (!Mathf.Approximately(initial.y, changed.y))
            toApply.y = _transform.localPosition.y;

        if (!Mathf.Approximately(initial.z, changed.z))
            toApply.z = _transform.localPosition.z;

        return toApply;
    }

    #endregion

    #region AlignRotation

    private bool alignRotation = false;
    private bool isPlanet = false;
    public enum Axis
    {
        X=0,
        Y=1,
        Z=2,
        invX=3,
        invY=4,
        invZ=5
    }
    private int selectedAxis = (int)Axis.Y;
    private List<string> options= new List<string>() {
        Axis.X.ToString(), Axis.Y.ToString(), Axis.Z.ToString(),
        Axis.invX.ToString(), Axis.invY.ToString(), Axis.invZ.ToString() 
    };
    private Vector3 lookDir = Vector3.zero;
    GameObject planetToAlignTo = null;

    private void AlignToGround()
    {
        //Only works if the object is not a planet
        if (_transform.CompareTag("Planet"))
        {
            isPlanet = true;
        }

        //This is the axis dropdown selection menu
        serializedObject.Update();
        selectedAxis = EditorGUILayout.Popup(selectedAxis, options.ToArray(), "Button", GUILayout.MaxWidth(40), GUILayout.MinWidth(25));
        serializedObject.ApplyModifiedProperties();

        //This is the align rotation button
        EditorGUI.BeginChangeCheck();
        alignRotation = GUILayout.Toggle(alignRotation, "Align Rotation",  "Button", GUILayout.MaxWidth(100));
        EditorGUI.EndChangeCheck();


        if (alignRotation && !isPlanet)
        {
            float distance = float.MaxValue;
            
            //We find the nearest planet to the current object 
            foreach (GameObject gm in GameObject.FindGameObjectsWithTag("Planet"))
            {
                float temp = Vector3.Distance(_transform.position, gm.transform.position);
                if (temp < distance)
                {
                    distance = temp;
                    planetToAlignTo = gm;
                }
            }

            if (planetToAlignTo != null)
            {

                lookDir = _transform.position - planetToAlignTo.transform.position; //Find the correct rotation
                var addition = Vector3.zero;
                switch ((Axis)selectedAxis)
                {
                    case Axis.X:
                        addition = new Vector3(90, 0, 0);
                        break;
                    case Axis.invX:
                        addition = new Vector3(270, 0, 0);
                        break;
                    case Axis.Y:
                        addition = new Vector3(0, 0, 90);
                        break;
                    case Axis.invY:
                        addition = new Vector3(180, 0, 90);
                        break;
                    case Axis.Z:
                        addition = new Vector3(0, 90, 0);
                        break;
                    case Axis.invZ:
                        addition = new Vector3(0, 270, 0);
                        break;
                }
                //addition is the correct rotation we need to add to satisfy the Selected Axis dropdown choice

                _transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up) * Quaternion.Euler(addition) ;
            }
        }
    }
    #endregion

    #region StickToGround

    private bool stickToGround = false;
    private void StickToGround()
    {
        //Only stick to ground if align rotation is activated
        if (!alignRotation)
            stickToGround = false;

        //StickToGround Button
        EditorGUI.BeginChangeCheck();
        stickToGround = GUILayout.Toggle(stickToGround, "Stick", "Button", GUILayout.MaxWidth(100));
        EditorGUI.EndChangeCheck();

        
        if (stickToGround && !isPlanet && alignRotation)
        {
            //Simple Raycast and change position to that of the RaycastHit
            RaycastHit hit;
            if (Physics.Raycast(_transform.position, planetToAlignTo.transform.position - _transform.position , out hit, Mathf.Infinity))
            {
                _transform.position = hit.point;
            }
        }

    }
    #endregion


}


using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerTracker : UdonSharpBehaviour
{
    public Transform headTransform;
    public Transform leftHandTransform;
    public Transform rightHandTransform;
    public Transform hudTransform;

    [HideInInspector] public float height = 170;
    [HideInInspector] public float rawHeight = 170;

    private float refreshTimer = 0;
    private float refreshSpeed = 2f;

    void Start()
    {
        _RefreshHeight();
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer > refreshSpeed)
        {
            refreshTimer = 0;
            _RefreshHeight();
        }
    }

    public override void PostLateUpdate()
    {
        VRCPlayerApi.TrackingData headData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        VRCPlayerApi.TrackingData leftHandData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
        VRCPlayerApi.TrackingData rightHandData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
        Vector3 forward = headData.rotation * Vector3.forward;
        headTransform.SetPositionAndRotation(headData.position, headData.rotation);
        leftHandTransform.SetPositionAndRotation(leftHandData.position, leftHandData.rotation);
        rightHandTransform.SetPositionAndRotation(rightHandData.position, rightHandData.rotation);

        hudTransform.position = headData.position;
        //hudTransform.rotation = Quaternion.Lerp(hudTransform.rotation, headData.rotation, Time.deltaTime * 10);
        hudTransform.rotation = headData.rotation;
        float scale = rawHeight / 1920.0f;
        hudTransform.localScale = new Vector3(scale, scale, scale);
    }

    public void _RefreshHeight()
    {
        float tmpHeight = Mathf.Clamp(rawHeight = GetPlayerHeight(Networking.LocalPlayer), 0.2f, 2);

        if (Math.Abs(tmpHeight - height) > 0.1f)
        {
            height = tmpHeight;
        }
    }

    float GetPlayerHeight(VRCPlayerApi p)
    {
        // check if avatar is invalid
        if (p.GetBonePosition(HumanBodyBones.Head).sqrMagnitude < 0.000001f ||
            p.GetBonePosition(HumanBodyBones.LeftFoot).sqrMagnitude < 0.000001f ||
            p.GetBonePosition(HumanBodyBones.RightFoot).sqrMagnitude < 0.000001f)
        {
            return 1.75f;
        }

        var pLegL = p.GetBonePosition(HumanBodyBones.LeftUpperLeg);
        var pLegR = p.GetBonePosition(HumanBodyBones.RightUpperLeg);
        var pSpine = p.GetBonePosition(HumanBodyBones.Spine);

        var legsMP = Vector3.LerpUnclamped(pLegL, pLegR, 0.5f);

        float lenLegL = MeasureBoneLength2(p, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
        float lenLegR = MeasureBoneLength2(p, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
        float spineLen = MeasureBoneLength(p, HumanBodyBones.Spine, HumanBodyBones.Head) + Vector3.Distance(legsMP, pSpine);

        float legsLen = Mathf.Max(lenLegL, lenLegR);
        float totalLen = legsLen + spineLen;

        return (totalLen > 0.001f ? totalLen : 1f);
    }

    float MeasureBoneLength(VRCPlayerApi p, HumanBodyBones b0, HumanBodyBones b1)
    {
        return Vector3.Distance(p.GetBonePosition(b0), p.GetBonePosition(b1));
    }

    float MeasureBoneLength2(VRCPlayerApi p, HumanBodyBones b0, HumanBodyBones b1, HumanBodyBones b2)
    {
        var p1 = p.GetBonePosition(b1);
        return Vector3.Distance(p.GetBonePosition(b0), p1) + Vector3.Distance(p1, p.GetBonePosition(b2));
    }
}

using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon.Common;

public class MenuController : UdonSharpBehaviour
{
    public RectTransform menu;
    [SerializeField] private PlayerTracker playerHeightCalculator;
    public Transform resetTarget;
    [SerializeField] private Rigidbody body;
    public VRCPickup pickup;
    [SerializeField] private ChatManager chatManager;
    [SerializeField] private KeyCode desktopKey = KeyCode.E;

    [SerializeField] private GameObject distanceHidenObj;
    [SerializeField] private GameObject menuIcon;

    private bool enablePortable = true;
    private bool grabLeft;
    private bool grabRight;
    private bool useLeft;
    private bool useRight;
    private bool isHolding;
    private bool isOpened;
    private bool hasCheckedDist = false;
    private float handDistance;
    private Vector3 menuSize;
    private float menuScale = 1;

    private void Start()
    {
        if (!resetTarget)
        {
            resetTarget = Instantiate(transform.GetChild(0).gameObject).GetComponent<Transform>();
            resetTarget.parent = null;
            resetTarget.position = transform.position;
            resetTarget.rotation = transform.rotation;
            resetTarget.localScale = Vector3.one;
        }
        menuSize = menu.sizeDelta * menu.localScale;
        menuSize.z = 1;
        menuScale = menu.localScale.x;
        pickup.pickupable = false;
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (args.handType == HandType.LEFT)
            useLeft = value;
        else
            useRight = value;
    }

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        if (args.handType == HandType.LEFT)
            grabLeft = value;
        else
            grabRight = value;
    }

    private void Update()
    {
        if (Networking.LocalPlayer.IsUserInVR() && enablePortable)
        {
            if (grabLeft && grabRight && useLeft && useRight)
            {

                VRCPlayerApi.TrackingData leftHand = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
                VRCPlayerApi.TrackingData rightHand = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);

                handDistance = Vector3.Distance(leftHand.position, rightHand.position);

                if (!hasCheckedDist)
                {
                    hasCheckedDist = true;
                    if (handDistance < playerHeightCalculator.rawHeight / 9 && !isHolding)
                    {
                        isHolding = true;
                        isOpened = false;
                        body.isKinematic = true;
                        pickup.pickupable = false;
                        pickup.Drop();
                    }
                }
                

                if (isHolding)
                {
                    Vector3 center = (leftHand.position + rightHand.position) / 2;
                    
                    Vector3 right = (rightHand.position - leftHand.position).normalized;
                    //Vector3 up = (leftHand.rotation * Vector3.right + rightHand.rotation * -Vector3.right).normalized;

                    Vector3 forward =
                        (center - Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position).normalized;

                    Vector3 up = Vector3.Cross(forward, right);

                    transform.rotation = Quaternion.LookRotation(forward, up);

                    float scale = handDistance / menuSize.x;
                    transform.localScale = new Vector3(scale, scale, scale);

                    transform.position = center;
                }
            }
            else
            {
                hasCheckedDist = false;

                if (isHolding && !isOpened)
                {
                    isHolding = false;
                    isOpened = true;
                    body.isKinematic = false;
                    pickup.pickupable = true;
                }
                else if (handDistance < playerHeightCalculator.rawHeight / 9)
                {
                    _CloseMenu();
                }

            }
        }
        else if (!chatManager.inChat && Input.GetKeyDown(desktopKey))
        {
            pickup.pickupable = false;
            if (isOpened)
            {
                _CloseMenu();
            }
            else if(enablePortable)
            {
                isOpened = true;

                VRCPlayerApi.TrackingData head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);

                Vector3 forward = (head.rotation * Vector3.forward).normalized;

                float scale = playerHeightCalculator.rawHeight / (menuSize.x*1.5f);
                transform.localScale = new Vector3(scale, scale, scale);

                transform.rotation = Quaternion.LookRotation(forward, head.rotation * Vector3.up);

                transform.position = (head.position + (forward * playerHeightCalculator.rawHeight / 2));
            }
        }

        if (isOpened || isHolding)
            _UpdateMenuPosition();

        if (isOpened)
        {
            Vector3 head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

            if (Vector3.Distance(head, menu.position) > playerHeightCalculator.rawHeight / 1.25f)
            {
                _CloseMenu();
            }
        }
        
    }

    private void _UpdateMenuPosition()
    {
        menu.localScale = transform.localScale * menuScale;
        menu.rotation = transform.rotation;
        menu.position = transform.position;
    }

    public void _CloseMenu()
    {
        isOpened = false;
        transform.position = resetTarget.position;
        transform.rotation = resetTarget.rotation;
        transform.localScale = resetTarget.localScale;
        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        if (Networking.LocalPlayer.IsUserInVR())
        {
            pickup.Drop();
        }
        pickup.pickupable = false;

        _UpdateMenuPosition();
    }

    public void _EnablePortable()
    {
        enablePortable = true;
    }

    public void _DisablePortable()
    {
        enablePortable = false;
    }

    public override void OnPickup()
    {
        if (isOpened && Networking.LocalPlayer.IsUserInVR())
        {
            pickup.pickupable = false;
        }
    }

    public override void OnDrop()
    {
        if (isOpened && Networking.LocalPlayer.IsUserInVR())
        {
            pickup.pickupable = true;
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (Networking.LocalPlayer != player) return;
        distanceHidenObj.SetActive(true);
        menuIcon.SetActive(false);
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (Networking.LocalPlayer != player) return;
        distanceHidenObj.SetActive(false);
        menuIcon.SetActive(true);
    }
}

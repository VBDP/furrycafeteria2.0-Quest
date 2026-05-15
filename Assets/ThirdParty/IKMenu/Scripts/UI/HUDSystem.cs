
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class HUDSystem : UdonSharpBehaviour
{
    [SerializeField] private GameObject notificationPrefab;
    [SerializeField] private RectTransform notificationsRoot;
    [SerializeField] private AudioSource notificationAudio;
    [SerializeField] private AudioClip infoSound;
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip alertSound;
    [SerializeField] private TextAsset tooltips;
    [SerializeField] private bool enableTooltips = true;

    private void Start()
    {
        if (enableTooltips) _SpawnTooltip();
    }

    public void _SpawnTooltip()
    {
        string[] tips = tooltips.text.Split(new string[] { "\n", "\n\r" }, System.StringSplitOptions.RemoveEmptyEntries);
        _ShowNotification(0, tips[Random.Range(0, tips.Length)]);
    }

    public void _ShowNotification(int type, string text)
    {
        GameObject notification = Instantiate(notificationPrefab);
        notification.transform.SetParent(notificationsRoot);
        notification.transform.localPosition = Vector3.zero;
        notification.transform.localRotation = Quaternion.identity;
        notification.transform.localScale = Vector3.one;
        notification.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = text;

        Animator notifAnimator = notification.GetComponent<Animator>();
        notifAnimator.SetInteger("Type",type);
        notifAnimator.SetTrigger("Popup");

        switch (type)
        {
            case 0: notificationAudio.PlayOneShot(infoSound); break;
            case 1: notificationAudio.PlayOneShot(warningSound); break;
            case 2: notificationAudio.PlayOneShot(alertSound); break;
        }
    }
}

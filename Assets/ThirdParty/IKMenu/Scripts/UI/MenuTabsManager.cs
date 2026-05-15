
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class MenuTabsManager : UdonSharpBehaviour
{
    [SerializeField] private Transform panelsRoot;
    [SerializeField] private Transform tabsRoot;
    private GameObject[] panels;
    private Toggle[] tabs;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        panels = new GameObject[panelsRoot.childCount];
        for (int i = 0; i < panelsRoot.childCount; i++)
            panels[i] = panelsRoot.GetChild(i).gameObject;

        tabs = tabsRoot.GetComponentsInChildren<Toggle>(true);

        foreach (GameObject panel in panels)
        {
            if(panel)
                panel.SetActive(false);
        }
        panels[1].SetActive(true);
        tabs[1].isOn = true;
    }

    public void _TabChanged()
    {
        audioSource.Play();
        int selectedTab = 0;
        foreach (Toggle toggle in tabs)
        {
            if (toggle && toggle.isOn)
                break;
            selectedTab++;
        }

        if (selectedTab >= tabs.Length)
            selectedTab = 0;

        foreach (GameObject panel in panels)
        {
            if(panel)
                panel.SetActive(false);
        }
        panels[selectedTab].SetActive(true);
    }

    public void _ResetTab()
    {
        tabs[0].isOn = true;
        _TabChanged();
    }
}

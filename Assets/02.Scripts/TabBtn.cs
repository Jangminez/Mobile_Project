using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabBtn : MonoBehaviour
{
    private enum Tabs { Weapon, Armor, Potion};
    [SerializeField] Tabs _tab;

    private Button _myBtn;
    public Transform _myTab;
    public Transform[] _otherTabs;

    private ColorBlock _cb;

    public Color32 _onColor;
    public Color32 _offColor;

    private void Awake()
    {
        _myBtn = GetComponent<Button>();

        _myBtn.onClick.AddListener(OpenTab);

        _cb = _myBtn.colors;

        _onColor = new Color32(152, 178, 221, 255);
        _offColor = new Color32(152, 152, 152, 255);

        if (_tab == Tabs.Weapon)
        {
            _cb.normalColor = _onColor;
            _myBtn.colors = _cb;
            _myTab.gameObject.SetActive(true);
        }

    }

    private void Update()
    {
        if (_myTab.gameObject.activeSelf)
        {
            _cb.normalColor = _onColor;
            _myBtn.colors = _cb;
        }

        else
        {
            _cb.normalColor = _offColor;
            _myBtn.colors = _cb;
        }
    }

    void OpenTab()
    {
        _myTab.gameObject.SetActive(true);

        foreach(Transform other in _otherTabs)
        {
            other.gameObject.SetActive(false);
        }
    }
}

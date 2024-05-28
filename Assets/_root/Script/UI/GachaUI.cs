using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaUI : MonoBehaviour {
    
    [Header("# CutScene")]
    [SerializeField] private GameObject camera;
    [SerializeField] private Transform startPos;
    [SerializeField] private Transform endPos;
    [SerializeField] private GameObject ui;
    [SerializeField] private float time;
    [SerializeField] private GameObject light;
    private Vector3 vel = Vector3.zero;
    
    [Header("# Gold")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private int gold;
    
    [Header("# Info")]
    [SerializeField] private GameObject infoPanel;
    private bool infoToggle;
    
    [Header("# Box")]
    [SerializeField] private GameObject box;
    [SerializeField] private List<Mesh> boxMeshes;
    [SerializeField] private List<Material> boxMaterials;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private int boxIndex;

    [Header("# Gacha")]
    [SerializeField] private TextMeshProUGUI singlePriceTxt;
    [SerializeField] private TextMeshProUGUI multiPriceTxt;
    [SerializeField] int gachaPrice;

    private void Start() {
        meshFilter = box.GetComponent<MeshFilter>();
        meshRenderer = box.GetComponent<MeshRenderer>();
        
        singlePriceTxt.text = string.Format($"{gachaPrice:N0}");
        multiPriceTxt.text = string.Format($"<color=#54d5ff><s>{gachaPrice*10:N0}</s></color>\n{gachaPrice*10-1:N0}");
        ChangeGoldTxt(gold);
        ui.SetActive(false);
        GachaToggle(startPos, endPos, time);
    }

    public void GachaToggle(Transform start, Transform end, float t) {
        StartCoroutine(CamMove(start, end));
        StartCoroutine(UiDisable(t));
    }

    IEnumerator CamMove(Transform start, Transform end) {
        float t=0;
        
        while (t < time) {
            t += Time.deltaTime;
            
            camera.transform.position = Vector3.Lerp(start.position, end.position, t/time);
            camera.transform.rotation = Quaternion.Lerp(start.rotation, end.rotation, t/time);
            
            yield return null;
        }
    }

    IEnumerator UiDisable(float s) {
        yield return new WaitForSeconds(s);
        ui.SetActive(!ui.activeSelf);
        light.SetActive(!light.activeSelf);
    }

    public void LeftBtn() {
        if (boxIndex <= 0) return;
        boxIndex--;
        ChangeBox();
    }

    public void RightBtn() {
        if (boxIndex >= boxMeshes.Count-1) return;
        boxIndex++;
        ChangeBox();
    }

    void ChangeBox() {
        meshFilter.mesh = boxMeshes[boxIndex];
        meshRenderer.material = boxMaterials[boxIndex];
    }

    public void DoGacha(int gachaNum) {
        gold -= gachaNum * gachaPrice;
        ChangeGoldTxt(gold);
        // 가챠
    }

    public void BackBtn() {
        // 이전화면
        GachaToggle(endPos, startPos, 0);
    }

    public void ToggleInfo() {
        infoToggle = !infoToggle;
        infoPanel.SetActive(infoToggle);
    }

    public void ChangeGoldTxt(int gold) {
        goldText.text = string.Format($"{gold:N0}");
    }
}
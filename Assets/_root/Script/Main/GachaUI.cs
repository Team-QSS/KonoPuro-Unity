using System;
using System.Collections.Generic;
using System.Linq;
using _root.Script.Card;
using _root.Script.Data;
using _root.Script.Manager;
using _root.Script.Network;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Material = UnityEngine.Material;
using Mesh = UnityEngine.Mesh;

namespace _root.Script.Main
{
    public class GachaUI : MonoBehaviour
    {
        [Header("# Gold")] [SerializeField] private TextMeshProUGUI goldText;

        [Header("# Info")] [SerializeField] private GameObject infoPanel;

        [Header("# Box")] [SerializeField] private GameObject box;
        [SerializeField] private List<Mesh> boxMeshes;
        [SerializeField] private List<Material> boxMaterials;
        [SerializeField] private List<int> boxSizes;
        [SerializeField] private List<Vector3> boxRotation;

        [Header("# Gacha")] [SerializeField] private TextMeshProUGUI singlePriceTxt;
        [SerializeField] private TextMeshProUGUI multiPriceTxt;
        private int boxIndex;
        private Canvas canvas;
        private readonly List<PlayerCardResponse> gatchaCards = new();

        private int gatchaPrice;
        private bool infoToggle;
        private MainUi mainUi;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        
        [SerializeField]private GameObject _changeBanner;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            mainUi = FindObjectOfType<MainUi>();
        }

        private void Start()
        {
            
            meshFilter = box.GetComponent<MeshFilter>();
            meshRenderer = box.GetComponent<MeshRenderer>();
            singlePriceTxt.text = string.Format($"{GameStatics.gatchaOncePrice:N0}");
            multiPriceTxt.text =
                string.Format(
                    $"<color=#54d5ff><s>{GameStatics.gatchaMultiPrice + 1:N0}</s></color>\n{GameStatics.gatchaMultiPrice:N0}");
            SetActive(false);
        }

        public void SetActive(bool active)
        {
            canvas.enabled = active;
            gameObject.SetActive(active);
            if (active) ChangeGoldTxt(UserData.Instance.gold.Value);
        }

        public void LeftBtn()
        {
            if (boxIndex <= 0) return;
            boxIndex--;
            ChangeBox();
            _changeBanner.GetComponent<BannerChanger>().ChageToFront();
        }

        public void RightBtn()
        {
            if (boxIndex >= boxMeshes.Count - 1) return;
            boxIndex++;
            ChangeBox();
            _changeBanner.GetComponent<BannerChanger>().ChangeToBack();
        }

        private void ChangeBox()
        {
            meshFilter.mesh = boxMeshes[boxIndex];
            meshRenderer.material = boxMaterials[boxIndex];
            box.transform.localScale = Vector3.one * boxSizes[boxIndex];
            box.transform.rotation = Quaternion.Euler(boxRotation[boxIndex]);
        }

        private void GatchaInit()
        {
            gatchaPrice = 0;
            gatchaCards.Clear();
        }

        public void Gatcha(bool multi)
        {
            GatchaInit();
            gatchaPrice = multi ? GameStatics.gatchaMultiPrice : GameStatics.gatchaOncePrice;
            if (UserData.Instance.gold < gatchaPrice)
            {
                ModalManager.ShowModal("골드가 부족합니다.", Color.black, Color.white, Tuple.Create("확인", new UnityAction(ModalManager.ResetModal)));
                return;
            }
                //TODO: 골드 부족 표시
            if (multi)
                API.GatchaMulti(GameStatics.gatchaList[boxIndex].id).OnResponse(response =>
                    {
                        gatchaCards.Clear();
                        gatchaCards.AddRange(response.cards);
                        GatchaSuccess();
                    }).OnError(GatchaError)
                    .Build();
            else
                API.GatchaOnce(GameStatics.gatchaList[boxIndex].id).OnResponse(response =>
                    {
                        gatchaCards.Clear();
                        gatchaCards.Add(response);
                        GatchaSuccess();
                    }).OnError(GatchaError)
                    .Build();
            mainUi.SetThrobber(true);
        }

        private void GatchaSuccess()
        {
            mainUi.SetThrobber(false);
            var gold = UserData.Instance.gold -= gatchaPrice;
            ChangeGoldTxt(gold.Value);
            UserData.Instance.InventoryCards.cards.AddRange(gatchaCards);
            GachaDirecting.gatchaCards = gatchaCards;
            GachaDirectionColor.maxTier = gatchaCards.Select(card => card.tier).Max();
            AudioManager.StopAllSoundsInstance();
            SceneManager.LoadScene("GachaDirectingStartScene");
        }

        private void GatchaError(ErrorBody body)
        {
            mainUi.SetThrobber(false);
        }

        public void ToggleInfo()
        {
            infoToggle = !infoToggle;
            infoPanel.SetActive(infoToggle);
        }

        private void ChangeGoldTxt(int gold)
        {
            goldText.text = string.Format($"{gold:N0}");
        }

        public void DoGacha()
        {
            AudioManager.StopAllSoundsInstance();
            SceneManager.LoadScene("GachaScene");
        }
    }
}
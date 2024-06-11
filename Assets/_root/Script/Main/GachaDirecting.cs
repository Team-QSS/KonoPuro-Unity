using System.Collections.Generic;
using System.Globalization;
using _root.Script.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _root.Script.Main
{
    public class GachaDirecting : MonoBehaviour
    {
        public static List<PlayerCardResponse> gatchaCards = new();
        private bool multi;
        public Transform singleCard, singleCardEndPos;
        public Transform multiCard, multiCardEndPos;
        private void Start()
        {
            multi = gatchaCards.Count > 1;
        }

        private void Update()
        {
            if (multi)
            {
                var mc = multiCard.GetComponentsInChildren<MultiComponent>();
                var ec = multiCardEndPos.GetComponentsInChildren<MultiComponent>();
                for (var i = 0; i < mc.Length; i++)
                {
                    mc[i].GetComponent<Transform>().position = Vector3.Lerp(mc[i].GetComponent<Transform>().position, ec[i].GetComponent<Transform>().position, 0.1f);
                    mc[i].GetComponent<Transform>().rotation = Quaternion.Lerp(mc[i].GetComponent<Transform>().rotation, ec[i].GetComponent<Transform>().rotation, 0.1f);
                }
                if (mc[0].GetComponent<Transform>().rotation.ToString().Equals(ec[0].GetComponent<Transform>().rotation.ToString()))
                {
                    SceneManager.LoadScene("CardGatchaMulti");
                }
            }
            else
            {
                singleCard.position = Vector3.Lerp(singleCard.position, singleCardEndPos.position, 0.1f);
                singleCard.rotation = Quaternion.Lerp(singleCard.rotation, singleCardEndPos.rotation, 0.1f);
                if (singleCard.rotation.ToString().Equals(singleCardEndPos.rotation.ToString()))
                {
                    SceneManager.LoadScene("CardGatchaSingle");
                }
            }
        }
    }
}

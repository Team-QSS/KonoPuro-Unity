using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using UnityEngine;

namespace _root.Script.Main
{
    public class CinemacineController : MonoBehaviour
    {
        public enum VCamName
        {
            None,
            Overview,
            Deck,
            Gatcha,
            Matching
        }

        private List<CinemachineVirtualCamera> vCams;

        private void Awake()
        {
            vCams = FindObjectsOfType<CinemachineVirtualCamera>().ToList();
        }

        private void Start()
        {
            SetPriority(VCamName.Overview);
        }

        public void SetPriority(VCamName cam)
        {
            foreach (var vCam in vCams) vCam.Priority = 10;

            vCams.Find(x => x.gameObject.name == cam.ToString()).Priority = 20;
        }
    }
}
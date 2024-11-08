using _root.Script.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _root.Script.Main
{
    public class MainUi : MonoBehaviour
    {
        private GameObject exitButton;
        private GameObject interactQuitButton;
        private GameObject matchingCancelButton;
        private Throbber throbber;
        private GameObject titleButton;

        private void Awake()
        {
            titleButton = transform.GetChild(2).gameObject;
            exitButton = transform.GetChild(1).gameObject;
            interactQuitButton = transform.GetChild(0).gameObject;
            matchingCancelButton = transform.GetChild(3).gameObject;
            throbber = GetComponentInChildren<Throbber>();
        }

        private void Start()
        {
            SetInteractQuitButton(false);
            SetMatchingCancelButton(false);
        }

        public void SetThrobber(bool active)
        {
            throbber.SetActive(active);
        }

        public void SetInteractQuitButton(bool active)
        {
            interactQuitButton.SetActive(active);
        }

        public void SetMatchingCancelButton(bool active)
        {
            matchingCancelButton.SetActive(active);
        }

        public void SetTitleButton(bool active)
        {
            titleButton.SetActive(active);
        }

        public void SetExitButton(bool active)
        {
            exitButton.SetActive(active);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        public void GoToTitle()
        {
            SceneManager.LoadScene("TitleScene");
        }
    }
}
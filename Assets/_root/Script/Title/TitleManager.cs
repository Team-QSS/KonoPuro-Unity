using System.Collections;
using System.Linq;
using _root.Script.Data;
using _root.Script.Manager;
using _root.Script.Network;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace _root.Script.Title
{
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private PlayableDirector director;

        [SerializeField] private PlayableAsset start;
        [SerializeField] private PlayableAsset end;
        [SerializeField] private GameObject LoginFailed;
        [SerializeField] private GameObject LoginSuccess;
        private AuthPanel authPanel;
        private bool loginSuccess;
        private TitleUi titleUi;

        private void Awake()
        {
            titleUi = FindObjectOfType<TitleUi>();
            authPanel = FindObjectOfType<AuthPanel>();
            var spotLight = FindObjectsOfType<Light>();
            spotLight.ToList().First(x => x.type == LightType.Spot).intensity = 0;
        }

        // Start is called before the first frame update
        private void Start()
        {
            StartCoroutine(StartFlow());
        }

        private IEnumerator StartFlow()
        {
            AudioManager.SetAsBackgroundMusicInstance("Audio/PURO_BUILDUP");
            AudioManager.AddEventHandlerInstance((_) =>
            {
                AudioManager.SetAsBackgroundMusicInstance("Audio/PURO_LOOP", true);
                AudioManager.ClearEventHandlerInstance();
            });
            yield return new WaitForSeconds(1f);
            var loaded = false;
            API.GetVersion()
                .OnResponse(s =>
                {
                    if (s.version != Application.version)
                        //TODO: 버전 안 맞음 표시
                        Application.Quit();
                    else loaded = true;
                })
                .OnError(body => Debug.LogError("Version Load Failed"))
                .Build();
            yield return new WaitUntil(() => loaded);
            var cutSceneEnd = false;
            director.playableAsset = start;
            director.Play();
            director.stopped += _ => cutSceneEnd = true;
            yield return new WaitUntil(() => cutSceneEnd);
            if (Networking.AccessToken == null) authPanel.Show(true);
            else titleUi.Login(false);
        }

        public void Exit()
        {
            Application.Quit();
        }

        public void GameStart()
        {
            AudioManager.ClearEventHandlerInstance();
            AudioManager.StopAllSoundsInstance();
            AudioManager.PlaySoundInstance("Audio/GAME_START");
            titleUi.gameObject.SetActive(false);
            LoginFailed.SetActive(false);
            LoginSuccess.SetActive(false);
            StartCoroutine(GameStartFlow());
        }

        private IEnumerator GameStartFlow()
        {
            StartCoroutine(LoadCoroutine());

            // Debug.LogError("Tiers");
            // foreach (var (key, value) in GameStatics.tierDictionary)
            // {
            // 	Debug.LogWarning($"Outer Key : {key}");
            // 	Debug.Log($"Name : {value.name}");
            // 	Debug.Log($"Time : {value.time}");
            // 	Debug.Log($"Description : {value.description}");
            // }
            //
            // Debug.LogError("Passives");
            // foreach (var (key, value) in GameStatics.passiveDictionary)
            // {
            // 	Debug.LogWarning($"Outer Key : {key}");
            // 	Debug.Log($"Name : {value.name}");
            // 	Debug.Log($"Description : {value.description}");
            // }
            //
            // Debug.LogError("Default Cards");
            // foreach (var (key, value) in GameStatics.defaultCardDictionary)
            // {
            // 	Debug.LogWarning($"Outer Key : {key}");
            // 	Debug.Log($"Name : {value.name}");
            // 	Debug.Log($"Time : {value.time}");
            // 	Debug.Log($"Description : {value.description}");
            // }

            yield return new WaitForSeconds(2f);
            director.playableAsset = end;
            director.Play();
            yield return new WaitForSeconds(2.5f);
            yield return new WaitUntil(() => UserData.Instance.ActiveDeck != null &&
                                             UserData.Instance.InventoryCards != null && GameStatics.gatchaList != null);
            SceneManager.LoadScene("MainScene");
        }

        private void LoadData()
        {
            if (UserData.Instance.ActiveDeck == null)
                API.GetActiveDeck()
                    .OnResponse(response => UserData.Instance.ActiveDeck = response)
                    .OnError(body => Debug.Log("Active Deck Load Failed"))
                    .Build();

            if (UserData.Instance.InventoryCards == null)
                API.GetInventoryCardAll()
                    .OnResponse(responses => UserData.Instance.InventoryCards = responses)
                    .OnError(body => Debug.LogError("Inventory Cards Load Failed"))
                    .Build();

            if (GameStatics.gatchaList == null)
                API.GatchaList()
                    .OnResponse(responses => { GameStatics.gatchaList = responses.data; })
                    // .OnResponse(responses => GameStatics.gatchaList = responses.data
                    //                                                            .Where(x =>
                    // 		                                                                DateTime
                    // 				                                                               .Compare(DateTime.Parse(x.startAt),
                    // 						                                                                 DateTime
                    // 								                                                                .Now) !=
                    // 		                                                                1 &&
                    // 		                                                                DateTime
                    // 				                                                               .Compare(DateTime.Parse(x.endAt),
                    // 						                                                                 DateTime
                    // 								                                                                .Now) !=
                    // 		                                                                -1).ToList())
                    .OnError(body => Debug.Log("Gatcha List Load Failed"))
                    .Build();

            if (GameStatics.tierDictionary == null)
                API.GetTiers()
                    .OnResponse(responses =>
                    {
                        // Debug.LogError("Tiers");
                        //   foreach (var (key, value) in responses)
                        //   {
                        //    Debug.LogWarning($"Outer Key : {key}");
                        //    Debug.Log($"Name : {value.name}");
                        //    Debug.Log($"Time : {value.time}");
                        //    Debug.Log($"Description : {value.description}");
                        //   }
                        GameStatics.tierDictionary = responses;
                    })
                    .OnError(body => Debug.Log("Tiers Load Failed"))
                    .Build();

            if (GameStatics.passiveDictionary == null)
                API.GetPassives()
                    .OnResponse(responses =>
                    {
                        // Debug.LogError("Passives");
                        //   foreach (var (key, value) in responses)
                        //   {
                        //    Debug.LogWarning($"Outer Key : {key}");
                        //    Debug.Log($"Name : {value.name}");
                        //    Debug.Log($"Description : {value.description}");
                        //   }
                        GameStatics.passiveDictionary = responses;
                    })
                    .OnError(body => Debug.Log("Passives Load Failed"))
                    .Build();

            if (GameStatics.defaultCardDictionary == null)
                API.GetDefaultCards()
                    .OnResponse(responses =>
                    {
                        // Debug.LogError("Default Cards");
                        //   foreach (var (key, value) in responses)
                        //   {
                        //    Debug.LogWarning($"Outer Key : {key}");
                        //    Debug.Log($"Name : {value.name}");
                        //    Debug.Log($"Time : {value.time}");
                        //    Debug.Log($"Description : {value.description}");
                        //   }
                        GameStatics.defaultCardDictionary = responses;
                    })
                    .OnError(body => Debug.Log("Default Cards Load Failed"))
                    .Build();

            if (GameStatics.studentCardDictionary == null)
                API.GetStudentCards()
                    .OnResponse(responses =>
                    {
                        // Debug.LogError("Default Cards");
                        //   foreach (var (key, value) in responses)
                        //   {
                        //    Debug.LogWarning($"Outer Key : {key}");
                        //    Debug.Log($"Name : {value.name}");
                        //    Debug.Log($"Description : {value.description}");
                        //    Debug.Log($"Idea : {value.idea}");
                        //    Debug.Log($"Motive : {value.motive}");
                        //   }
                        GameStatics.studentCardDictionary = responses;
                    })
                    .OnError(body => Debug.Log("Default Cards Load Failed"))
                    .Build();

            if (UserData.Instance.gold == null)
                API.GetGold()
                    .OnResponse(response =>
                    {
                        // Debug.LogError("Default Cards");
                        //   foreach (var (key, value) in responses)
                        //   {
                        //    Debug.LogWarning($"Outer Key : {key}");
                        //    Debug.Log($"Name : {value.name}");
                        //    Debug.Log($"Description : {value.description}");
                        //    Debug.Log($"Idea : {value.idea}");
                        //    Debug.Log($"Motive : {value.motive}");
                        //   }
                        UserData.Instance.gold = response.gold;
                        Debug.LogError(UserData.Instance.gold); })
                    .OnError(body => Debug.Log("Gold Load Failed"))
                    .Build();
        }

        private IEnumerator LoadCoroutine()
        {
            const float iterTime = 1f;
            const int iterCount = 15;

            LoadData();
            for (var i = 0; i < iterCount; i++)
            {
                yield return new WaitForSeconds(iterTime);
                if (UserData.Instance.ActiveDeck != null && UserData.Instance.InventoryCards != null &&
                    GameStatics.gatchaList != null && GameStatics.tierDictionary != null &&
                    GameStatics.passiveDictionary != null && GameStatics.defaultCardDictionary != null &&
                    GameStatics.studentCardDictionary != null)
                    yield break;
            }

            Application.Quit();
        }

        public void Sign(bool signUp)
        {
            if (signUp)
            {
                var post = authPanel.SignUp();
                if (post == null) return;

                titleUi.SetThrobber(true);
                post.OnSuccess(SignUpSuccess).OnError(SignUpError).Build();
            }
            else
            {
                var post = authPanel.SignIn();
                if (post == null) return;

                titleUi.SetThrobber(true);
                post.OnResponse(SignInSuccess).OnError(SignInError).Build();
            }
        }

        private void SignInSuccess(TokenResponse response)
        {
            titleUi.SetThrobber(false);
            Networking.AccessToken = response.accessToken;
            titleUi.Login(false);
            LoginSuccess.SetActive(true);
            loginSuccess = true;
            StopAllCoroutines();
            StartCoroutine(Login());
        }

        private void SignInError(ErrorBody errorBody)
        {
            titleUi.SetThrobber(false);
            LoginFailed.SetActive(true);
            loginSuccess = false;
            StopAllCoroutines();
            StartCoroutine(Login());
        }

        private void SignUpSuccess()
        {
            titleUi.SetThrobber(false);
            AuthPanel.instance.ShowSignUp(false);
        }

        private void SignUpError(ErrorBody errorBody)
        {
            titleUi.SetThrobber(false);
        }

        private IEnumerator Login()
        {
            if (loginSuccess)
            {
                yield return new WaitForSeconds(2f);
                LoginSuccess.SetActive(false);
            }
            else
            {
                yield return new WaitForSeconds(2f);
                LoginFailed.SetActive(false);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using _root.Script.Client;
using _root.Script.Data;
using _root.Script.Manager;
using _root.Script.Network;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace _root.Script.Ingame
{
    
    public class IngameManager : MonoBehaviour
    {
        [SerializeField] private PlayableAsset start;

        public List<GameStudentCard> selfStudents = new();
        public List<GameStudentCard> otherStudents = new();

        //TODO: 실험용 삭제필요
        [SerializeField] private bool spriteDebug;
        [SerializeField] private Light light1;
        [SerializeField] private Light light2;

        [FormerlySerializedAs("oponentusedcard")] [SerializeField]
        private TextMeshProUGUI usedcard;

        private readonly List<int> flowIndexes = new();

        

        private bool abilityUsable;
        private PlayerActivity activity;
        private Camera cam;
        private bool canUseFlow;
        private Animator carduseageAnim;
        private int currentFlowIndex;
        private int day;
        private PlayableDirector director;
        private DrawDeck otherDeck;
        private FieldSetter otherField;

        private bool preTurn;

        private DrawDeck selfDeck;

        private FieldSetter selfField;
        private TextMeshProUGUI turnhandcarduse;

        private IngameUi ui;

        private bool updated = true;

        private void Awake()
        {
            var decks = FindObjectsOfType<DrawDeck>();
            selfDeck = decks.First(x => x.isMine);
            otherDeck = decks.First(x => !x.isMine);

            var fields = FindObjectsOfType<FieldSetter>();
            selfField = fields.First(x => x.isMine);
            otherField = fields.First(x => !x.isMine);

            director = GetComponent<PlayableDirector>();
            activity = GetComponent<PlayerActivity>();

            ui = FindObjectOfType<IngameUi>();

            var lights = FindObjectsOfType<Light>();
            foreach (var areaLight in lights)
                areaLight.intensity = 0;

            if (!spriteDebug)
            {
                var sprites = FindObjectsOfType<SpriteRenderer>().Where(x => !x.GetComponent<Card.Card>());
                foreach (var spriteRenderer in sprites)
                    Destroy(spriteRenderer);
            }

            FindObjectsOfType<Canvas>().First(x => x.gameObject.name == "Field Canvas").enabled = false;

            //TODO: 빌드시에 포함 고려 (커서가 화면 밖으로 안나가는 기능)
            // Cursor.lockState = CursorLockMode.Confined;
        }

        private void Start()
        {
            cam = Camera.main;
            NetworkClient.DelegateEvent(NetworkClient.ClientEvent.OtherCardUse, OtherCardUse);
            NetworkClient.DelegateEvent(NetworkClient.ClientEvent.OtherAbilityUse, OtherAbilityUse);
            NetworkClient.DelegateEvent(NetworkClient.ClientEvent.NextDay, _ => NextDay());
            NetworkClient.DelegateEvent(NetworkClient.ClientEvent.DataUpdated, UpdateData);
            NetworkClient.DelegateEvent(NetworkClient.ClientEvent.GameEnd, GameEnd);
            
            light1.color = Color.white;
            light2.color = Color.white;

            GameStart();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current.IsPointerOverGameObject()) return;
            var hitSuccess = Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out var hit);
            if (hitSuccess)
            {
                var ingameCard = hit.transform.GetComponent<IngameCard>();
                if (abilityUsable && ingameCard && ingameCard.type == IngameCardType.Student && ingameCard.isMine)
                {
                    ui.SetCardInfo(ingameCard);
                    ShowAbilities(ingameCard);
                }
                else if (ingameCard && ingameCard.type == IngameCardType.Field)
                {
                    ShowAbilities(null);
                    ui.SetCardInfo(ingameCard);
                }
                else
                {
                    ShowAbilities(null);
                    var card = activity.SelectCard(ingameCard);
                    if (card && GameStatics.isTurn && canUseFlow)
                        StartCoroutine(UseCardFlow(card, GetFlowIndex()));
                }

                return;
            }

            ShowAbilities(null);
            activity.SelectCard(null);
        }

        void OnEnable()
        {
            Debug.Log("OnEnable");
            
        }

        private int GetFlowIndex()
        {
            var index = flowIndexes.Count > 0 ? flowIndexes.Max() + 1 : 0;
            flowIndexes.Add(index);
            return index;
        }

        private void EndFlow(int index)
        {
            flowIndexes.Remove(index);
            if (flowIndexes.Count == 0) currentFlowIndex = 0;
            else currentFlowIndex = index + 1;
        }

        private void GameStart()
        {
            //TODO: 실험용 삭제 필요
            if (GameStatics.self == null || GameStatics.other == null)
            {
                List<GameStudentCard> student = new()
                {
                    new GameStudentCard { tiers = new List<Tiers> { Tiers.Android, Tiers.Backend } },
                    new GameStudentCard(), new GameStudentCard(), new GameStudentCard(), new GameStudentCard()
                };
                selfStudents = student;
                otherStudents = student;

                StartCoroutine(StartFlow(new List<GameCard> { new(), new(), new(), new(), new() },
                    new List<GameCard> { new GameCard(), new GameCard(), new GameCard(), new GameCard(), new GameCard() }));

                GameStatics.isTurn = true;
                return;
            }

            var selfStudent = GameStatics.self?.student;
            var otherStudent = GameStatics.other?.student;
            selfStudents = selfStudent?.cards;
            otherStudents = otherStudent?.cards;
            var selfHeldCards = GameStatics.self?.heldCards;
            var otherHeldCards = GameStatics.other?.heldCards?.cards;
            if (selfHeldCards == null || otherHeldCards == null)
            {
                Debug.LogError("game start held cards data is null");
                return;
            }

            StartCoroutine(StartFlow(selfHeldCards.cards, otherHeldCards));
        }

        private IEnumerator StartFlow(List<GameCard> selfHand, List<GameCard> otherHand)
        {
            activity.SetActive(false);
            ui.SetInteract(false);
            ui.SetHover(false);

            ui.Init();

            yield return new WaitForSeconds(1f);

            director.playableAsset = start;
            director.Play();

            AudioManager.PlaySoundInstance("Audio/INGAME_START");

            yield return new WaitForSeconds(1f);

            StartCoroutine(SetStudent(true));
            StartCoroutine(SetStudent(false));

            yield return new WaitForSeconds(1f);

            selfDeck.Init();
            otherDeck.Init();

            selfDeck.DrawCards(activity.AddHandCard, false, selfHand);
            otherDeck.DrawCards(activity.AddHandCard, false, otherHand);

            yield return new WaitForSeconds(3f);

            preTurn = GameStatics.isTurn;
            ui.DisplayTurn(preTurn);

            yield return new WaitForSeconds(1f);

            ui.SetHover(true);
            ui.SetInteract(preTurn);
            activity.SetActive(true);
            canUseFlow = true;
            abilityUsable = true;

            GameStatics.self = null;
            GameStatics.other = null;
        }

        private IEnumerator SetStudent(bool isMine)
        {
            var students = isMine ? selfStudents : otherStudents;
            var field = isMine ? selfField : otherField;

            foreach (var student in students)
            {
                AudioManager.PlaySoundInstance("Audio/CARD_SETTING");
                field.AddNewCard(student);
                yield return new WaitForSeconds(0.25f);
            }
        }

        private void ShowAbilities(IngameCard card)
        {
            ui.SetAbilities(card?.GetStudentData(), ui.SelectAbility, ability => UseAbility(ability, card));
        }

        private void UseAbility(Tiers ability, IngameCard card)
        {
            if (canUseFlow && preTurn) StartCoroutine(UseAbilityFlow(ability, card, GetFlowIndex()));
            //TODO: 플로우 진행 중 상호작용 불가 or 턴이 아님 표시
        }

        private void NextDay()
        {
            StartCoroutine(NextDayFlow(GetFlowIndex()));
        }

        private void UpdateData(object data)
        {
            UpdatedData self;
            UpdatedData other;
            (self, other) = ((UpdatedData, UpdatedData))data;

            StartCoroutine(DataUpdateFlow(self, other, GetFlowIndex()));
        }

        private void OtherCardUse(object card)
        {
            StartCoroutine(OtherCardUseFlow((GameCard)card, GetFlowIndex()));
        }

        private void OtherAbilityUse(object data)
        {
            Tiers tier;
            GameStudentCard activeStudent;
            (tier, activeStudent) = ((Tiers, GameStudentCard))data;
            StartCoroutine(OtherAbilityUseFlow(tier, activeStudent, GetFlowIndex()));
        }

        private void GameEnd(object info)
        {
            StartCoroutine(GameEndFlow((string)info, GetFlowIndex()));
        }

        private IEnumerator UseAbilityFlow(Tiers ability, IngameCard card, int index)
        {
            if(!updated) yield break;
            
            yield return new WaitUntil(() => currentFlowIndex == index);

            abilityUsable = false;
            ui.SetCardInfo(null);
            activity.SetActive(false);
            ui.SetHover(false);
            ui.SetInteract(false);
            ShowAbilities(null);

            //TODO: 능력 사용 유형에 따라 선택할 카드들 지정
            // var selfStudents  = selfField.GetStudentCards();
            // var otherStudents = otherField.GetStudentCards();

            bool? selection = null;
            List<IngameCard> selectedCards = new();

            var selectionModeUi = ui.GetSelectionModeUi();
            selectionModeUi.SetActive((b, cards) =>
            {
                selection = b;
                selectedCards = cards;
            });

            yield return new WaitUntil(() => selection != null);

            if (selection == false)
            {
                ui.SetHover(true);
                ui.SetInteract(preTurn);
                activity.SetActive(true);
                canUseFlow = true;
                abilityUsable = true;
                EndFlow(index);
                yield break;
            }
            
            //TODO 시간이 모자람 공지
            if (GameStatics.tierDictionary[ability].time > GameStatics.self.time)
            {
                Debug.LogWarning("시간 부족");
                
                abilityUsable = true;
                ui.SetHover(true);
                ui.SetInteract(preTurn);
                activity.SetActive(true);
                activity.AddHandCard(card, true);
                card.Show(true);
                canUseFlow = true;
                EndFlow(index);
                yield break;
            }

            updated = false;
            
            AudioManager.PlaySoundInstance("Audio/CARD_USED");
            ui.SayOutLoud(GameStatics.tierDictionary[ability].name, true);

            if (selectedCards != null)
                NetworkClient.Send(RawProtocol.Of(104, card.GetStudentData().id, ability.ToString(),
                    selectedCards.Select(x => x.GetStudentData().id)));
            else
                NetworkClient.Send(RawProtocol.Of(104, card.GetStudentData().id, ability.ToString()));

            //Dictionary<int, string>
            //dictionary[10]
            //GameStatics.studentCardDictionary[new GameStudentCard().cardType].;

            yield return new WaitForSeconds(1f);

            abilityUsable = true;
            ui.SetHover(true);
            activity.SetActive(true);

            EndFlow(index);
        }

        private IEnumerator UseCardFlow(IngameCard card, int index)
        {
            if(!updated) yield break;
            yield return new WaitUntil(() => currentFlowIndex == index);

            abilityUsable = false;
            canUseFlow = false;
            activity.RemoveHandCard(card, true);
            activity.SetActive(false);
            ui.SetHover(false);
            ui.SetInteract(false);
            card.Show(false);

            //TODO: 카드 사용 유형에 따라 선택할 카드들 지정
            // var selfStudents  = selfField.GetStudentCards();
            // var otherStudents = otherField.GetStudentCards();

            bool? selection = null;
            List<IngameCard> selectedCards = new();

            var selectionModeUi = ui.GetSelectionModeUi();
            selectionModeUi.SetActive((b, cards) =>
            {
                selection = b;
                selectedCards = cards;
            });

            yield return new WaitUntil(() => selection != null);

            if (selection == false)
            {
                abilityUsable = true;
                ui.SetHover(true);
                ui.SetInteract(preTurn);
                activity.SetActive(true);
                activity.AddHandCard(card, true);
                card.Show(true);
                canUseFlow = true;
                EndFlow(index);
                yield break;
            }

            //TODO 시간이 모자람 공지
            var cd = card.GetCardData();
            Debug.LogWarning(cd);
            var def = cd.defaultCardType;
            Debug.LogWarning(def);
            var sel = GameStatics.defaultCardDictionary[def];
            Debug.LogWarning(sel);
            Debug.LogWarning(sel.time);
            Debug.LogWarning(GameStatics.selfTime);
            if (sel.time > GameStatics.selfTime)
            {
                Debug.LogWarning("시간 부족");
                
                abilityUsable = true;
                ui.SetHover(true);
                ui.SetInteract(preTurn);
                activity.SetActive(true);
                activity.AddHandCard(card, true);
                card.Show(true);
                canUseFlow = true;
                EndFlow(index);
                yield break;
            }

            updated = false;

            AudioManager.PlaySoundInstance("Audio/CARD_USED");
            ui.SayOutLoud(GameStatics.defaultCardDictionary[card.GetCardData().defaultCardType].name, true);

            if (selectedCards != null)
                NetworkClient.Send(RawProtocol.Of(103, card.GetCardData().id,
                    selectedCards.Select(x => x.GetStudentData().id)));
            else
                NetworkClient.Send(RawProtocol.Of(103, card.GetCardData().id));

            card.transform.position = new Vector3(-2, 8, 7);
            card.Show(true);

            yield return new WaitForSeconds(1.5f);

            card.Show(false, true);

            ui.SetHover(true);
            activity.SetActive(true);

            EndFlow(index);
        }

        private IEnumerator NextDayFlow(int index)
        {
            yield return new WaitUntil(() => currentFlowIndex == index);

            ui.SetInteract(false);

            Debug.LogError("NextDay");

            AudioManager.PlaySoundInstance("Audio/NEXT_DAY");
            day++;

            if (day == GameStatics.dDay)
            {
                AudioManager.SetAsBackgroundMusicInstance("Audio/LAST_DAY", true);
                for (float i = 0; i < 4; i += Time.deltaTime)
                {
                    light1.color = Color.Lerp(light1.color, Color.red, 0.5f * Time.deltaTime);
                    light2.color = Color.Lerp(light2.color, Color.red, 0.5f * Time.deltaTime);
                    yield return null;
                }

                light1.color = Color.red;
                light2.color = Color.red;
            }
            else
            {
                for (float i = 0; i < 2.5; i += Time.deltaTime)
                {
                    light1.color = Color.Lerp(light1.color, Color.black, 1f * Time.deltaTime);
                    light2.color = Color.Lerp(light2.color, Color.black, 1f * Time.deltaTime);
                    yield return null;
                }

                light1.color = Color.black;
                light2.color = Color.black;
                yield return new WaitForSeconds(0.5f);
                for (float i = 0; i < 2.5; i += Time.deltaTime)
                {
                    light1.color = Color.Lerp(light1.color, Color.white, 1f * Time.deltaTime);
                    light2.color = Color.Lerp(light2.color, Color.white, 1f * Time.deltaTime);
                    yield return null;
                }

                light1.color = Color.white;
                light2.color = Color.white;
            }

            Debug.Log(day);

            ui.DayChange(day);

            yield return new WaitForSeconds(2f);

            EndFlow(index);
        }

        private IEnumerator DataUpdateFlow(UpdatedData self, UpdatedData other, int index)
        {
            yield return new WaitUntil(() => currentFlowIndex == index);

            if (other?.sleep != null)
            {
                ui.TimeChanged(0, false);
                yield return new WaitForSeconds(1f);
            }

            if (self?.sleep != null)
            {
                ui.TimeChanged(0, true);
                yield return new WaitForSeconds(1f);
            }

            if (other?.fieldCards != null)
            {
                otherField.UpdateField(other.fieldCards.cards);
                yield return new WaitForSeconds(1f);
            }

            if (self?.fieldCards != null)
            {
                selfField.UpdateField(self.fieldCards.cards);
                yield return new WaitForSeconds(1f);
            }

            if (other?.heldCards != null)
            {
                DrawCard(other.heldCards.cards, false, other.deckSize == 0);
                yield return new WaitForSeconds(1f);
            }

            if (self?.heldCards != null)
            {
                DrawCard(self.heldCards.cards, true, self.deckSize == 0);
                yield return new WaitForSeconds(1f);
            }

            TimeChanged(self?.time, other?.time);
            yield return new WaitForSeconds(1f);

            ProjectUpdate(self?.projects, other?.projects);
            yield return new WaitForSeconds(1f);

            var turn = GameStatics.isTurn;
            if (turn != preTurn)
            {
                preTurn = turn;
                ui.DisplayTurn(turn);
                yield return new WaitForSeconds(1f);
            }

            ui.SetInteract(turn);
            canUseFlow = true;
            abilityUsable = true;

            updated = true;

            EndFlow(index);
        }

        private IEnumerator OtherAbilityUseFlow(Tiers ability, GameStudentCard activeStudent, int index)
        {
            yield return new WaitUntil(() => currentFlowIndex == index);

            AudioManager.PlaySoundInstance("Audio/CARD_USED");
            ui.SayOutLoud(GameStatics.tierDictionary[ability].name, false);

            //TODO: 능력 사용 연출
            yield return new WaitForSeconds(1f);
            EffectForCard();
            EndFlow(index);
        }

        private IEnumerator OtherCardUseFlow(GameCard cardData, int index)
        {
            yield return new WaitUntil(() => currentFlowIndex == index);

            var card = activity.RemoveHandCard(cardData.id, false);
            card.LoadDisplay(cardData);
            card.MoveByRichTime(new Vector3(-2, 8, 7), Quaternion.Euler(-90, 0, 90), .5f, .5f);

            AudioManager.PlaySoundInstance("Audio/CARD_USED");
            ui.SayOutLoud(GameStatics.defaultCardDictionary[cardData.defaultCardType].name, false);

            yield return new WaitForSeconds(1.5f);

            card.Show(false, true);

            EndFlow(index);
        }

        private IEnumerator GameEndFlow(string info, int index)
        {
            yield return new WaitUntil(() => currentFlowIndex == index);

            ui.SetInteract(false);
            ui.SetHover(false);
            activity.SetActive(false);
            canUseFlow = false;
            abilityUsable = false;

            //TODO: 승패 연출
            yield return new WaitForSeconds(1f);

            ui.SetGameEnd(true, info);

            EndFlow(index);
            currentFlowIndex = -1;
        }

        private void ProjectUpdate(Dictionary<MajorType, int> self, Dictionary<MajorType, int> other)
        {
            if (self != null) ui.SetProgressDetail(self, true);
            if (other != null) ui.SetProgressDetail(other, false);
        }

        private void DrawCard(IReadOnlyCollection<GameCard> cards, bool self, bool last)
        {
            var handCards = activity.GetHandCards(self);

            var ids = cards.Select(x => x.id);
            var handIds = handCards.Select(x => x.id);
            var drawIds = ids.Except(handIds);
            (self ? selfDeck : otherDeck).DrawCards(activity.AddHandCard, last, cards.Where(x => drawIds.Contains(x.id)));
        }

        private void TimeChanged(int? self, int? other)
        {
            if (self != null) ui.TimeChanged(self.Value, true);

            if (other != null) ui.TimeChanged(other.Value, false);
        }

        public void Sleep()
        {
            if (!preTurn) return;
            ui.SetInteract(false);
            canUseFlow = false;
            NetworkClient.Send(RawProtocol.Of(105, null));
        }

        private void OtherSleep()
        {
            ui.TimeChanged(0, false);
        }
        

         public void EffectForCard()
         {
             Debug.Log("Effect!!");
             
             
         } 
    }
}
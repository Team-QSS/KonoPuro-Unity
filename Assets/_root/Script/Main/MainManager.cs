using System;
using System.Collections;
using System.Linq;
using _root.Script.Data;
using _root.Script.Network;
using UnityEngine;
using UnityEngine.Playables;

public class MainManager : MonoBehaviour
{
	private MainUi               mainUi;
	private CinemacineController cineController;
	private PlayableDirector     director;

	[SerializeField] private PlayableAsset start;

	private PlaceableObject hoveredPlaceableObject;
	private Camera          mainCam;

	private bool isInteracting;

	private void Awake()
	{
		mainUi         = FindObjectOfType<MainUi>();
		cineController = FindObjectOfType<CinemacineController>();
		director       = FindObjectOfType<PlayableDirector>();
		var spotLight = FindObjectsOfType<Light>();
		spotLight.ToList().First(x => x.type == LightType.Spot).intensity = 0;
	}

	private void Start()
	{
		mainCam = Camera.main;
		StartCoroutine(StartFlow());
	}

	private void Update()
	{
		CheckPlaceable();
	}

	private void CheckPlaceable()
	{
		if (isInteracting) return;
		var ray = mainCam.ScreenPointToRay(Input.mousePosition);

		if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

		if (Physics.Raycast(ray, out var hit))
		{
			var placeble = hit.transform.GetComponent<PlaceableObject>();
			if (!placeble) return;
			if (hoveredPlaceableObject)
			{
				if (placeble != hoveredPlaceableObject)
				{
					hoveredPlaceableObject.OnHover(false);
					placeble.OnHover(true);
					hoveredPlaceableObject = placeble;
				}
			}
			else
			{
				placeble.OnHover(true);
				hoveredPlaceableObject = placeble;
			}
		}
		else
		{
			return;
		}

		if (!hoveredPlaceableObject || !Input.GetMouseButtonDown(0)) return;
		var cam = hoveredPlaceableObject.Interact();
		if (cam == CinemacineController.VCamName.None) return;

		isInteracting = true;
		mainUi.SetInteractQuitButton(true);
		cineController.SetPriority(cam);
	}

	public void QuitInteract()
	{
		if (!isInteracting) return;
		isInteracting = false;
		mainUi.SetInteractQuitButton(false);
		hoveredPlaceableObject.Init();
		cineController.SetPriority(CinemacineController.VCamName.Overview);
	}

	private IEnumerator StartFlow()
	{
		isInteracting = true;
		yield return new WaitForSeconds(1f);
		director.playableAsset = start;
		director.Play();
		director.stopped += (_ => isInteracting = false);
	}
}
﻿
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace CoffeeCommand {



	public class CoffeeCommandView : MonoBehaviour, PlacenoteListener
	{
		public static CoffeeCommandView inst;

		public GameObject onionPrefab;
		[SerializeField] GameObject mMapSelectedPanel;
		[SerializeField] GameObject mInitButtonPanel;
		[SerializeField] GameObject mMappingButtonPanel;
		[SerializeField] GameObject mMapListPanel;
		[SerializeField] GameObject mExitButton;
		[SerializeField] GameObject mListElement;
		[SerializeField] RectTransform mListContentParent;
		[SerializeField] ToggleGroup mToggleGroup;
	//	[SerializeField] GameObject mPlaneDetectionToggle;
		[SerializeField] Text mLabelText;
		[SerializeField] public Material mShapeMaterial;
		[SerializeField] PlacenoteARGeneratePlane mPNPlaneManager;
	//	[SerializeField] Slider mRadiusSlider;
		[SerializeField] float mMaxRadiusSearch;
	//	[SerializeField] Text mRadiusLabel;

		private UnityARSessionNativeInterface mSession;
		private bool mFrameUpdated = false;
		private UnityARImageFrameData mImage = null;
		private UnityARCamera mARCamera;
		private bool mARKitInit = false;
		private List<GameObject> shapeObjList = new List<GameObject> ();
		private LibPlacenote.MapMetadataSettable mCurrMapDetails;

		private bool mReportDebug = false;

		private LibPlacenote.MapInfo mSelectedMapInfo;
		private string mSelectedMapId {
			get {
				return mSelectedMapInfo != null ? mSelectedMapInfo.placeId : null;
			}
		}
		private string mSaveMapId = null;


		private BoxCollider mBoxColliderDummy;
		private SphereCollider mSphereColliderDummy;
		private CapsuleCollider mCapColliderDummy;



		#region testing / not user accessible
		public void PlaceOneOnion(){
			ClearOnions ();
			PlaceOnion ();
		}

		public void PlaceOnion(){
			GameObject onion = (GameObject)Instantiate (onionPrefab, Camera.main.transform.position + Camera.main.transform.forward * 2f, Quaternion.identity);
		}
		public void ClearOnions(){
			foreach (MetalOnion mo in FindObjectsOfType<MetalOnion>()) {
				Destroy (mo.gameObject);
			}
		}
		#endregion


		void SpawnDogs(){
			FindObjectOfType<Dogs> ().SpawnDogs();
		}

		// Use this for initialization
		void Start ()
		{
			Input.location.Start ();

			mMapListPanel.SetActive (false);

			mSession = UnityARSessionNativeInterface.GetARSessionNativeInterface ();
			UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
			StartARKit ();
			CoffeeCommandFeaturesVisualizer.EnablePointcloud ();
			LibPlacenote.Instance.RegisterListener (this);
	//		mRadiusSlider.value = 1.0f;
		}


		private void ARFrameUpdated (UnityARCamera camera)
		{
			mFrameUpdated = true;
			mARCamera = camera;
		}


		private void InitARFrameBuffer ()
		{
			mImage = new UnityARImageFrameData ();

			int yBufSize = mARCamera.videoParams.yWidth * mARCamera.videoParams.yHeight;
			mImage.y.data = Marshal.AllocHGlobal (yBufSize);
			mImage.y.width = (ulong)mARCamera.videoParams.yWidth;
			mImage.y.height = (ulong)mARCamera.videoParams.yHeight;
			mImage.y.stride = (ulong)mARCamera.videoParams.yWidth;

			// This does assume the YUV_NV21 format
			int vuBufSize = mARCamera.videoParams.yWidth * mARCamera.videoParams.yWidth/2;
			mImage.vu.data = Marshal.AllocHGlobal (vuBufSize);
			mImage.vu.width = (ulong)mARCamera.videoParams.yWidth/2;
			mImage.vu.height = (ulong)mARCamera.videoParams.yHeight/2;
			mImage.vu.stride = (ulong)mARCamera.videoParams.yWidth;

			mSession.SetCapturePixelData (true, mImage.y.data, mImage.vu.data);
		}


		// Update is called once per frame
		void Update ()
		{
			if (mFrameUpdated) {
				mFrameUpdated = false;
				if (mImage == null) {
					InitARFrameBuffer ();
				}

				if (mARCamera.trackingState == ARTrackingState.ARTrackingStateNotAvailable) {
					// ARKit pose is not yet initialized
					return;
				} else if (!mARKitInit && LibPlacenote.Instance.Initialized()) {
					mARKitInit = true;
					mLabelText.text = "ARKit Initialized";
				}

				Matrix4x4 matrix = mSession.GetCameraPose ();

				Vector3 arkitPosition = PNUtility.MatrixOps.GetPosition (matrix);
				Quaternion arkitQuat = PNUtility.MatrixOps.GetRotation (matrix);

				LibPlacenote.Instance.SendARFrame (mImage, arkitPosition, arkitQuat, mARCamera.videoParams.screenOrientation);
			}
		}


	//	public void OnListMapClick ()
	//	{
	//		if (!LibPlacenote.Instance.Initialized()) {
	//			Debug.Log ("SDK not yet initialized");
	//			ToastManager.ShowToast ("SDK not yet initialized", 2f);
	//			return;
	//		}
	//
	//		foreach (Transform t in mListContentParent.transform) {
	//			Destroy (t.gameObject);
	//		}
	//
	//
	//		mMapListPanel.SetActive (true);
	//		mInitButtonPanel.SetActive (false);
	////		mRadiusSlider.gameObject.SetActive (true);
	//		LibPlacenote.Instance.ListMaps ((mapList) => {
	//			// render the map list!
	//			foreach (LibPlacenote.MapInfo mapId in mapList) {
	//				if (mapId.metadata.userdata != null) {
	//					Debug.Log(mapId.metadata.userdata.ToString (Formatting.None));
	//				}
	//				AddMapToList (mapId);
	//			}
	//		});
	//	}

	//	public void OnRadiusSelect ()
	//	{
	//		Debug.Log ("Map search:" + mRadiusSlider.value.ToString("F2"));
	//		LocationInfo locationInfo = Input.location.lastData;
	//
	//		foreach (Transform t in mListContentParent.transform) {
	//			Destroy (t.gameObject);
	//		}
	//
	//		float radiusSearch = mRadiusSlider.value * mMaxRadiusSearch;
	//		mRadiusLabel.text = "Distance Filter: " + (radiusSearch / 1000.0).ToString ("F2") + " km";
	//
	//		LibPlacenote.Instance.SearchMaps(locationInfo.latitude, locationInfo.longitude, radiusSearch, 
	//			(mapList) => {
	//				// render the map list!
	//				foreach (LibPlacenote.MapInfo mapId in mapList) {
	//					if (mapId.metadata.userdata != null) {
	//						Debug.Log(mapId.metadata.userdata.ToString (Formatting.None));
	//					}
	//					AddMapToList (mapId);
	//				}
	//			});
	//	}

		public void OnCancelClick ()
		{
			mMapSelectedPanel.SetActive (false);
			mMapListPanel.SetActive (false);
			mInitButtonPanel.SetActive (true);
		}


		public void OnExitClick ()
		{
			mInitButtonPanel.SetActive (true);
			mExitButton.SetActive (false);
	//		mPlaneDetectionToggle.SetActive (false);

			//clear all existing planes
			mPNPlaneManager.ClearPlanes ();
	//		mPlaneDetectionToggle.GetComponent<Toggle>().isOn = false;

			LibPlacenote.Instance.StopSession ();
		}




		public void SelectMap (LibPlacenote.MapInfo mapInfo)
		{
			mSelectedMapInfo = mapInfo;
			mMapSelectedPanel.SetActive (true);
		}


		public void LoadSelectedMap ()
		{
			ConfigureSession (false);

			if (!LibPlacenote.Instance.Initialized()) {
				Debug.Log ("SDK not yet initialized");
				ToastManager.ShowToast ("SDK not yet initialized", 2f);
				return;
			}

			mLabelText.text = "Loading Map ID: " + mSelectedMapId;
			LibPlacenote.Instance.LoadMap (mSelectedMapId,
				(completed, faulted, percentage) => {
					if (completed) {
						mMapSelectedPanel.SetActive (false);
						mMapListPanel.SetActive (false);
						mInitButtonPanel.SetActive (false);
						mExitButton.SetActive (true);
	//					mPlaneDetectionToggle.SetActive(true);

						LibPlacenote.Instance.StartSession ();

						if (mReportDebug) {
							LibPlacenote.Instance.StartRecordDataset (
								(datasetCompleted, datasetFaulted, datasetPercentage) => {

									if (datasetCompleted) {
										mLabelText.text = "Dataset Upload Complete";
									} else if (datasetFaulted) {
										mLabelText.text = "Dataset Upload Faulted";
									} else {
										mLabelText.text = "Dataset Upload: " + datasetPercentage.ToString ("F2") + "/1.0";
									}
								});
							Debug.Log ("Started Debug Report");
						}

						mLabelText.text = "Loaded ID: " + mSelectedMapId;
					} else if (faulted) {
						mLabelText.text = "Failed to load ID: " + mSelectedMapId;
					} else {
						mLabelText.text = "Map Download: " + percentage.ToString ("F2") + "/1.0";
					}
				}
			);
		}

	

		public void StartNewMap ()
		{
			ConfigureSession (false);

			if (!LibPlacenote.Instance.Initialized()) {
				mLabelText.text = "Not initialized, no new map.";
	//			Debug.Log ("SDK not yet initialized");
				return;
			}

			mLabelText.text = "Started map.";
			mInitButtonPanel.SetActive (false);
			mMappingButtonPanel.SetActive (true);
			Debug.Log ("Started Session");
			LibPlacenote.Instance.StartSession ();

			if (mReportDebug) {
				LibPlacenote.Instance.StartRecordDataset (
					(completed, faulted, percentage) => {
						if (completed) {
							mLabelText.text = "Dataset Upload Complete";
						} else if (faulted) {
							mLabelText.text = "Dataset Upload Faulted";
						} else {
							mLabelText.text = "Dataset Upload: (" + percentage.ToString ("F2") + "/1.0)";
						}
					});
				Debug.Log ("Started Debug Report");
			}
			Invoke ("PlaceOneOnion", 2f);
			Invoke ("SpawnDogs", 2f);
		}


		private void StartARKit ()
		{
			mLabelText.text = "Initializing ARKit";
			Application.targetFrameRate = 60;
			ConfigureSession (false);
		}


		private void ConfigureSession(bool clearPlanes) {
			#if !UNITY_EDITOR
			ARKitWorldTrackingSessionConfiguration config = new ARKitWorldTrackingSessionConfiguration ();


			if (UnityARSessionNativeInterface.IsARKit_1_5_Supported ()) {
				config.planeDetection = UnityARPlaneDetection.HorizontalAndVertical;
			} else {
				config.planeDetection = UnityARPlaneDetection.Horizontal;
			}

			mPNPlaneManager.StartPlaneDetection ();

			if (clearPlanes) {
				mPNPlaneManager.ClearPlanes ();
			}


			config.alignment = UnityARAlignment.UnityARAlignmentGravity;
			config.getPointCloudData = true;
			config.enableLightEstimation = true;
			mSession.RunWithConfig (config);
			#endif
		}


		public void SaveMapNow (Action<string> cbFunc)
		{
			if (!LibPlacenote.Instance.Initialized()) {
				Debug.Log ("SDK not yet initialized");
				ToastManager.ShowToast ("SDK not yet initialized", 2f);
				return;
			}
			CLogger.Log ("Saving ..");
			bool useLocation = Input.location.status == LocationServiceStatus.Running;
			LocationInfo locationInfo = Input.location.lastData;


			// Case 0: This is the first map in this location (user did not load a previous map)
			// Save the map normally.
			// Case 1: This is a map loaded from a previous save. 
			// Update that save with new metadata 
			// Save an "invisible" map to keep the mesh


			mLabelText.text = "Saving...";

			LibPlacenote.Instance.SaveMap (
				(mapId) => {
					CLogger.Log ("Saving .. 2");
					LibPlacenote.Instance.StopSession ();
					mSaveMapId = mapId;
					mInitButtonPanel.SetActive (true);
					mMappingButtonPanel.SetActive (false);
	//				mPlaneDetectionToggle.SetActive (false);

					//clear all existing planes
					mPNPlaneManager.ClearPlanes ();
	//				mPlaneDetectionToggle.GetComponent<Toggle>().isOn = false;

					LibPlacenote.MapMetadataSettable metadata = new LibPlacenote.MapMetadataSettable();
					metadata.name = RandomName.Get ();
					mLabelText.text = "Saved Map Name: " + metadata.name;

					if (useLocation) {
						mLabelText.text = "location!";
						metadata.location = new LibPlacenote.MapLocation();
						metadata.location.latitude = locationInfo.latitude;
						metadata.location.longitude = locationInfo.longitude;
						metadata.location.altitude = locationInfo.altitude;
						
					} else {
						Debug.Log("no loc");
					}


					// Are we updating an old map, or saving a brand new place?
					if (UserDataManager.loadedExistingMap){
						CLogger.Log ("Saving .. was existed");
						// Go ahead and upload this map but make it invisible
						metadata.userdata["invisible"] = true;

						// Also, modify the map ID we loaded earlier
						LibPlacenote.Instance.SetMetadata (UserDataManager.loadedMapPlaceId, metadata);

					} else {
						CLogger.Log ("Saving .. NEW");
						// Save a brand new map normally
						metadata.userdata = UserDataManager.localDataObject;
//						LibPlacenote.Instance.SetMetadata (mapId, metadata);
					}
					LibPlacenote.Instance.SetMetadata (mapId, metadata);
					mCurrMapDetails = metadata;
					CLogger.Log ("Saving .. 3");
					UserDataManager.loadedMapPlaceId = mapId;
				},
				(completed, faulted, percentage) => {
					if (completed) {
						
						CLogger.Log ("Saving .. complete");
						FindObjectOfType<MetalOnion>().StupidCallback();
						mLabelText.text = "Upload Complete:" + mCurrMapDetails.name;
						CLogger.Log ("upload complete text?");
						cbFunc("test");
						CLogger.Log ("callback should have been called");
						Debug.Log("Callback should have been called");
					}
					else if (faulted) {
						mLabelText.text = "Upload of Map Named: " + mCurrMapDetails.name + "faulted";
					}
					else {
						mLabelText.text = "Uploading Map Named: " + mCurrMapDetails.name + "(" + percentage.ToString("F2") + "/1.0)";
					}
				}
			);
		}



		public void OnPose (Matrix4x4 outputPose, Matrix4x4 arkitPose) {}


		public void OnStatusChange (LibPlacenote.MappingStatus prevStatus, LibPlacenote.MappingStatus currStatus)
		{
			Debug.Log ("prevStatus: " + prevStatus.ToString() + " currStatus: " + currStatus.ToString());
			if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.LOST) {
				mLabelText.text = "Localized";
//				UserDataManager.SetLocalData
//				LoadShapesJSON (mSelectedMapInfo.metadata.userdata);
			} else if (currStatus == LibPlacenote.MappingStatus.RUNNING && prevStatus == LibPlacenote.MappingStatus.WAITING) {
				mLabelText.text = "Mapping";
			} else if (currStatus == LibPlacenote.MappingStatus.LOST) {
				mLabelText.text = "Searching for position lock";
			} else if (currStatus == LibPlacenote.MappingStatus.WAITING) {
				if (shapeObjList.Count != 0) {
//					ClearShapes ();
				}
			}
		}
	}


}
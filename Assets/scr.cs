
using CesiumForUnity;
namespace Google.XR.ARCoreExtensions.Samples.Geospatial
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;

    using UnityEngine.XR.ARFoundation;
    using UnityEngine.XR.ARSubsystems;

    public class scr : MonoBehaviour
    {
        public CesiumGeoreference origin;
        public AREarthManager EarthManager;
        public ARAnchorManager AnchorManager;
        public GameObject default_pref;

        public GameObject start_pref;
        public GameObject path_pref;
        public GameObject end_pref;

        public float distance;

        private IEnumerator coroutine;
	    public string status;

        private bool recording;
        private List<ARGeospatialAnchor> path;
        private GeospatialPose prev;

        private const int lat_to_feet = 364000;
        private const int long_to_feet = 288200;

        // Start is called before the first frame update
        void Start()
        {
            recording = false;
            status = "Begin";
            path = new List<ARGeospatialAnchor>();
            coroutine = place_anchor();
            // StartCoroutine(coroutine);
        }

        // Update is called once per frame
        void Update()
        {
        }

        public void recclick() {
            if (recording) stop_recording();
            else StartCoroutine(coroutine);
            recording = !recording;
        }

        private bool localized() {
            return ARSession.state == ARSessionState.SessionTracking && Input.location.status == LocationServiceStatus.Running && EarthManager.EarthTrackingState == TrackingState.Tracking;
        }

        private void stop_recording() {
            if (localized()) {
                GeospatialPose pos = EarthManager.CameraGeospatialPose;
                ARGeospatialAnchor anchor = AnchorManager.AddAnchor(pos.Latitude, pos.Longitude, pos.Altitude-1, Quaternion.identity);
                place_object(anchor);
            }
            StopCoroutine(coroutine);
        }

        private void start_recording() {
            if (localized()) {
                GeospatialPose pos = EarthManager.CameraGeospatialPose;
                ARGeospatialAnchor anchor = AnchorManager.AddAnchor(pos.Latitude, pos.Longitude, pos.Altitude-1, Quaternion.identity);
                prev = pos;
                if (place_object(anchor)) StartCoroutine(coroutine);
            }
        }

        private bool place_object(ARGeospatialAnchor anchor, GameObject prefab=null, bool add_to_path=true) {
            if (prefab == null) prefab = default_pref;
            if (anchor != null) {
                GameObject anchorGO = Instantiate(prefab, anchor.transform);
                anchor.gameObject.SetActive(true);
                anchorGO.transform.parent = anchor.gameObject.transform;
                if (add_to_path) path.Add(anchor);
                return true;
            }
            return false;
        }

        private void update_path(ARGeospatialAnchor anchor, GameObject prefab) {
            Destroy(anchor.gameObject.transform.GetChild(0).gameObject);
            place_object(anchor, prefab, false);
        }

        private void set_path() {
            update_path(path[0], start_pref);
            for (int i = 1; i < path.Count - 1; i++) update_path(path[i], path_pref);
            update_path(path[path.Count - 1], end_pref);
        }

        private IEnumerator place_anchor()
        {
            while (true) {
                yield return new WaitForSeconds(5.0f);
		        status = "Waiting";
                
                if (localized()) {
                    status = "Good";
                    GeospatialPose pos = EarthManager.CameraGeospatialPose;
                    if (path.Count > 0) {
                        if (Mathf.Pow((float)((prev.Latitude - pos.Latitude) * lat_to_feet), 2) + Mathf.Pow((float)((prev.Longitude - pos.Longitude) * lat_to_feet), 2) > 36) {
                            ARGeospatialAnchor anchor = AnchorManager.AddAnchor(pos.Latitude, pos.Longitude, pos.Altitude-1, Quaternion.identity);
                            place_object(anchor);
                            prev = pos;
                            status = string.Format("Placed at {0}°, {1}°", 
                            pos.Latitude.ToString("F6"),
                            pos.Longitude.ToString("F6"));
                        }
                    }
                    if (Mathf.Pow((float)((origin.latitude - pos.Latitude) * lat_to_feet), 2) + Mathf.Pow((float)((origin.longitude - pos.Longitude) * long_to_feet), 2) > 6400000000) {
                        origin.latitude = pos.Latitude;
                        origin.longitude = pos.Longitude;
                    }
                }
            }
        }
    }
}
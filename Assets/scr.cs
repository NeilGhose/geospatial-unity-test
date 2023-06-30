
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
        private List<ARGeospatialAnchor> rec_path;
        private List<GeospatialPose> path;
        private GeospatialPose prev;
        private int counter;

        private const int lat_to_feet = 364000;
        private const int long_to_feet = 288200;

        // Start is called before the first frame update
        void Start()
        {
            recording = false;
            status = "Begin";
            rec_path = new List<ARGeospatialAnchor>();
            path = new List<GeospatialPose>();
            coroutine = place_anchor();
            counter = 0;
            // StartCoroutine(coroutine);
        }

        // Update is called once per frame
        void Update()
        {}

        public void recclick() {
            counter++;
            if (recording) stop_recording();
            else start_recording();
            recording = !recording;
        }

        private bool localized() {
            return ARSession.state == ARSessionState.SessionTracking && Input.location.status == LocationServiceStatus.Running && EarthManager.EarthTrackingState == TrackingState.Tracking;
        }

        private void stop_recording() {
            StopCoroutine(coroutine);
            if (localized()) {
                GeospatialPose pos = EarthManager.CameraGeospatialPose;
                place_rec_object(pos);
                place_final_object(pos, end_pref, Quaternion.identity);
            }
            set_path();
            // status = "Recording Stopped" + counter.ToString();
        }

        private void start_recording() {
            if (localized()) {
                GeospatialPose pos = EarthManager.CameraGeospatialPose;
                prev = pos;
                if (place_rec_object(pos)) StartCoroutine(coroutine);
                status = "Recording Started " + counter.ToString();
            }
        }

        private bool place_anchor_GO(ARGeospatialAnchor anchor, GameObject prefab) {
            if (anchor != null) {
                GameObject anchorGO = Instantiate(prefab, anchor.transform);
                anchor.gameObject.SetActive(true);
                anchorGO.transform.parent = anchor.gameObject.transform;

                return true;
            }
            return false;
        }

        private bool place_rec_object(GeospatialPose pos) {
            ARGeospatialAnchor anchor = AnchorManager.AddAnchor(pos.Latitude, pos.Longitude, pos.Altitude-1, Quaternion.identity);
            if (place_anchor_GO(anchor, default_pref)) {
                rec_path.Add(anchor);
                path.Add(pos);
                return true;
            }
            return false;
        }

        private bool place_final_object(GeospatialPose pos, GameObject prefab, Quaternion dir) {
            ARGeospatialAnchor anchor = AnchorManager.AddAnchor(pos.Latitude, pos.Longitude, pos.Altitude-1, dir);

            return place_anchor_GO(anchor, prefab);
        }

        private void set_path() {
            place_final_object(path[0], start_pref, Quaternion.identity);

            for (int i = 1; i < path.Count - 1; i++) {
                place_final_object(path[i], path_pref, Quaternion.LookRotation(rec_path[i+1].transform.position - rec_path[i].transform.position, Vector3.up));
                // status += "\nself: " + path[i].transform.position.ToString() + " target: " + path[i+1].transform.position.ToString() + " rotation: " + path[i].transform.eulerAngles;
            }
            int c = rec_path.Count;
            for (int i = 0; i<c; i++) UnityEngine.Object.Destroy(rec_path[i].gameObject);     
        }

        private IEnumerator place_anchor()
        {
            while (true) {
                yield return new WaitForSeconds(0.1f);
		        status = "Waiting";
                
                if (localized()) {
                    status = "Good";
                    GeospatialPose pos = EarthManager.CameraGeospatialPose;
                    if (path.Count > 0) {
                        if (Mathf.Pow((float)((prev.Latitude - pos.Latitude) * lat_to_feet), 2) + Mathf.Pow((float)((prev.Longitude - pos.Longitude) * lat_to_feet), 2) > 36) {
                            if (place_rec_object(pos)) {
                                prev = pos;
                                status = string.Format("Recording: Placed at {0}°, {1}°", 
                                pos.Latitude.ToString("F6"),
                                pos.Longitude.ToString("F6"));
                            }
                        }
                    }
                    // if (Mathf.Pow((float)((origin.latitude - pos.Latitude) * lat_to_feet), 2) + Mathf.Pow((float)((origin.longitude - pos.Longitude) * long_to_feet), 2) > 6400000000) {
                    //     origin.latitude = pos.Latitude;
                    //     origin.longitude = pos.Longitude;
                    // }
                }
            }
        }
    }
}
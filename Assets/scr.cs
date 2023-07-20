
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

        public bool path_placed;

        private IEnumerator coroutine;
	    public string status;

        private bool recording;
        private bool ready;
        private List<ARGeospatialAnchor> rec_path;
        private List<GeospatialPose> path;
        private GeospatialPose prev;
        private int counter;

        private const double lat_to_miles = 69;
        private const double long_to_miles = 54.7;
        private const double meters_to_miles = 0.000621371;

        // Start is called before the first frame update
        void Start()
        {
            recording = false;
            ready = false;
            status = "Begin";
            reset_path();
            path_placed = true;
            StartCoroutine(on_start_after_loc());
            coroutine = place_anchor();
            counter = 0;
        }

        private IEnumerator on_start_after_loc() {
            while (!localized()) yield return new WaitForSeconds(0.1f);
            GeospatialPose pos = EarthManager.CameraGeospatialPose;
            if (!ready && Mathf.Pow((float)((origin.latitude - pos.Latitude) * lat_to_miles), 2) + Mathf.Pow((float)((origin.longitude - pos.Longitude) * long_to_miles), 2) > 225) {
                origin.latitude = pos.Latitude;
                origin.longitude = pos.Longitude;
            }
            ready = true;
            if (path.Count > 0) set_path();
        }
        public void recclick() {
            counter++;
            if (recording) stop_recording();
            else start_recording();
            recording = !recording;
        }

        private bool localized() {
            return ARSession.state == ARSessionState.SessionTracking && Input.location.status == LocationServiceStatus.Running && EarthManager.EarthTrackingState == TrackingState.Tracking;
        }

        public bool localized_and_ready() {
            return ready && localized();
        }

        private void stop_recording() {
            StopCoroutine(coroutine);
            if (localized_and_ready()) {
                GeospatialPose pos = EarthManager.CameraGeospatialPose;
                place_rec_object(pos);
            }
            set_path();
            // status = "Recording Stopped" + counter.ToString();
        }

        private void start_recording() {
            if (localized_and_ready()) {
                reset_path();
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
            Debug.Log("place_final_object=======================");
            Debug.Log(dir.ToString());
            Debug.Log(pos.Latitude.ToString() + ", " + pos.Longitude.ToString() + ", " + pos.Altitude.ToString());
            ARGeospatialAnchor anchor = AnchorManager.AddAnchor(pos.Latitude, pos.Longitude, pos.Altitude-1, dir);

            return place_anchor_GO(anchor, prefab);
        }

        private void set_path() {
            // Debug.Log("set_path");
            // Debug.Log(path[0].Latitude.ToString() + ", " + path[0].Longitude.ToString() + ", " + path[0].Altitude.ToString());
            // place_final_object(path[0], start_pref, Quaternion.identity);
            // Debug.Log("set_path2");

            for (int i = 0; i < path.Count; i++) {
                GameObject prefab = path_pref;
                Quaternion dir = Quaternion.identity;
                if (i==0) prefab = start_pref;
                else if (i==path.Count-1) prefab = end_pref;
                else {
                    dir = Quaternion.LookRotation(new Vector3(
                        (float)((path[i+1].Longitude - path[i].Longitude) * lat_to_miles), 
                        (float)((path[i+1].Altitude - path[i].Altitude) * meters_to_miles),
                        (float)((path[i+1].Latitude - path[i].Latitude) * lat_to_miles)), Vector3.up);
                }
                place_final_object(path[i], prefab, dir);
                // status += "\nself: " + path[i].transform.position.ToString() + " target: " + path[i+1].transform.position.ToString() + " rotation: " + path[i].transform.eulerAngles;
            }

            // place_final_object(path[path.Count - 1], end_pref, Quaternion.identity);

            int c = rec_path.Count;
            for (int i = 0; i<c; i++) UnityEngine.Object.Destroy(rec_path[i].gameObject);

            path_placed = true;
        }

        private IEnumerator place_anchor()
        {
            while (true) {
                yield return new WaitForSeconds(0.1f);
		        status = "Waiting";
                
                if (localized_and_ready()) {
                    status = "Good";
                    GeospatialPose pos = EarthManager.CameraGeospatialPose;
                    if (path.Count > 0) {
                        if (Mathf.Pow((float)((prev.Latitude - pos.Latitude) * lat_to_miles * 5280), 2) + Mathf.Pow((float)((prev.Longitude - pos.Longitude) * lat_to_miles * 5280), 2) > 36) {
                            if (place_rec_object(pos)) {
                                prev = pos;
                                status = string.Format("Recording: Placed at {0}°, {1}°", 
                                pos.Latitude.ToString("F6"),
                                pos.Longitude.ToString("F6"));
                            }
                        }
                    }
                }
            }
        }

        public void geojson_to_path(List<List<double>> path_data){
            Debug.Log("geojson_to_path");
            reset_path();
            for (int i = 0; i < path_data.Count; i++) {
                GeospatialPose p = new GeospatialPose();
                p.Longitude = path_data[i][0];
                p.Latitude = path_data[i][1];
                p.Altitude = path_data[i][2];
                path.Add(p);
                Debug.Log(p.Latitude.ToString() + ", " + p.Longitude.ToString() + ", " + p.Altitude.ToString());
            }
            StartCoroutine(on_start_after_loc());
        }

        public string path_to_geojson() {
            string geojson = "{\"type\":\"Feature\",\"properties\":{},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[";
            for (int i = 0; i < path.Count; i++) {
                geojson += "[" + path[i].Longitude.ToString() + "," + path[i].Latitude.ToString() + "," + path[i].Altitude.ToString() + "]";
                if (i < path.Count - 1) geojson += ",";
            }
            geojson += "]}}";
            return geojson;
        }

        public bool full_path() {
            return path.Count > 0;
        }

        private void reset_path() {
            rec_path = new List<ARGeospatialAnchor>();
            path = new List<GeospatialPose>();
            path_placed = false;
        }
    }
}
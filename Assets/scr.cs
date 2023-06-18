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
        public AREarthManager EarthManager;
        public ARAnchorManager AnchorManager;
        public GameObject prefab;

        private IEnumerator coroutine;
	public string status;

        // Start is called before the first frame update
        void Start()
        {
            status = "Begin";
            coroutine = place_anchor();
            StartCoroutine(coroutine);
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        private IEnumerator place_anchor()
        {
            while (true) {
                yield return new WaitForSeconds(5.0f);
		status = "Waiting";
                if (ARSession.state == ARSessionState.SessionTracking && Input.location.status == LocationServiceStatus.Running && EarthManager.EarthTrackingState == TrackingState.Tracking) {
                    status = "Good";
                    GeospatialPose pos = EarthManager.CameraGeospatialPose;
                    ARGeospatialAnchor anchor = AnchorManager.AddAnchor(pos.Latitude, pos.Longitude, pos.Altitude-1, Quaternion.identity);
                    if (anchor != null) {
                        GameObject anchorGO = Instantiate(prefab, anchor.transform);
                        anchor.gameObject.SetActive(true);
                        anchorGO.transform.parent = anchor.gameObject.transform;
			status = string.Format("Placed at {0}°, {1}°", 
                		pos.Latitude.ToString("F6"),
                		pos.Longitude.ToString("F6"));
                    }
                }
            }
        }
    }
}

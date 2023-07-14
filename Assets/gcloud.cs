using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Google.XR.ARCoreExtensions.Samples.Geospatial
{
    public class gcloud : MonoBehaviour
    {

        public scr controller;
        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(getData("https://storage.googleapis.com/path-data/path.geojson"));
        }

        IEnumerator getData(string url)
        {
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
                Debug.Log(www.error);
            else {
                string data = www.downloadHandler.text;
                data = data[(data.IndexOf("coordinates\":[[") + 15)..];
                data = data[..data.IndexOf("]],\"type\":\"LineString\"")];
                string[] points = data.Split("],[");
                List<List<double>> path = new List<List<double>>();
                for (int i = 0; i < points.Length; i++) {
                    path.Add(new List<double>());
                    string[] point = points[i].Split(",");
                    for (int j = 0; j < point.Length; j++) {
                        path[i].Add(double.Parse(point[j]));
                    }
                }
                controller.geojson_to_path(path);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Google.XR.ARCoreExtensions.Samples.Geospatial
{
    public class gcloud : MonoBehaviour
    {

        public scr controller;
        public string status;

        private List<List<List<double>>> paths;
        // Start is called before the first frame update
        void Start()
        {
            status = "Retrieving data";
            paths = new List<List<List<double>>>();
            StartCoroutine(getBucketData("https://storage.googleapis.com/storage/v1/b/path-data/o"));
            // StartCoroutine(postData("test5.geojson", "{\"type\":\"Feature\",\"properties\":{},\"geometry\":{\"type\":\"LineString\",\"coordinates\":[[-122.084,37.4219983,0],[-122.084,37.4219983,0]]}}"));
        }

        IEnumerator getBucketData(string url) {
            UnityWebRequest www = UnityWebRequest.Get(url);
            Debug.Log("bucket url: " + url);
            yield return www.SendWebRequest();
            if (www.error != null) Debug.Log(www.error);
            else {
                string data = www.downloadHandler.text;
                while (data.Contains("name\": \"")) {
                    data = data[(data.IndexOf("name\": \"") + 8)..];
                    string name = data[..data.IndexOf("\"")];
                    StartCoroutine(getData("https://storage.googleapis.com/path-data/" + name));
                }
                status = "All buckets retrieved";
            }
        }

        IEnumerator getData(string url)
        {
            Debug.Log("data url: " + url);
            UnityWebRequest www = UnityWebRequest.Get(url);
            yield return www.SendWebRequest();
            if (www.error != null) Debug.Log(www.error);
            else {
                string data = www.downloadHandler.text;
                data = data[(data.IndexOf("coordinates\":[[") + 15)..];
                data = data[..data.IndexOf("]]")];
                string[] points = data.Split("],[");
                List<List<double>> path = new List<List<double>>();
                for (int i = 0; i < points.Length; i++) {
                    path.Add(new List<double>());
                    string[] point = points[i].Split(",");
                    for (int j = 0; j < point.Length; j++) {
                        path[i].Add(double.Parse(point[j]));
                    }
                }
                paths.Add(path);
                status = "File " + url[(url.LastIndexOf("/") + 1)..] + " retrieved";
                Debug.Log("status: " + status);
            }
            StartCoroutine(sendPathsData());
        }

        IEnumerator sendPathsData()
        {
            Debug.Log("sendPathsData");
            Debug.Log("paths.Count: " + paths.Count);
            while (paths.Count > 0){
                if (controller.full_path()) yield return new WaitForSeconds(0.1f);
                controller.geojson_to_path(paths[0]);
                paths.RemoveAt(0);
            }
        }

        public IEnumerator postData(string url_name, string data)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
            // string url = "https://storage.googleapis.com/upload/storage/v1/b/path-data/o?uploadType=media&name=" + url_name;
            string url = "https://storage.googleapis.com/path-data/" + url_name;
            UnityWebRequest www = UnityWebRequest.Put(url, bytes);
            www.SetRequestHeader("Content-Type", "application/octet-stream");
            yield return www.SendWebRequest();
            if (www.error != null) Debug.Log(www.error);
            else Debug.Log("Form upload complete!");
            status = "File " + url_name + " uploaded";
        }
    }
}